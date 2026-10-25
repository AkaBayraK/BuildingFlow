using System.Globalization;
using System.Text.RegularExpressions;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record PaymentActor(AccessScope Access, bool CanPropose, bool CanApprove);

public sealed record PaymentProviderResponse(string Code, string Name, bool Integrated, bool Enabled, bool SupportsRefund, string Environment, long Version);
public sealed record UpdatePaymentProviderRequest(bool Enabled, long Version);

public sealed record PaymentSettingsResponse(Guid PropertyId, bool OnlineEnabled, bool PartialAllowed, bool ShowIbanToResidents, bool RequireDualApproval,
    string ReceiptPrefix, string CommissionPolicy, long Version, bool CanEdit, bool CanPropose);
public sealed record PaymentSettingsRequest(bool OnlineEnabled, bool PartialAllowed, bool ShowIbanToResidents, bool RequireDualApproval,
    string? ReceiptPrefix, string? CommissionPolicy, long Version);

public sealed record BankAccountRequest(Guid RequestId, Guid? BuildingId, string? BankName, string? AccountHolder, string? Iban,
    string? Purpose, DateOnly? ValidFrom, DateOnly? ValidTo);
public sealed record BankAccountCommand(Guid RequestId, Guid? BuildingId, string BankName, string AccountHolder, string Iban,
    BankAccountPurpose Purpose, DateOnly ValidFrom, DateOnly? ValidTo);
public sealed record BankAccountResponse(Guid Id, Guid? BuildingId, string? BuildingName, string BankName, string AccountHolder, string Iban,
    string Currency, string Purpose, bool IsDefault, DateOnly ValidFrom, DateOnly? ValidTo, string Status, bool CreatedByMe,
    DateTimeOffset CreatedAt, DateTimeOffset? DecidedAt, string? StatusReason, long Version, bool Replayed = false);
public sealed record BankAccountDecisionRequest(string? Decision, string? Reason, long Version);
public sealed record BankAccountCloseRequest(DateOnly? ValidTo, string? Reason, long Version);
public sealed record VersionRequest(long Version);

public sealed record MerchantAccountRequest(Guid RequestId, Guid? BuildingId, string? ProviderCode, string? Label, string? MerchantId,
    string? SecretReference, string? Environment);
public sealed record MerchantAccountResponse(Guid Id, Guid? BuildingId, string? BuildingName, string ProviderCode, string Label, string MerchantId,
    string SecretReference, bool SecretConfigured, string Environment, bool IsDefault, bool IsEnabled, DateTimeOffset? LastCheckedAt,
    bool? LastCheckSucceeded, long Version, bool Replayed = false);

public sealed record IntentLineResponse(Guid ChargeId, string Period, string ChargeKind, string? ChargeCategory, string Description, string Kind, string Amount);
public sealed record IntentEventResponse(string EventType, string? Outcome, DateTimeOffset OccurredAt, DateTimeOffset? ProcessedAt);
public sealed record RefundResponse(Guid Id, Guid IntentId, string Amount, string Reason, string Status, bool RequestedByMe, DateTimeOffset CreatedAt,
    DateTimeOffset? DecidedAt, string? DecisionNote, string? FailureCode, long Version, bool Replayed = false);
public sealed record ReconciliationItemResponse(Guid Id, Guid? IntentId, string Type, string Status, string Detail, DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt, string? ResolutionNote, long Version);
public sealed record OnlinePaymentResponse(Guid Id, Guid UnitId, string UnitNumber, string? BuildingName, string Amount, string Currency, string Status,
    string ProviderCode, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, DateTimeOffset StatusChangedAt, DateOnly? PaidDate, string? FeeAmount,
    Guid? PaymentId, string? ReceiptNumber, string? FailureCode, long Version, IReadOnlyList<IntentLineResponse> Lines,
    IReadOnlyList<IntentEventResponse>? Events = null, IReadOnlyList<RefundResponse>? Refunds = null, IReadOnlyList<ReconciliationItemResponse>? Issues = null);
