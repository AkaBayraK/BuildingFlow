using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record SmsTypeDefinition(string Code, [property: JsonConverter(typeof(JsonStringEnumConverter))] SmsPurpose Purpose, IReadOnlyList<string> Parameters, IReadOnlyList<string> SecretParameters, string DefaultContent,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] SmsCategory Category);
public sealed record SmsAccountResponse(Guid Id, Guid PropertyId, string Name, string Provider, string SenderHeader, string? Username, bool HasSecret,
    string? ApiUrl, int CreditBalance, int LowCreditThreshold, bool IsLowCredit, bool IsEnabled, long Version);
public sealed record SmsOverview(Guid PropertyId, SmsAccountResponse? Account, IReadOnlyList<SmsTypeDefinition> Types);
public sealed record SaveSmsAccountRequest(string? Name, string? Provider, string? SenderHeader, string? Username, string? Secret, string? ApiUrl,
    int LowCreditThreshold, bool IsEnabled = true, long Version = 0);
public sealed record SmsCreditRequest(string? Type, int Quantity, decimal? UnitPrice = null, string? Currency = null, string? ReferenceNo = null, string? Description = null);
public sealed record SmsCreditTransactionResponse(Guid Id, string Type, int Quantity, int BalanceAfter, decimal? UnitPrice, string? Currency,
    string? ReferenceNo, string? Description, Guid? MessageId, DateTimeOffset CreatedAt, string CreatedBy);
public sealed record SmsTemplateRequest(string? TypeCode, string? Name, string? Content, bool IsEnabled = true, long Version = 0);
public sealed record SmsTemplateResponse(Guid? Id, string TypeCode, string Name, string Content, string Purpose, bool IsEnabled, bool IsSystemDefault, int EstimatedSegments, long Version);
public sealed record SmsMessageResponse(Guid Id, string TypeCode, string Purpose, Guid? PersonId, string PhoneMasked, string Message, int Segments, int CreditCost,
    string Status, string? Provider, string? ProviderMessageId, string? ErrorCode, int Attempts, DateTimeOffset CreatedAt, DateTimeOffset? SentAt, string CreatedBy);
public sealed record SendSmsRequest(Guid RequestId, Guid? PersonId, string? PhoneNumber, string? Body);
public sealed record SmsQueueResult(Guid? MessageId, string Status, string? ErrorCode);
public sealed record SmsLength(string Encoding, int Characters, int Segments);
public sealed record SmsSendRequest(SmsAccount Account, string PhoneNumber, string Body);
public sealed record SmsSendResult(bool Success, string? ProviderMessageId, string? ErrorCode, bool Permanent);

public interface ISmsProtector
{
    string Protect(string plain);
    string Unprotect(string protectedValue);
}
public interface ISmsProvider
{
    SmsProvider Kind { get; }
    Task<SmsSendResult> SendAsync(SmsSendRequest request, CancellationToken token);
}
// Adds an SMS row to the caller's unit of work; the caller's SaveChanges commits it with the business write.
public interface ISmsQueue
{
    Task<SmsQueueResult> EnqueueAsync(Guid property, string typeCode, string? phoneNumber, IReadOnlyDictionary<string, string> parameters,
        Guid? personId, Guid? requestId, CancellationToken token);
}
public interface ISmsStore
{
    Task<SmsOverview?> OverviewAsync(Guid property, AccessScope access, CancellationToken token);
    Task<SmsAccountResponse?> SaveAccountAsync(Guid property, SaveSmsAccountRequest request, AccessScope access, CancellationToken token);
    Task<SmsCreditTransactionResponse?> AddCreditAsync(Guid property, SmsCreditRequest request, AccessScope access, CancellationToken token);
    Task<PagedRows<SmsCreditTransactionResponse>?> CreditsAsync(Guid property, int page, int pageSize, AccessScope access, CancellationToken token);
    Task<IReadOnlyList<SmsTemplateResponse>?> TemplatesAsync(Guid property, AccessScope access, CancellationToken token);
    Task<SmsTemplateResponse?> SaveTemplateAsync(Guid property, Guid? template, SmsTemplateRequest request, AccessScope access, CancellationToken token);
    Task<bool> DeleteTemplateAsync(Guid property, Guid template, long version, AccessScope access, CancellationToken token);
    Task<PagedRows<SmsMessageResponse>?> MessagesAsync(Guid property, int page, int pageSize, string? status, AccessScope access, CancellationToken token);
    Task<SmsMessageResponse?> SendAsync(Guid property, SendSmsRequest request, AccessScope access, CancellationToken token);
}

