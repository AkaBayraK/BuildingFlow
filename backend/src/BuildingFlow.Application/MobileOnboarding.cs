using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
namespace BuildingFlow.Application;

public sealed record MobileRegistrationRequest(Guid RequestId, string? Username, string? Password, string? PasswordConfirmation,
    string? FirstName, string? LastName, string? PhoneNumber, string? Email, string? PersonCode = null);
public sealed record PersonLinkRequest(string? PersonCode, string? FirstName, string? LastName, string? PhoneNumber, string? Email);
public sealed record PersonLinkResult(string Status, Guid? PropertyId = null, string? PropertyName = null, string? ErrorCode = null);
public sealed record MobileRegistrationResult(string UserId, string Username, PersonLinkResult PersonLink, bool Replayed = false);
public sealed record PersonIdentity(string FirstName, string LastName, string PhoneNumber, string? Email);
public sealed record StoredRegistration(string Fingerprint, string UserId, string Username);

public interface IMobileOnboardingStore
{
    Task<StoredRegistration?> RegistrationAsync(Guid requestId, CancellationToken token);
    Task<int> RecentRegistrationsAsync(string clientKey, TimeSpan window, CancellationToken token);
    Task SaveRegistrationAsync(Guid requestId, string fingerprint, string userId, string username, string clientKey, PersonIdentity identity, CancellationToken token);
    Task<PersonLinkResult> LinkAsync(string userId, string codeHash, PersonIdentity identity, CancellationToken token);
    Task<bool> IsLockedAsync(string userId, CancellationToken token);
    Task RecordFailureAsync(string userId, CancellationToken token);
}

public static class PersonMatching
{
    public const string Linked = "LINKED", Rejected = "REJECTED", Locked = "LOCKED", NotRequested = "NONE";
    public const int MaxFailures = 5;
    public static readonly TimeSpan LockWindow = TimeSpan.FromMinutes(15);
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public static string Name(string value) => string.Join(' ', value.Trim().ToUpper(Turkish).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    public static bool Matches(PersonIdentity entered, string firstName, string lastName, string? phone, string? email) =>
        Name(entered.FirstName) == Name(firstName) && Name(entered.LastName) == Name(lastName) &&
        SmsText.NormalizePhone(phone) is { } stored && stored == entered.PhoneNumber &&
        (string.IsNullOrWhiteSpace(email) || string.Equals(email.Trim(), entered.Email, StringComparison.OrdinalIgnoreCase));
}

public sealed class MobileOnboardingService(IKeycloakAdmin keycloak, IMobileOnboardingStore store, IPersonCodeHasher hasher, ICurrentUserService user)
{
    private const int RegistrationsPerHour = 5;
    [DoesNotReturn] private static void Invalid(string field, ErrorDefinition? error = null) => throw new RequestValidationException(new() { [field] = [(error ?? ErrorCatalog.MobileRegistrationInvalid).Create()] });

    public async Task<MobileRegistrationResult> RegisterAsync(MobileRegistrationRequest request, string clientKey, CancellationToken token)
    {
        if (request.RequestId == Guid.Empty) Invalid("requestId");
        var username = request.Username?.Trim().ToLowerInvariant();
        if (username is null || !UserRoles.Username().IsMatch(username)) Invalid("username");
        if (request.Password is null || !StrongPassword(request.Password)) Invalid("password", ErrorCatalog.UserPasswordWeak);
        if (request.Password != request.PasswordConfirmation) Invalid("passwordConfirmation");
        var identity = Identity(request.FirstName, request.LastName, request.PhoneNumber, request.Email, true);
        var code = string.IsNullOrWhiteSpace(request.PersonCode) ? null : request.PersonCode.Trim();
        if (code is not null && !PersonCodePolicy.IsWellFormed(code)) Invalid("personCode", ErrorCatalog.MobileLinkRejected);
        var fingerprint = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', username, identity.FirstName, identity.LastName, identity.PhoneNumber, identity.Email, code))));
        if (await store.RegistrationAsync(request.RequestId, token) is { } previous)
        {
            if (previous.Fingerprint != fingerprint) throw new HierarchyConflictException(ErrorCatalog.MobileRegistrationConflict.Create());
            return new(previous.UserId, previous.Username, new(PersonMatching.NotRequested), true);
        }
        if (await store.RecentRegistrationsAsync(clientKey, TimeSpan.FromHours(1), token) >= RegistrationsPerHour) throw new HierarchyConflictException(ErrorCatalog.MobileRateLimited.Create());
        string id;
        try { id = await keycloak.CreateAsync(new(username, identity.Email!, identity.FirstName, identity.LastName, true), token); }
        catch (IdentityConflictException) { throw new HierarchyConflictException(ErrorCatalog.MobileRegistrationConflict.Create()); }
        try
        {
            await keycloak.SetPasswordAsync(id, request.Password, false, token);
            await store.SaveRegistrationAsync(request.RequestId, fingerprint, id, username, clientKey, identity, token);
        }
        catch
        {
            // A registration either completes or leaves no Keycloak account behind; the app retries with the same request id.
            try { await keycloak.DeleteAsync(id, CancellationToken.None); } catch (IdentityUnavailableException) { }
            throw;
        }
        var link = code is null ? new PersonLinkResult(PersonMatching.NotRequested) : await LinkFor(id, code, identity, token);
        return new(id, username, link);
    }

    public async Task<PersonLinkResult> LinkAsync(PersonLinkRequest request, CancellationToken token)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId)) throw new UnauthorizedAccessException();
        var identity = Identity(request.FirstName, request.LastName, request.PhoneNumber, request.Email, false);
        var result = await LinkFor(user.UserId, request.PersonCode?.Trim() ?? "", identity, token);
        return result.Status switch
        {
            PersonMatching.Linked => result,
            PersonMatching.Locked => throw new HierarchyConflictException(ErrorCatalog.MobileLinkLocked.Create()),
            _ => throw new HierarchyConflictException(ErrorCatalog.MobileLinkRejected.Create())
        };
    }

    private async Task<PersonLinkResult> LinkFor(string userId, string code, PersonIdentity identity, CancellationToken token)
    {
        if (await store.IsLockedAsync(userId, token)) return new(PersonMatching.Locked, ErrorCode: ErrorCatalog.MobileLinkLocked.Code);
        if (!PersonCodePolicy.IsWellFormed(code))
        {
            await store.RecordFailureAsync(userId, token);
            return new(PersonMatching.Rejected, ErrorCode: ErrorCatalog.MobileLinkRejected.Code);
        }
        return await store.LinkAsync(userId, hasher.Hash(code), identity, token);
    }

    private static PersonIdentity Identity(string? first, string? last, string? phone, string? email, bool emailRequired)
    {
        first = first?.Trim(); last = last?.Trim(); email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(first) || first.Length > 80 || first.Any(char.IsControl)) Invalid("firstName");
        if (string.IsNullOrEmpty(last) || last.Length > 80 || last.Any(char.IsControl)) Invalid("lastName");
        var normalized = SmsText.NormalizePhone(phone);
        if (normalized is null) Invalid("phoneNumber", ErrorCatalog.SmsPhoneInvalid);
        if (email is null ? emailRequired : email.Length > 254 || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email) Invalid("email");
        return new(PersonNames.Given(first)!, PersonNames.Family(last)!, normalized, email);
    }

    private static bool StrongPassword(string value) => value.Length is >= 12 and <= 128 && value.Any(char.IsAsciiLetterUpper) && value.Any(char.IsAsciiLetterLower) && value.Any(char.IsAsciiDigit);
}