public sealed record OnlinePaymentQuery(int Page = 1, int PageSize = 10, string? Status = null, Guid? UnitId = null, DateOnly? From = null, DateOnly? To = null);
public sealed record OnlinePaymentSummary(string SucceededAmount, string ReviewAmount, string RefundedAmount, string FeeAmount, int PendingCount, int ReviewCount, string Currency);
public sealed record OnlinePaymentPage(IReadOnlyList<OnlinePaymentResponse> Items, int Page, int PageSize, int TotalCount, OnlinePaymentSummary Summary);
public sealed record RefundRequest(Guid RequestId, string? Reason);
public sealed record RefundDecisionRequest(string? Decision, string? Note, long Version);
public sealed record ResolveItemRequest(string? Note, long Version);

public sealed record LedgerEntryResponse(Guid Id, Guid UnitId, string UnitNumber, string Type, string Amount, DateOnly OccurredOn, string Method,
    Guid? PaymentId, Guid? IntentId, Guid? RefundId, string? ReceiptNumber, DateTimeOffset CreatedAt);
public sealed record LedgerTotals(string Credits, string Debits, string Net, string Currency);
public sealed record LedgerPage(IReadOnlyList<LedgerEntryResponse> Items, int Page, int PageSize, int TotalCount, LedgerTotals Totals);
public sealed record SettlementDay(DateOnly Date, int PaymentCount, string Collected, string Fees, string Refunded, string ChargedBack, string Net);
public sealed record SettlementReport(Guid PropertyId, DateOnly From, DateOnly To, string Currency, IReadOnlyList<SettlementDay> Days, int OpenIssues);
public sealed record ReconciliationRunRequest(DateOnly? Date);
public sealed record ReconciliationRunResult(int Checked, int Settled, int Expired, int IssuesOpened);

public sealed record ReceiptLineResponse(string Period, string ChargeKind, string? ChargeCategory, string Description, string Kind, string Amount);
public sealed record PaymentReceiptResponse(string Number, DateTimeOffset IssuedAt, string PropertyName, string UnitNumber, string? BuildingName,
    DateOnly PaidDate, string Method, string Amount, string Currency, string Status, IReadOnlyList<ReceiptLineResponse> Lines);

public sealed record ResidentContext(Guid PropertyId, Guid UnitId, string Role);
public sealed record ResidentBankAccount(string BankName, string AccountHolder, string Iban, string Purpose);
public sealed record PayableCharge(Guid ChargeId, string Period, string ChargeKind, string? ChargeCategory, string Description, DateOnly DueDate,
    string Principal, string Penalty, string Total);
public sealed record ResidentPaymentOptions(Guid PropertyId, Guid UnitId, bool OnlineEnabled, bool PartialAllowed, string? ProviderName,
    string ReferenceHint, IReadOnlyList<ResidentBankAccount> BankAccounts, IReadOnlyList<PayableCharge> Charges, DateTimeOffset AsOf);
public sealed record CreateIntentRequest(Guid RequestId, Guid PropertyId, Guid UnitId, string? Role, IReadOnlyList<Guid>? ChargeIds, string? Amount);
public sealed record IntentCommand(Guid RequestId, ResidentContext Context, IReadOnlyList<Guid> ChargeIds, decimal? Amount);
public sealed record ResidentIntentResponse(Guid Id, string Status, string Amount, string Currency, DateTimeOffset ExpiresAt, string? CheckoutUrl,
    DateOnly? PaidDate, Guid? PaymentId, string? ReceiptNumber, string? FailureCode, DateTimeOffset CreatedAt, IReadOnlyList<IntentLineResponse> Lines, bool Replayed = false);
public sealed record ResidentIntentPage(IReadOnlyList<ResidentIntentResponse> Items, int Page, int Total);

