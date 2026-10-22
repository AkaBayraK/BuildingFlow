using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using System.Text.RegularExpressions;
namespace BuildingFlow.Application;

public sealed record KeycloakUser(string Id, string Username, string? Email, string? FirstName, string? LastName, bool Enabled, bool EmailVerified, DateTimeOffset? CreatedAt);
public sealed record EmailVerificationPolicy(bool Enabled = false);
public sealed record KeycloakUserDraft(string Username, string Email, string FirstName, string LastName, bool Enabled);
public sealed record LinkedPerson(Guid PersonId, Guid PropertyId, string PropertyName, string FullName, long Version);
public sealed record UserResponse(string Id, string Username, string? Email, string? FirstName, string? LastName, bool Enabled, bool EmailVerified,
    DateTimeOffset? CreatedAt, IReadOnlyList<string> Roles, int PropertyCount, IReadOnlyList<LinkedPerson> LinkedPeople, bool CanEdit, Guid? PhotoId = null, bool CanManagePhoto = false, string? InvitationStatus = null, bool CanEditEmail = true);
public sealed record CreateUserRequest(string? Username, string? Email, string? FirstName, string? LastName, bool Enabled = true,
    IReadOnlyList<string>? Roles = null, Guid? PropertyId = null, string? TemporaryPassword = null, string? Phone = null);
public sealed record UpdateUserRequest(string? Email, string? FirstName, string? LastName, bool Enabled, IReadOnlyList<string>? Roles);
public sealed record LinkPersonRequest(Guid PropertyId, Guid PersonId, long Version);
public sealed record UserScope(string ActorId, bool All, IReadOnlyList<Guid> Properties, IReadOnlySet<string> ManagedRoles);

public sealed class IdentityUnavailableException() : Exception("Identity administration is unavailable.");
public sealed class IdentityConflictException() : Exception("User already exists.");

public interface IKeycloakAdmin
{
    Task<(IReadOnlyList<KeycloakUser> Users, int Total)> SearchAsync(string? search, int first, int max, CancellationToken token);
    Task<KeycloakUser?> GetAsync(string id, CancellationToken token);
    Task<IReadOnlyList<string>> RealmRolesAsync(string id, CancellationToken token);
    Task<string> CreateAsync(KeycloakUserDraft draft, CancellationToken token);
    Task UpdateAsync(string id, KeycloakUserDraft draft, CancellationToken token);
    Task ChangeRealmRolesAsync(string id, IReadOnlyCollection<string> add, IReadOnlyCollection<string> remove, CancellationToken token);
    Task SetPasswordAsync(string id, string password, bool temporary, CancellationToken token);
    Task SendActionsEmailAsync(string id, IReadOnlyCollection<string> actions, CancellationToken token);
    Task DeleteAsync(string id, CancellationToken token);
}

public interface IUserDirectoryStore
{
    Task<IReadOnlyList<Guid>> ManagedPropertiesAsync(string actor, CancellationToken token);
    Task<PagedRows<string>> MemberIdsAsync(IReadOnlyList<Guid> properties, int page, int pageSize, CancellationToken token);
    Task<IReadOnlySet<string>> MembersAmongAsync(IReadOnlyList<Guid>? properties, IReadOnlyCollection<string> userIds, CancellationToken token);
    Task<IReadOnlyDictionary<string, (int PropertyCount, IReadOnlyList<LinkedPerson> People)>> LinksAsync(IReadOnlyCollection<string> userIds, IReadOnlyList<Guid>? properties, CancellationToken token);
    Task<bool> PropertyExistsAsync(Guid property, CancellationToken token);
    Task EnsureMembershipAsync(Guid property, string userId, CancellationToken token);
    Task InitializeCreatedAccountAsync(Guid? property, string userId, string firstName, string lastName, string? phone, CancellationToken token);
    Task<LinkedPerson?> LinkAsync(Guid property, Guid person, long version, string userId, CancellationToken token);
    Task<bool> UnlinkAsync(Guid property, Guid person, long version, string userId, CancellationToken token);
}