public static class SmsTypes
{
    public const string PersonRegistered = "PERSON_REGISTERED";
    public const string Manual = "MANUAL";
    public static IReadOnlyList<SmsTypeDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        new SmsTypeDefinition(PersonRegistered, SmsPurpose.TRANSACTIONAL, ["code", "fullName", "propertyName"], ["code"],
            "Sayın {{fullName}}, {{code}} kod ile kişi kaydınız kayıt altına alınmıştır. {{propertyName}}", SmsCategory.ACCOUNT_VERIFICATION),
        new SmsTypeDefinition(Manual, SmsPurpose.OPERATIONAL, ["message"], [], "{{message}}", SmsCategory.MAINTENANCE_NOTIFICATION)
    });
    public static SmsTypeDefinition? Find(string? code) => All.SingleOrDefault(type => type.Code == code);
    // Commercial types are only previewable until the campaign sending flow is published; they are not part of the operational overview.
    public static IReadOnlyList<SmsTypeDefinition> Marketing { get; } = Array.AsReadOnly(new[]
    {
        new SmsTypeDefinition("MARKETING", SmsPurpose.MARKETING, ["message"], [], "{{message}}", SmsCategory.MARKETING)
    });
}

public static partial class SmsText
{
    private const string GsmBasic = "@£$¥èéùìòÇ\nØø\rÅåΔ_ΦΓΛΩΠΨΣΘΞÆæßÉ !\"#¤%&'()*+,-./0123456789:;<=>?¡ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÑÜ§¿abcdefghijklmnopqrstuvwxyzäöñüà";
    private const string GsmExtended = "^{}\\[~]|€\f";
    public const int MaxSegments = 10;

    [GeneratedRegex(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholder();
    [GeneratedRegex(@"^\+[1-9][0-9]{7,14}$", RegexOptions.CultureInvariant)]
    private static partial Regex E164();

    public static IReadOnlyList<string> Placeholders(string content) => Placeholder().Matches(content).Select(match => match.Groups[1].Value).Distinct().ToArray();
    public static string Render(string content, IReadOnlyDictionary<string, string> values) =>
        Placeholder().Replace(content, match => values.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value);
    public static string Mask(string content, SmsTypeDefinition type, IReadOnlyDictionary<string, string> values) =>
        Render(content, values.ToDictionary(pair => pair.Key, pair => type.SecretParameters.Contains(pair.Key) ? new string('*', pair.Value.Length) : pair.Value));

    public static SmsLength Measure(string text) => new(IsGsm(text) ? "GSM7" : "UCS2", text.Length, Segments(text));
    public static bool IsGsm(string text) => text.All(c => GsmBasic.Contains(c) || GsmExtended.Contains(c));

    public static int Segments(string text)
    {
        if (text.Length == 0) return 0;
        var units = 0;
        foreach (var c in text)
        {
            if (GsmBasic.Contains(c)) units++;
            else if (GsmExtended.Contains(c)) units += 2;
            else return text.Length <= 70 ? 1 : (int)Math.Ceiling(text.Length / 67d);
        }
        return units <= 160 ? 1 : (int)Math.Ceiling(units / 153d);
    }

    // Accepts E.164 and common Turkish forms (0 5xx…, 5xx…, 0090…) and returns +<country><number>.
    public static string? NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = new StringBuilder();
        foreach (var c in value.Trim()) { if (char.IsAsciiDigit(c) || c == '+' && text.Length == 0) text.Append(c); else if (c is not (' ' or '(' or ')' or '-' or '.')) return null; }
        var phone = text.ToString();
        if (phone.StartsWith("00", StringComparison.Ordinal)) phone = "+" + phone[2..];
        else if (phone.StartsWith('0') && phone.Length == 11) phone = "+90" + phone[1..];
        else if (phone.StartsWith('5') && phone.Length == 10) phone = "+90" + phone;
        return E164().IsMatch(phone) ? phone : null;
    }
    public static string MaskPhone(string phone) => phone.Length <= 6 ? new string('*', phone.Length) : phone[..3] + new string('*', phone.Length - 5) + phone[^2..];
}

public sealed class SmsService(ISmsStore store, ICurrentUserService user)
{
    private AccessScope Access(bool admin = false)
    {
        var roles = admin ? new[] { "SUPER_ADMIN" } : new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER" };
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !roles.Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return new(user.UserId, user.IsInRole("SUPER_ADMIN"));
    }
    [DoesNotReturn] private static void Invalid(string field, ErrorDefinition? error = null) => throw new RequestValidationException(new() { [field] = [(error ?? ErrorCatalog.SmsInvalid).Create()] });
    private static void Page(int page, int size) { if (page is < 1 or > 1000000 || size is < 1 or > 100) throw new RequestValidationException(new() { ["page"] = [ErrorCatalog.InvalidPage.Create()] }); }