public enum ProviderPaymentState { PENDING, SUCCEEDED, FAILED, EXPIRED, REFUNDED, CHARGED_BACK, UNKNOWN }
public sealed record ProviderMerchant(string MerchantId, string SecretReference);
public sealed record ProviderCheckoutRequest(Guid IntentId, ProviderMerchant Merchant, decimal Amount, string Currency, string Description, DateTimeOffset ExpiresAt);
public sealed record ProviderCheckout(string Reference, string CheckoutUrl);
public sealed record ProviderPaymentStatus(ProviderPaymentState State, string? TransactionId, decimal? Amount, decimal? Fee, DateTimeOffset? CompletedAt, string? FailureCode);
public sealed record ProviderWebhookEvent(string EventId, string EventType, string Reference, DateTimeOffset OccurredAt);
public sealed record ProviderSettlementLine(string Reference, string Kind, decimal Amount, decimal Fee, DateOnly Date);
public sealed class PaymentProviderException(string code) : Exception("Payment provider call failed: " + code)
{
    public string Code { get; } = code;
}

// Provider-neutral port: card data never passes through BuildingFlow, only hosted checkout references.
public interface IPaymentProviderAdapter
{
    string Code { get; }
    string DisplayName { get; }
    bool SupportsRefund { get; }
    Task<ProviderCheckout> CreateCheckoutAsync(ProviderCheckoutRequest request, CancellationToken token);
    Task<ProviderPaymentStatus> GetStatusAsync(ProviderMerchant merchant, string reference, CancellationToken token);
    Task<string> RefundAsync(ProviderMerchant merchant, string reference, decimal amount, CancellationToken token);
    ProviderWebhookEvent? VerifyWebhook(IReadOnlyDictionary<string, string> headers, ReadOnlySpan<byte> body, DateTimeOffset now);
    Task<bool> TestConnectionAsync(ProviderMerchant merchant, CancellationToken token);
    Task<IReadOnlyList<ProviderSettlementLine>> SettlementAsync(ProviderMerchant merchant, DateOnly date, CancellationToken token);
}

public interface IPaymentSecretResolver { bool Has(string reference); }

public interface IPaymentConfigurationStore
{
    Task<IReadOnlyList<PaymentProviderResponse>> ProvidersAsync(CancellationToken token);
    Task<PaymentProviderResponse?> UpdateProviderAsync(string code, UpdatePaymentProviderRequest request, CancellationToken token);
    Task<PaymentSettingsResponse?> SettingsAsync(Guid property, PaymentActor actor, CancellationToken token);
    Task<PaymentSettingsResponse?> UpdateSettingsAsync(Guid property, PaymentSettingsRequest request, PaymentActor actor, CancellationToken token);
    Task<PagedRows<BankAccountResponse>?> BankAccountsAsync(Guid property, int page, int pageSize, string? status, PaymentActor actor, CancellationToken token);
    Task<BankAccountResponse?> CreateBankAccountAsync(Guid property, BankAccountCommand command, PaymentActor actor, CancellationToken token);
    Task<BankAccountResponse?> DecideBankAccountAsync(Guid property, Guid account, bool approve, string? reason, long version, PaymentActor actor, CancellationToken token);
    Task<BankAccountResponse?> CloseBankAccountAsync(Guid property, Guid account, DateOnly validTo, string reason, long version, PaymentActor actor, CancellationToken token);
    Task<BankAccountResponse?> DefaultBankAccountAsync(Guid property, Guid account, long version, PaymentActor actor, CancellationToken token);
    Task<PagedRows<MerchantAccountResponse>?> MerchantsAsync(Guid property, int page, int pageSize, PaymentActor actor, CancellationToken token);
    Task<MerchantAccountResponse?> CreateMerchantAsync(Guid property, MerchantAccountRequest request, PaymentActor actor, CancellationToken token);
    Task<MerchantAccountResponse?> DefaultMerchantAsync(Guid property, Guid merchant, long version, PaymentActor actor, CancellationToken token);
    Task<MerchantAccountResponse?> DeactivateMerchantAsync(Guid property, Guid merchant, long version, PaymentActor actor, CancellationToken token);
    Task<MerchantAccountResponse?> TestMerchantAsync(Guid property, Guid merchant, PaymentActor actor, CancellationToken token);
}

