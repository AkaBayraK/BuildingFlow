using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record LegalSenderResponse(Guid Id, Guid? PropertyId, string Title, string? MersisNumber, string ContactInformation, string OptOutKeyword,
    string OptOutShortCode, bool OptOutEnabled, bool IysIntegrationEnabled, string? IysBrandCode, bool IsEnabled, long Version);
public sealed record LegalSenderView(LegalSenderResponse? Sender);
public sealed record SaveLegalSenderRequest(string? Title, string? MersisNumber, string? ContactInformation, string? OptOutKeyword, string? OptOutShortCode,
    bool OptOutEnabled, bool IysIntegrationEnabled = false, string? IysBrandCode = null, bool IsEnabled = true, long Version = 0);
public sealed record SmsPreviewRequest(string? TypeCode, string? Body, string? PhoneNumber = null);
public sealed record SmsPreviewResponse(string TypeCode, string Category, string Body, string Footer, string Encoding, int Characters, int Segments,
    bool Allowed, string? BlockedCode, Guid? LegalSenderId);
public sealed record AdminOptOutRequest(Guid OperationId, string? PhoneNumber);
public sealed record SmsOptOutCallback(string? PhoneNumber, string? Text, string? ShortCode, string? ProviderMessageId);
public sealed record SmsOptOutCallbackResult(int MatchedSenders);
public sealed record CommercialConsentText(int Version, string Title, string Body, string BodySha256);
public sealed record CommunicationPreferenceItem(Guid SenderId, string Scope, Guid? PropertyId, string SenderTitle, string Status, DateTimeOffset? OccurredAt, int? TextVersion);
public sealed record CommunicationPreferences(string? VerifiedPhoneMasked, CommercialConsentText Text, IReadOnlyList<CommunicationPreferenceItem> Items);
public sealed record CommunicationPreferenceCommand(Guid OperationId, bool Approved, int? TextVersion = null, string? TextSha256 = null, string? Channel = "WEB");

public static class CommercialConsentTexts
{
    public const int Version = 1;
    public const string Title = "Ticari elektronik ileti (SMS) onayı";
    public const string Body = "Kampanya, tanıtım ve yeni hizmet duyurularının doğrulanmış cep telefonuma SMS ile gönderilmesine onay veriyorum. " +
        "Bu onay isteğe bağlıdır, hizmetten yararlanma şartı değildir; aidat, bakım ve güvenlik bildirimlerini etkilemez. " +
        "Onayımı istediğim zaman bu ekrandan veya SMS'teki ücretsiz ret yöntemiyle geri çekebilirim.";
    public static CommercialConsentText Current { get; } = new(Version, Title, Body, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Body))));
}

public interface ISmsConsentStore
{
    Task<LegalSenderView?> LegalSenderAsync(Guid? property, AccessScope access, CancellationToken token);
    Task<LegalSenderResponse?> SaveLegalSenderAsync(Guid? property, SaveLegalSenderRequest request, AccessScope access, CancellationToken token);
    Task<SmsPreviewResponse?> PreviewAsync(Guid property, SmsTypeDefinition type, string body, string? phone, AccessScope access, CancellationToken token);
    Task<PagedRows<CommercialConsentState>?> ConsentsAsync(Guid property, int page, int pageSize, AccessScope access, CancellationToken token);
    Task<CommercialConsentState?> OptOutAsync(Guid property, Guid operationId, string phone, AccessScope access, CancellationToken token);
    Task<CommunicationPreferences> PreferencesAsync(string userId, CancellationToken token);
    Task<CommunicationPreferenceItem?> SetPreferenceAsync(string userId, Guid sender, CommunicationPreferenceCommand command, CancellationToken token);
    Task<PagedRows<CommercialConsentState>?> PreferenceHistoryAsync(string userId, Guid sender, int page, int pageSize, CancellationToken token);
    Task<int> InboundOptOutAsync(string phone, string text, string shortCode, string providerMessageId, CancellationToken token);
}