    public Task<SmsOverview?> OverviewAsync(Guid property, CancellationToken token) => store.OverviewAsync(property, Access(), token);
    public Task<SmsAccountResponse?> SaveAccountAsync(Guid property, SaveSmsAccountRequest request, CancellationToken token)
    {
        var access = Access(true);
        if (request.Provider is null || int.TryParse(request.Provider, out _) || !Enum.TryParse<SmsProvider>(request.Provider, false, out var provider) || !Enum.IsDefined(provider)) Invalid("provider");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100) Invalid("name");
        if (string.IsNullOrWhiteSpace(request.SenderHeader) || request.SenderHeader.Trim().Length is < 3 or > 11) Invalid("senderHeader");
        if (request.LowCreditThreshold is < 0 or > SmsAccount.MaxCredit) Invalid("lowCreditThreshold");
        if (request.Secret?.Length > 500 || request.Username?.Length > 100 || request.ApiUrl?.Length > 300) Invalid("secret");
        if (request.Version < 0) Invalid("version");
        return store.SaveAccountAsync(property, request, access, token);
    }
    public Task<SmsCreditTransactionResponse?> AddCreditAsync(Guid property, SmsCreditRequest request, CancellationToken token)
    {
        var access = Access(true);
        if (request.Type is not ("LOAD" or "ADJUST")) Invalid("type");
        if (request.Type == "LOAD" ? request.Quantity is < 1 or > SmsAccount.MaxCredit : request.Quantity == 0 || Math.Abs(request.Quantity) > SmsAccount.MaxCredit) Invalid("quantity");
        if (request.UnitPrice is < 0 or > 1_000_000 || (request.UnitPrice is null) != string.IsNullOrWhiteSpace(request.Currency)) Invalid("unitPrice");
        if (request.ReferenceNo?.Trim().Length > 100 || request.Description?.Trim().Length > 500) Invalid("description");
        return store.AddCreditAsync(property, request, access, token);
    }
    public Task<PagedRows<SmsCreditTransactionResponse>?> CreditsAsync(Guid property, int page, int pageSize, CancellationToken token)
    { var access = Access(); Page(page, pageSize); return store.CreditsAsync(property, page, pageSize, access, token); }
    public Task<IReadOnlyList<SmsTemplateResponse>?> TemplatesAsync(Guid property, CancellationToken token) => store.TemplatesAsync(property, Access(), token);
    public Task<SmsTemplateResponse?> SaveTemplateAsync(Guid property, Guid? template, SmsTemplateRequest request, CancellationToken token)
    {
        var access = Access();
        var type = SmsTypes.Find(request.TypeCode);
        if (type is null) Invalid("typeCode");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100) Invalid("name");
        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Trim().Length > SmsTemplate.MaxContentLength || request.Content.Any(c => char.IsControl(c) && c != '\n')) Invalid("content");
        if (SmsText.Placeholders(request.Content!).Except(type.Parameters).Any()) Invalid("content", ErrorCatalog.SmsTemplateParameterInvalid);
        if (template is not null && request.Version < 1) Invalid("version");
        return store.SaveTemplateAsync(property, template, request with { Name = request.Name!.Trim(), Content = request.Content!.Trim() }, access, token);
    }
    public Task<bool> DeleteTemplateAsync(Guid property, Guid template, long version, CancellationToken token)
    { var access = Access(); if (version < 1) Invalid("version"); return store.DeleteTemplateAsync(property, template, version, access, token); }
    public Task<PagedRows<SmsMessageResponse>?> MessagesAsync(Guid property, int page, int pageSize, string? status, CancellationToken token)
    {
        var access = Access(); Page(page, pageSize);
        if (status is not null && (int.TryParse(status, out _) || !Enum.TryParse<SmsMessageStatus>(status, false, out _))) Invalid("status");
        return store.MessagesAsync(property, page, pageSize, status, access, token);
    }
    public Task<SmsMessageResponse?> SendAsync(Guid property, SendSmsRequest request, CancellationToken token)
    {
        var access = Access();
        if (request.RequestId == Guid.Empty) Invalid("requestId");
        if ((request.PersonId is null) == string.IsNullOrWhiteSpace(request.PhoneNumber) || request.PersonId == Guid.Empty) Invalid("phoneNumber");
        if (request.PhoneNumber is not null && SmsText.NormalizePhone(request.PhoneNumber) is null) Invalid("phoneNumber", ErrorCatalog.SmsPhoneInvalid);
        if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Trim().Length > SmsTemplate.MaxContentLength || request.Body.Any(c => char.IsControl(c) && c != '\n')) Invalid("body");
        return store.SendAsync(property, request with { Body = request.Body!.Trim() }, access, token);
    }
}