public interface IOnlinePaymentStore
{
    Task<ResidentPaymentOptions> OptionsAsync(ResidentContext context, CancellationToken token);
    Task<ResidentIntentResponse> CreateIntentAsync(IntentCommand command, CancellationToken token);
    Task<ResidentIntentResponse?> ResidentIntentAsync(ResidentContext context, Guid intent, bool cancel, CancellationToken token);
    Task<ResidentIntentPage> ResidentIntentsAsync(ResidentContext context, int page, CancellationToken token);
    Task<PaymentReceiptResponse?> ResidentReceiptAsync(ResidentContext context, Guid payment, CancellationToken token);
    Task<bool> HandleWebhookAsync(string provider, IReadOnlyDictionary<string, string> headers, byte[] body, CancellationToken token);
    Task<OnlinePaymentPage?> ListAsync(Guid property, OnlinePaymentQuery query, PaymentActor actor, CancellationToken token);
    Task<OnlinePaymentResponse?> GetAsync(Guid property, Guid intent, PaymentActor actor, CancellationToken token);
    Task<OnlinePaymentResponse?> SettleReviewAsync(Guid property, Guid intent, long version, PaymentActor actor, CancellationToken token);
    Task<RefundResponse?> RequestRefundAsync(Guid property, Guid intent, RefundRequest request, PaymentActor actor, CancellationToken token);
    Task<RefundResponse?> DecideRefundAsync(Guid property, Guid refund, bool approve, string? note, long version, PaymentActor actor, CancellationToken token);
    Task<PagedRows<ReconciliationItemResponse>?> IssuesAsync(Guid property, string? status, int page, int pageSize, PaymentActor actor, CancellationToken token);
    Task<ReconciliationItemResponse?> ResolveIssueAsync(Guid property, Guid item, string note, long version, PaymentActor actor, CancellationToken token);
    Task<ReconciliationRunResult?> ReconcileAsync(Guid property, DateOnly date, PaymentActor actor, CancellationToken token);
    Task<SettlementReport?> SettlementAsync(Guid property, DateOnly from, DateOnly to, PaymentActor actor, CancellationToken token);
    Task<LedgerPage?> LedgerAsync(Guid property, DateOnly? from, DateOnly? to, int page, int pageSize, PaymentActor actor, CancellationToken token);
    Task<PaymentReceiptResponse?> ReceiptAsync(Guid property, Guid payment, PaymentActor actor, CancellationToken token);
}

public interface IOnlinePaymentScheduler
{
    Task<int> SyncPendingAsync(CancellationToken token);
}