public sealed class SmsConsentService(ISmsConsentStore store, ICurrentUserService user)
{
    private AccessScope Access(params string[] roles)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !roles.Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return new(user.UserId, user.IsInRole("SUPER_ADMIN"));
    }
    private AccessScope Managers() => Access("SUPER_ADMIN", "SITE_ADMIN", "MANAGER");
    private string Self() => user.IsAuthenticated && !string.IsNullOrWhiteSpace(user.UserId) ? user.UserId : throw new UnauthorizedAccessException();
    [DoesNotReturn] private static void Invalid(string field, ErrorDefinition? error = null) => throw new RequestValidationException(new() { [field] = [(error ?? ErrorCatalog.SmsInvalid).Create()] });
    private static void Page(int page, int size) { if (page is < 1 or > 1000000 || size is < 1 or > 100) throw new RequestValidationException(new() { ["page"] = [ErrorCatalog.InvalidPage.Create()] }); }

    public Task<LegalSenderView?> LegalSenderAsync(Guid? property, CancellationToken token) =>
        store.LegalSenderAsync(property, property is null ? Access("SUPER_ADMIN") : Managers(), token);

    public Task<LegalSenderResponse?> SaveLegalSenderAsync(Guid? property, SaveLegalSenderRequest request, CancellationToken token)
    {
        var access = property is null ? Access("SUPER_ADMIN") : Access("SUPER_ADMIN", "SITE_ADMIN");
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200) Invalid("title");
        if (!string.IsNullOrWhiteSpace(request.MersisNumber) && (request.MersisNumber.Trim().Length != 16 || !request.MersisNumber.Trim().All(char.IsAsciiDigit))) Invalid("mersisNumber");
        if (string.IsNullOrWhiteSpace(request.ContactInformation) || request.ContactInformation.Trim().Length > 200) Invalid("contactInformation");
        var keyword = request.OptOutKeyword?.Trim() ?? "";
        if (keyword.Length is < 3 or > 20 || !keyword.StartsWith("RET", StringComparison.Ordinal) || keyword.Any(c => !char.IsAsciiLetterUpper(c) && !char.IsAsciiDigit(c) && c != ' ')) Invalid("optOutKeyword");
        var code = request.OptOutShortCode?.Trim() ?? "";
        if (code.Length is < 3 or > 15 || !code.All(char.IsAsciiDigit)) Invalid("optOutShortCode");
        if (request.IysIntegrationEnabled && (string.IsNullOrWhiteSpace(request.IysBrandCode) || request.IysBrandCode.Trim().Length > 20)) Invalid("iysBrandCode");
        if (request.Version < 0) Invalid("version");
        return store.SaveLegalSenderAsync(property, request, access, token);
    }

    public Task<SmsPreviewResponse?> PreviewAsync(Guid property, SmsPreviewRequest request, CancellationToken token)
    {
        var access = Managers();
        var type = SmsTypes.Find(request.TypeCode) ?? SmsTypes.Marketing.SingleOrDefault(x => x.Code == request.TypeCode);
        if (type is null) Invalid("typeCode");
        if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Trim().Length > SmsTemplate.MaxContentLength || request.Body.Any(c => char.IsControl(c) && c != '\n')) Invalid("body");
        string? phone = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && (phone = SmsText.NormalizePhone(request.PhoneNumber)) is null) Invalid("phoneNumber", ErrorCatalog.SmsPhoneInvalid);
        return store.PreviewAsync(property, type, request.Body!.Trim(), phone, access, token);
    }

    public Task<PagedRows<CommercialConsentState>?> ConsentsAsync(Guid property, int page, int pageSize, CancellationToken token)
    { var access = Managers(); Page(page, pageSize); return store.ConsentsAsync(property, page, pageSize, access, token); }

    public Task<CommercialConsentState?> OptOutAsync(Guid property, AdminOptOutRequest request, CancellationToken token)
    {
        var access = Managers();
        if (request.OperationId == Guid.Empty) Invalid("operationId");
        var phone = SmsText.NormalizePhone(request.PhoneNumber);
        if (phone is null) Invalid("phoneNumber", ErrorCatalog.SmsPhoneInvalid);
        return store.OptOutAsync(property, request.OperationId, phone, access, token);
    }

    public Task<CommunicationPreferences> PreferencesAsync(CancellationToken token) => store.PreferencesAsync(Self(), token);

    public Task<CommunicationPreferenceItem?> SetPreferenceAsync(Guid sender, CommunicationPreferenceCommand command, CancellationToken token)
    {
        var self = Self();
        if (command.OperationId == Guid.Empty) Invalid("operationId");
        if (command.Channel is not ("WEB" or "MOBILE")) Invalid("channel");
        if (command.Approved && (command.TextVersion != CommercialConsentTexts.Version || command.TextSha256 != CommercialConsentTexts.Current.BodySha256))
            throw new HierarchyConflictException(ErrorCatalog.ConsentTextOutdated.Create());
        return store.SetPreferenceAsync(self, sender, command, token);
    }

    // Called only after the provider signature is verified; it never needs a signed-in user.
    public async Task<SmsOptOutCallbackResult> InboundOptOutAsync(SmsOptOutCallback callback, CancellationToken token)
    {
        var phone = SmsText.NormalizePhone(callback.PhoneNumber);
        if (phone is null) Invalid("phoneNumber", ErrorCatalog.SmsPhoneInvalid);
        var code = callback.ShortCode?.Trim() ?? "";
        if (code.Length is < 3 or > 15 || !code.All(char.IsAsciiDigit)) Invalid("shortCode");
        var id = callback.ProviderMessageId?.Trim() ?? "";
        if (id.Length is < 1 or > 100 || id.Any(char.IsControl)) Invalid("providerMessageId");
        if (callback.Text is not { Length: <= 160 } || !SmsOptOutKeyword.IsOptOut(callback.Text)) return new(0);
        return new(await store.InboundOptOutAsync(phone, callback.Text, code, id, token));
    }

    public Task<PagedRows<CommercialConsentState>?> PreferenceHistoryAsync(Guid sender, int page, int pageSize, CancellationToken token)
    { var self = Self(); Page(page, pageSize); return store.PreferenceHistoryAsync(self, sender, page, pageSize, token); }
}