public static partial class UserRoles
{
    public static readonly IReadOnlySet<string> All = new HashSet<string> { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT", "RESIDENT" };
    public static readonly IReadOnlySet<string> SiteManaged = new HashSet<string> { "MANAGER", "ACCOUNTANT", "RESIDENT" };
    public static readonly IReadOnlySet<string> Privileged = new HashSet<string> { "SUPER_ADMIN", "SITE_ADMIN" };
    [GeneratedRegex(@"^[a-z0-9][a-z0-9._-]{2,49}$", RegexOptions.CultureInvariant)]
    public static partial Regex Username();
}

public sealed class UserService(IKeycloakAdmin keycloak, IUserDirectoryStore store, ICurrentUserService user, IMobileProfileStore? photos = null, IManagedUserPhotoStore? managedPhotos = null, EmailVerificationPolicy? emailPolicy = null)
{
    private const int MaxSearchScan = 100;

    private async Task<UserScope> Scope(CancellationToken token)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId)) throw new UnauthorizedAccessException();
        if (user.IsInRole("SUPER_ADMIN")) return new(user.UserId, true, [], UserRoles.All);
        if (!user.IsInRole("SITE_ADMIN")) throw new UnauthorizedAccessException();
        return new(user.UserId, false, await store.ManagedPropertiesAsync(user.UserId, token), UserRoles.SiteManaged);
    }
    [DoesNotReturn] private static void Invalid(string field, ErrorDefinition? error = null) => throw new RequestValidationException(new() { [field] = [(error ?? ErrorCatalog.UserInvalid).Create()] });
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task<PagedRows<UserResponse>> ListAsync(int page, int pageSize, string? search, CancellationToken token)
    {
        var scope = await Scope(token);
        if (page is < 1 or > 100000 || pageSize is < 1 or > 50) throw new RequestValidationException(new() { ["page"] = [ErrorCatalog.InvalidPage.Create()] });
        search = Clean(search);
        if (search?.Length > 100) Invalid("search");
        if (scope.All)
        {
            var (users, total) = await keycloak.SearchAsync(search, (page - 1) * pageSize, pageSize, token);
            return new(await Describe(users, scope, token), page, pageSize, total);
        }
        if (scope.Properties.Count == 0) return new([], page, pageSize, 0);
        if (search is null)
        {
            var ids = await store.MemberIdsAsync(scope.Properties, page, pageSize, token);
            var found = (await Task.WhenAll(ids.Items.Select(id => keycloak.GetAsync(id, token)))).OfType<KeycloakUser>().ToArray();
            return new(await Describe(found, scope, token), page, pageSize, ids.TotalCount);
        }
        var (candidates, _) = await keycloak.SearchAsync(search, 0, MaxSearchScan, token);
        var members = await store.MembersAmongAsync(scope.Properties, candidates.Select(c => c.Id).ToArray(), token);
        var visible = candidates.Where(c => members.Contains(c.Id)).ToArray();
        return new(await Describe(visible.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), scope, token), page, pageSize, visible.Length);
    }

    public async Task<UserResponse?> GetAsync(string id, CancellationToken token)
    {
        var scope = await Scope(token);
        var found = await Visible(id, scope, token);
        return found is null ? null : (await Describe([found], scope, token))[0];
    }

    // İŞ19: the account's own photo (added by its owner with consent) is visible to the managers who can see the account.
    private async Task<KeycloakUser?> PhotoTarget(string id, CancellationToken token)
    {
        var scope = await Scope(token);
        var target = await Visible(id, scope, token); if (target is null) return null;
        if (id != scope.ActorId) Editable(scope, id, await keycloak.RealmRolesAsync(id, token));
        return target;
    }
    public async Task<UserPhotoState?> PhotoStateAsync(string id, CancellationToken token)
    {
        if (await PhotoTarget(id, token) is null) return null;
        return await (managedPhotos ?? throw new IdentityUnavailableException()).ReadPhotoAsync(id, token);
    }
    public async Task<UserPhotoResult?> SavePhotoAsync(string id, UserPhotoCommand command, CancellationToken token)
    {
        if (await PhotoTarget(id, token) is not { } target) return null;
        async Task Authorize(CancellationToken ct) { if (await PhotoTarget(id, ct) is null) throw new UnauthorizedAccessException(); }
        var result = await (managedPhotos ?? throw new IdentityUnavailableException()).SavePhotoAsync(id, command, target.FirstName ?? "", target.LastName ?? "", Authorize, token);
        var p = result.Profile;
        // The mobile receipt remains private; target phone/name never enter the administration response.
        var state = await managedPhotos.ReadPhotoAsync(id, token);
        return new(result.OperationId, new(id, p.PhotoId, p.PhotoSha256, p.Version, state.CanReplace), result.Replayed);
    }

    public async Task<byte[]?> PhotoAsync(string id, CancellationToken token)
    {
        var scope = await Scope(token);
        return photos is null || await Visible(id, scope, token) is null ? null : await photos.AccountPhotoAsync(id, token);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken token)
    {
        var scope = await Scope(token);
        var draft = Draft(Clean(request.Username)?.ToLowerInvariant(), request.Email, request.FirstName, request.LastName, request.Enabled);
        var phone = Clean(request.Phone);
        if (phone is not null)
        {
            var digits = phone.Count(char.IsAsciiDigit);
            if (phone.Length > 32 || digits is < 7 or > 15 || phone.Any(c => !char.IsAsciiDigit(c) && !"+ -()".Contains(c))) Invalid("phone");
            // The shared mobile profile accepts at most 80 characters per name.
            if (draft.FirstName.Length > 80) Invalid("firstName");
            if (draft.LastName.Length > 80) Invalid("lastName");
        }
        var roles = Roles(request.Roles, scope);
        if (request.TemporaryPassword is { } password && !StrongPassword(password)) Invalid("temporaryPassword", ErrorCatalog.UserPasswordWeak);
        if (request.PropertyId is { } property)
        {
            if (property == Guid.Empty || !scope.All && !scope.Properties.Contains(property) || scope.All && !await store.PropertyExistsAsync(property, token)) Invalid("propertyId");
        }
        else if (!scope.All) Invalid("propertyId");
        string id;
        try { id = await keycloak.CreateAsync(draft, token); }
        catch (IdentityConflictException) { throw new HierarchyConflictException(ErrorCatalog.UserExists.Create()); }
        try
        {
            if (roles.Count > 0) await keycloak.ChangeRealmRolesAsync(id, roles, [], token);
            if (request.TemporaryPassword is not null) await keycloak.SetPasswordAsync(id, request.TemporaryPassword, true, token);
            await store.InitializeCreatedAccountAsync(request.PropertyId, id, draft.FirstName, draft.LastName, phone, token);
        }
        catch
        {
            // Avoid an orphan identity when the remaining steps fail; the caller retries the whole creation.
            try { await keycloak.DeleteAsync(id, CancellationToken.None); } catch (IdentityUnavailableException) { }
            throw;
        }
        var invitation = "NOT_REQUESTED";
        if (request.TemporaryPassword is null || emailPolicy?.Enabled == true)
        {
            try {
                await keycloak.SendActionsEmailAsync(id, emailPolicy?.Enabled == true ? ["VERIFY_EMAIL", "UPDATE_PASSWORD"] : ["UPDATE_PASSWORD"], token);
                invitation = "ACCEPTED"; // SMTP acceptance is not mailbox delivery or ownership proof.
            } catch (IdentityUnavailableException) { invitation = "UNAVAILABLE"; }
        }
        return (await GetAsync(id, token))! with { InvitationStatus = invitation };
    }

    public async Task<UserResponse?> UpdateAsync(string id, UpdateUserRequest request, CancellationToken token)
    {
        var scope = await Scope(token);
        var found = await Visible(id, scope, token); if (found is null) return null;
        var current = await keycloak.RealmRolesAsync(id, token);
        Editable(scope, id, current);
        var desired = Roles(request.Roles, scope);
        if (id == scope.ActorId && (!request.Enabled || scope.All && !desired.Contains("SUPER_ADMIN"))) Invalid("enabled", ErrorCatalog.UserSelfChange);
        var draft = Draft(found.Username, request.Email, request.FirstName, request.LastName, request.Enabled);
        // Only the account owner's native UPDATE_EMAIL flow may change an address once enabled.
        if (emailPolicy?.Enabled == true && !string.Equals(found.Email, draft.Email, StringComparison.OrdinalIgnoreCase)) Invalid("email");
        await keycloak.UpdateAsync(id, draft, token);
        var add = desired.Except(current).ToArray();
        var remove = current.Where(scope.ManagedRoles.Contains).Except(desired).ToArray();
        if (add.Length + remove.Length > 0) await keycloak.ChangeRealmRolesAsync(id, add, remove, token);
        return await GetAsync(id, token);
    }

    public async Task<bool> SendPasswordResetAsync(string id, CancellationToken token)
    {
        var scope = await Scope(token);
        if (await Visible(id, scope, token) is not { } found) return false;
        Editable(scope, id, await keycloak.RealmRolesAsync(id, token));
        if (string.IsNullOrWhiteSpace(found.Email)) Invalid("email");
        await keycloak.SendActionsEmailAsync(id, emailPolicy?.Enabled == true && !found.EmailVerified ? ["VERIFY_EMAIL", "UPDATE_PASSWORD"] : ["UPDATE_PASSWORD"], token);
        return true;
    }

    public async Task<UserResponse?> LinkAsync(string id, LinkPersonRequest request, CancellationToken token)
    {
        var scope = await Scope(token);
        if (request.PropertyId == Guid.Empty || request.PersonId == Guid.Empty || request.Version < 1) Invalid("personId");
        if (!scope.All && !scope.Properties.Contains(request.PropertyId)) return null;
        var found = scope.All ? await keycloak.GetAsync(id, token) : await Visible(id, scope, token);
        if (found is null) return null;
        if (!scope.All) Editable(scope, id, await keycloak.RealmRolesAsync(id, token));
        if (await store.LinkAsync(request.PropertyId, request.PersonId, request.Version, id, token) is null) return null;
        return await GetAsync(id, token);
    }

    public async Task<bool> UnlinkAsync(string id, Guid property, Guid person, long version, CancellationToken token)
    {
        var scope = await Scope(token);
        if (version < 1) Invalid("version");
        if (!scope.All && !scope.Properties.Contains(property)) return false;
        return await store.UnlinkAsync(property, person, version, id, token);
    }

    private async Task<KeycloakUser?> Visible(string id, UserScope scope, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 200) return null;
        if (!scope.All && (await store.MembersAmongAsync(scope.Properties, [id], token)).Count == 0) return null;
        return await keycloak.GetAsync(id, token);
    }

    private async Task<UserResponse[]> Describe(IReadOnlyList<KeycloakUser> users, UserScope scope, CancellationToken token)
    {
        if (users.Count == 0) return [];
        var roles = await Task.WhenAll(users.Select(u => keycloak.RealmRolesAsync(u.Id, token)));
        var links = await store.LinksAsync(users.Select(u => u.Id).ToArray(), scope.All ? null : scope.Properties, token);
        var photoIds = photos is null ? new Dictionary<string, Guid>() : await photos.CurrentPhotosAsync(users.Select(u => u.Id).ToArray(), token);
        return users.Select((u, i) =>
        {
            var own = roles[i].Where(UserRoles.All.Contains).Order(StringComparer.Ordinal).ToArray();
            var link = links.TryGetValue(u.Id, out var value) ? value : (0, []);
            var canEdit = scope.All || !own.Any(UserRoles.Privileged.Contains);
            return new UserResponse(u.Id, u.Username, u.Email, u.FirstName, u.LastName, u.Enabled, u.EmailVerified, u.CreatedAt, own, link.Item1, link.Item2, canEdit, photoIds.TryGetValue(u.Id, out var photo) ? photo : null, managedPhotos is not null && (u.Id == scope.ActorId || canEdit), CanEditEmail: emailPolicy?.Enabled != true);
        }).ToArray();
    }

    private static void Editable(UserScope scope, string id, IReadOnlyList<string> roles)
    {
        if (!scope.All && (roles.Any(UserRoles.Privileged.Contains) || id == scope.ActorId)) throw new UnauthorizedAccessException();
    }

    private static HashSet<string> Roles(IReadOnlyList<string>? requested, UserScope scope)
    {
        var roles = (requested ?? []).Select(r => r?.Trim() ?? "").ToHashSet(StringComparer.Ordinal);
        if (roles.Count > 5 || roles.Any(r => !scope.ManagedRoles.Contains(r))) Invalid("roles", ErrorCatalog.UserRoleForbidden);
        return roles;
    }

    private static KeycloakUserDraft Draft(string? username, string? email, string? firstName, string? lastName, bool enabled)
    {
        if (username is null || !UserRoles.Username().IsMatch(username)) Invalid("username");
        email = Clean(email);
        if (email is null || email.Length > 254 || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email) Invalid("email");
        firstName = Clean(firstName); lastName = Clean(lastName);
        if (firstName is null || firstName.Length > 100 || firstName.Any(char.IsControl)) Invalid("firstName");
        if (lastName is null || lastName.Length > 100 || lastName.Any(char.IsControl)) Invalid("lastName");
        return new(username, email.ToLowerInvariant(), PersonNames.Given(firstName)!, PersonNames.Family(lastName)!, enabled);
    }

    private static bool StrongPassword(string value) => value.Length is >= 12 and <= 128 && value.Any(char.IsAsciiLetterUpper) && value.Any(char.IsAsciiLetterLower) && value.Any(char.IsAsciiDigit);
}