public sealed class OnlinePaymentService(IPaymentConfigurationStore config, IOnlinePaymentStore store, ICurrentUserService user)
{
    private static readonly string[] FinanceRoles = ["SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT"];
    private PaymentActor Actor()
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !FinanceRoles.Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return new(new(user.UserId, user.IsInRole("SUPER_ADMIN")), new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER" }.Any(user.IsInRole),
            new[] { "SUPER_ADMIN", "SITE_ADMIN" }.Any(user.IsInRole));
    }
    private PaymentActor Proposer() { var actor = Actor(); return actor.CanPropose ? actor : throw new UnauthorizedAccessException(); }
    private PaymentActor Approver() { var actor = Actor(); return actor.CanApprove ? actor : throw new UnauthorizedAccessException(); }
    private void SystemAdmin()
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !user.IsInRole("SUPER_ADMIN")) throw new UnauthorizedAccessException();
    }
    private static RequestValidationException Invalid(string field, ErrorDefinition error) => new(new() { [field] = [error.Create()] });
    private static void Paging(int page, int pageSize)
    {
        if (page is < 1 or > 1000000 || pageSize is < 1 or > 100) throw Invalid("page", ErrorCatalog.InvalidPage);
    }
    private static string Text(string? value, int max, string field, ErrorDefinition error) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Length > max ? throw Invalid(field, error) : value.Trim();
    private static bool Decision(string? value, string field, ErrorDefinition error) =>
        value switch { "APPROVE" => true, "REJECT" => false, _ => throw Invalid(field, error) };
    public static bool TryMoney(string? text, out decimal amount)
    {
        amount = 0;
        return text is { Length: <= 20 } && Regex.IsMatch(text, @"^[0-9]+(?:\.[0-9]{1,2})?$") &&
            decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) && amount > 0 && amount <= 99999999.99m;
    }

    public Task<IReadOnlyList<PaymentProviderResponse>> ProvidersAsync(CancellationToken token) { Actor(); return config.ProvidersAsync(token); }
    public Task<PaymentProviderResponse?> UpdateProviderAsync(string code, UpdatePaymentProviderRequest request, CancellationToken token)
    {
        SystemAdmin();
        if (request.Version < 0) throw Invalid("version", ErrorCatalog.VersionRequired);
        return config.UpdateProviderAsync(code.Trim().ToUpperInvariant(), request, token);
    }
    public Task<PaymentSettingsResponse?> SettingsAsync(Guid property, CancellationToken token) => config.SettingsAsync(property, Actor(), token);
    public Task<PaymentSettingsResponse?> UpdateSettingsAsync(Guid property, PaymentSettingsRequest request, CancellationToken token)
    {
        var actor = Approver();
        var prefix = request.ReceiptPrefix?.Trim().ToUpperInvariant();
        if (prefix is null || !Regex.IsMatch(prefix, "^[A-Z0-9]{2,10}$")) throw Invalid("receiptPrefix", ErrorCatalog.PaymentSettingsInvalid);
        if (request.CommissionPolicy != nameof(CommissionPolicy.ABSORBED)) throw Invalid("commissionPolicy", ErrorCatalog.PaymentSettingsInvalid);
        if (request.Version < 0) throw Invalid("version", ErrorCatalog.VersionRequired);
        return config.UpdateSettingsAsync(property, request with { ReceiptPrefix = prefix }, actor, token);
    }
    public Task<PagedRows<BankAccountResponse>?> BankAccountsAsync(Guid property, int page, int pageSize, string? status, CancellationToken token)
    {
        var actor = Actor(); Paging(page, pageSize);
        if (status is not null && !Enum.TryParse<BankAccountStatus>(status, false, out _)) throw Invalid("status", ErrorCatalog.PaymentBankAccountInvalid);
        return config.BankAccountsAsync(property, page, pageSize, status, actor, token);
    }
    public Task<BankAccountResponse?> CreateBankAccountAsync(Guid property, BankAccountRequest request, CancellationToken token)
    {
        var actor = Proposer(); Dictionary<string, ApiErrorMessage[]> errors = [];
        if (request.RequestId == Guid.Empty) errors["requestId"] = [ErrorCatalog.DuesErrorRequestId.Create()];
        if (request.BuildingId == Guid.Empty) errors["buildingId"] = [ErrorCatalog.PaymentBankAccountInvalid.Create()];
        if (string.IsNullOrWhiteSpace(request.BankName) || request.BankName.Trim().Length > 100) errors["bankName"] = [ErrorCatalog.PaymentBankAccountInvalid.Create()];
        if (string.IsNullOrWhiteSpace(request.AccountHolder) || request.AccountHolder.Trim().Length > 200) errors["accountHolder"] = [ErrorCatalog.PaymentBankAccountInvalid.Create()];
        var iban = Iban.Normalize(request.Iban);
        if (!Iban.IsValid(iban)) errors["iban"] = [ErrorCatalog.PaymentIbanInvalid.Create()];
        if (!Enum.TryParse<BankAccountPurpose>(request.Purpose, false, out var purpose) || !Enum.IsDefined(purpose)) errors["purpose"] = [ErrorCatalog.PaymentBankAccountInvalid.Create()];
        if (request.ValidFrom is null || request.ValidFrom.Value.Year < 2000 || request.ValidTo < request.ValidFrom) errors["validFrom"] = [ErrorCatalog.PaymentBankAccountInvalid.Create()];
        if (errors.Count > 0) throw new RequestValidationException(errors);
        return config.CreateBankAccountAsync(property, new(request.RequestId, request.BuildingId, request.BankName!.Trim(), request.AccountHolder!.Trim(),
            iban, purpose, request.ValidFrom!.Value, request.ValidTo), actor, token);
    }
    public Task<BankAccountResponse?> DecideBankAccountAsync(Guid property, Guid account, BankAccountDecisionRequest request, CancellationToken token)
    {
        var actor = Approver(); var approve = Decision(request.Decision, "decision", ErrorCatalog.PaymentBankAccountInvalid);
        var reason = approve ? null : Text(request.Reason, 500, "reason", ErrorCatalog.DuesErrorReason);
        return config.DecideBankAccountAsync(property, account, approve, reason, request.Version, actor, token);
    }
    public Task<BankAccountResponse?> CloseBankAccountAsync(Guid property, Guid account, BankAccountCloseRequest request, CancellationToken token)
    {
        var actor = Approver();
        if (request.ValidTo is null) throw Invalid("validTo", ErrorCatalog.PaymentBankAccountInvalid);
        return config.CloseBankAccountAsync(property, account, request.ValidTo.Value, Text(request.Reason, 500, "reason", ErrorCatalog.DuesErrorReason), request.Version, actor, token);
    }
    public Task<BankAccountResponse?> DefaultBankAccountAsync(Guid property, Guid account, VersionRequest request, CancellationToken token) =>
        config.DefaultBankAccountAsync(property, account, request.Version, Approver(), token);
    public Task<PagedRows<MerchantAccountResponse>?> MerchantsAsync(Guid property, int page, int pageSize, CancellationToken token)
    {
        var actor = Actor(); Paging(page, pageSize); return config.MerchantsAsync(property, page, pageSize, actor, token);
    }
    public Task<MerchantAccountResponse?> CreateMerchantAsync(Guid property, MerchantAccountRequest request, CancellationToken token)
    {
        var actor = Proposer(); Dictionary<string, ApiErrorMessage[]> errors = [];
        if (request.RequestId == Guid.Empty) errors["requestId"] = [ErrorCatalog.DuesErrorRequestId.Create()];
        if (request.BuildingId == Guid.Empty) errors["buildingId"] = [ErrorCatalog.PaymentMerchantInvalid.Create()];
        if (string.IsNullOrWhiteSpace(request.ProviderCode) || request.ProviderCode.Trim().Length > 30) errors["providerCode"] = [ErrorCatalog.PaymentMerchantInvalid.Create()];
        if (string.IsNullOrWhiteSpace(request.Label) || request.Label.Trim().Length > 100) errors["label"] = [ErrorCatalog.PaymentMerchantInvalid.Create()];
        if (string.IsNullOrWhiteSpace(request.MerchantId) || request.MerchantId.Trim().Length > 100) errors["merchantId"] = [ErrorCatalog.PaymentMerchantInvalid.Create()];
        if (request.SecretReference is null || !Regex.IsMatch(request.SecretReference, "^[A-Za-z0-9_:-]{1,100}$")) errors["secretReference"] = [ErrorCatalog.PaymentMerchantInvalid.Create()];
        if (!Enum.TryParse<PaymentEnvironment>(request.Environment, false, out var environment) || !Enum.IsDefined(environment)) errors["environment"] = [ErrorCatalog.PaymentMerchantInvalid.Create()];
        if (errors.Count > 0) throw new RequestValidationException(errors);
        if (environment != PaymentEnvironment.SANDBOX) throw new HierarchyConflictException(ErrorCatalog.PaymentLiveDisabled.Create());
        return config.CreateMerchantAsync(property, request with { ProviderCode = request.ProviderCode!.Trim().ToUpperInvariant(), Label = request.Label!.Trim(), MerchantId = request.MerchantId!.Trim() }, actor, token);
    }
    public Task<MerchantAccountResponse?> DefaultMerchantAsync(Guid property, Guid merchant, VersionRequest request, CancellationToken token) =>
        config.DefaultMerchantAsync(property, merchant, request.Version, Approver(), token);
    public Task<MerchantAccountResponse?> DeactivateMerchantAsync(Guid property, Guid merchant, VersionRequest request, CancellationToken token) =>
        config.DeactivateMerchantAsync(property, merchant, request.Version, Approver(), token);
    public Task<MerchantAccountResponse?> TestMerchantAsync(Guid property, Guid merchant, CancellationToken token) =>
        config.TestMerchantAsync(property, merchant, Proposer(), token);

    public Task<OnlinePaymentPage?> ListAsync(Guid property, OnlinePaymentQuery query, CancellationToken token)
    {
        var actor = Actor(); Paging(query.Page, query.PageSize);
        if (query.Status is not null && !Enum.TryParse<PaymentIntentStatus>(query.Status, false, out _)) throw Invalid("status", ErrorCatalog.PaymentsErrorStatus);
        if (query.UnitId == Guid.Empty) throw Invalid("unitId", ErrorCatalog.DuesErrorUnit);
        if (query.From > query.To) throw Invalid("to", ErrorCatalog.PaymentsErrorDateRange);
        return store.ListAsync(property, query, actor, token);
    }
    public Task<OnlinePaymentResponse?> GetAsync(Guid property, Guid intent, CancellationToken token) => store.GetAsync(property, intent, Actor(), token);
    public Task<OnlinePaymentResponse?> SettleReviewAsync(Guid property, Guid intent, VersionRequest request, CancellationToken token) =>
        store.SettleReviewAsync(property, intent, request.Version, Proposer(), token);
    public Task<RefundResponse?> RequestRefundAsync(Guid property, Guid intent, RefundRequest request, CancellationToken token)
    {
        var actor = Proposer();
        if (request.RequestId == Guid.Empty) throw Invalid("requestId", ErrorCatalog.PaymentRefundInvalid);
        return store.RequestRefundAsync(property, intent, request with { Reason = Text(request.Reason, 500, "reason", ErrorCatalog.PaymentRefundInvalid) }, actor, token);
    }
    public Task<RefundResponse?> DecideRefundAsync(Guid property, Guid refund, RefundDecisionRequest request, CancellationToken token)
    {
        var actor = Approver(); var approve = Decision(request.Decision, "decision", ErrorCatalog.PaymentRefundInvalid);
        var note = approve ? request.Note?.Trim() : Text(request.Note, 500, "note", ErrorCatalog.DuesErrorReason);
        if (note?.Length > 500) throw Invalid("note", ErrorCatalog.DuesErrorReason);
        return store.DecideRefundAsync(property, refund, approve, note, request.Version, actor, token);
    }
    public Task<PagedRows<ReconciliationItemResponse>?> IssuesAsync(Guid property, string? status, int page, int pageSize, CancellationToken token)
    {
        var actor = Actor(); Paging(page, pageSize);
        if (status is not null && !Enum.TryParse<ReconciliationIssueStatus>(status, false, out _)) throw Invalid("status", ErrorCatalog.PaymentsErrorStatus);
        return store.IssuesAsync(property, status, page, pageSize, actor, token);
    }
    public Task<ReconciliationItemResponse?> ResolveIssueAsync(Guid property, Guid item, ResolveItemRequest request, CancellationToken token) =>
        store.ResolveIssueAsync(property, item, Text(request.Note, 500, "note", ErrorCatalog.DuesErrorReason), request.Version, Proposer(), token);
    public Task<ReconciliationRunResult?> ReconcileAsync(Guid property, ReconciliationRunRequest request, DateOnly today, CancellationToken token)
    {
        var actor = Proposer(); var date = request.Date ?? today;
        if (date > today || date.Year < 2000) throw Invalid("date", ErrorCatalog.PaymentsErrorDate);
        return store.ReconcileAsync(property, date, actor, token);
    }
    public Task<SettlementReport?> SettlementAsync(Guid property, DateOnly? from, DateOnly? to, DateOnly today, CancellationToken token)
    {
        var actor = Actor(); var end = to ?? today; var start = from ?? end.AddDays(-30);
        if (start > end || end.DayNumber - start.DayNumber > 366 || start.Year < 2000) throw Invalid("to", ErrorCatalog.PaymentsErrorDateRange);
        return store.SettlementAsync(property, start, end, actor, token);
    }
    public Task<LedgerPage?> LedgerAsync(Guid property, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken token)
    {
        var actor = Actor(); Paging(page, pageSize);
        if (from > to) throw Invalid("to", ErrorCatalog.PaymentsErrorDateRange);
        return store.LedgerAsync(property, from, to, page, pageSize, actor, token);
    }
    public Task<PaymentReceiptResponse?> ReceiptAsync(Guid property, Guid payment, CancellationToken token) => store.ReceiptAsync(property, payment, Actor(), token);
}

// Residents act only through their mobile session context; the store re-checks that context on every call.
public sealed class ResidentPaymentService(IOnlinePaymentStore store, ICurrentUserService user)
{
    private ResidentContext Context(Guid property, Guid unit, string? role)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId)) throw new UnauthorizedAccessException();
        if (property == Guid.Empty || unit == Guid.Empty || role is not ("OWNER" or "TENANT"))
            throw new RequestValidationException(new() { ["role"] = [ErrorCatalog.PaymentIntentInvalid.Create()] });
        return new(property, unit, role);
    }
    public Task<ResidentPaymentOptions> OptionsAsync(Guid property, Guid unit, string? role, CancellationToken token) => store.OptionsAsync(Context(property, unit, role), token);
    public Task<ResidentIntentResponse> CreateIntentAsync(CreateIntentRequest request, CancellationToken token)
    {
        var context = Context(request.PropertyId, request.UnitId, request.Role);
        decimal? amount = null;
        if (request.RequestId == Guid.Empty || request.ChargeIds is null || request.ChargeIds.Count is < 1 or > 50 || request.ChargeIds.Contains(Guid.Empty) ||
            request.ChargeIds.Distinct().Count() != request.ChargeIds.Count)
            throw new RequestValidationException(new() { ["chargeIds"] = [ErrorCatalog.PaymentIntentInvalid.Create()] });
        if (request.Amount is not null)
        {
            if (!OnlinePaymentService.TryMoney(request.Amount, out var value)) throw new RequestValidationException(new() { ["amount"] = [ErrorCatalog.PaymentPartialAmount.Create()] });
            amount = value;
        }
        return store.CreateIntentAsync(new(request.RequestId, context, request.ChargeIds.Order().ToArray(), amount), token);
    }
    public Task<ResidentIntentResponse?> IntentAsync(Guid property, Guid unit, string? role, Guid intent, bool cancel, CancellationToken token) =>
        store.ResidentIntentAsync(Context(property, unit, role), intent, cancel, token);
    public Task<ResidentIntentPage> IntentsAsync(Guid property, Guid unit, string? role, int page, CancellationToken token)
    {
        var context = Context(property, unit, role);
        if (page is < 1 or > 10000) throw new RequestValidationException(new() { ["page"] = [ErrorCatalog.InvalidPage.Create()] });
        return store.ResidentIntentsAsync(context, page, token);
    }
    public Task<PaymentReceiptResponse?> ReceiptAsync(Guid property, Guid unit, string? role, Guid payment, CancellationToken token) =>
        store.ResidentReceiptAsync(Context(property, unit, role), payment, token);
}
