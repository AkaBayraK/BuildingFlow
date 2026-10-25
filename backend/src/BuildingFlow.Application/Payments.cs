using System.Globalization;
using System.Text.RegularExpressions;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record AllocationRequest(Guid ChargeId, string? Amount, long Version, string? Kind = null);
public sealed record CreatePaymentRequest(Guid RequestId, Guid UnitId, string? Amount, string? Currency,
    DateOnly? PaidDate, string? Method, string? Reference, string? Description, IReadOnlyList<AllocationRequest>? Allocations, Guid? BankAccountId = null);
public sealed record AllocationCommand(Guid ChargeId, decimal Amount, long Version, AllocationKind Kind = AllocationKind.PRINCIPAL);
public sealed record PaymentCommand(Guid RequestId, Guid UnitId, decimal Amount, DateOnly PaidDate,
    PaymentMethod Method, string? Reference, string Description, IReadOnlyList<AllocationCommand> Allocations, Guid? BankAccountId = null);
public sealed record PaymentAllocationResponse(Guid Id, Guid ChargeId, string Period, string Amount, string Kind = "PRINCIPAL", Guid? AccrualId = null,
    string ChargeKind = "DUES", string? ChargeCategory = null);
public sealed record PaymentResponse(Guid Id, Guid PropertyId, Guid UnitId, Guid? BuildingId, string UnitNumber,
    string? BuildingName, Guid RequestId, string Amount, string Currency, DateOnly PaidDate, string Method,
    string? Reference, string Description, string Status, long Version, DateTimeOffset CreatedAt,
    string? CancellationReason, DateTimeOffset? CancelledAt, IReadOnlyList<PaymentAllocationResponse> Allocations, bool Replayed,
    string? ReceiptNumber = null, Guid? BankAccountId = null, string? BankAccountIban = null);
public sealed record PaymentQuery(int Page = 1, int PageSize = 10, string? Search = null, Guid? UnitId = null,
    Guid? ChargeId = null, DateOnly? From = null, DateOnly? To = null, string? Status = null, string? Method = null,
    string SortBy = "paidDate", string SortDirection = "desc");
public sealed record PaymentSummary(string RecordedAmount, string CancelledAmount, int RecordedCount, int CancelledCount, string Currency);
public sealed record PaymentPage(IReadOnlyList<PaymentResponse> Items, int Page, int PageSize, int TotalCount, PaymentSummary Summary);
public interface IPaymentStore
{
    Task<PaymentResponse?> CreateAsync(Guid property, PaymentCommand command, AccessScope access, CancellationToken token);
    Task<PaymentResponse?> GetAsync(Guid property, Guid payment, AccessScope access, CancellationToken token);
    Task<PaymentPage?> ListAsync(Guid property, PaymentQuery query, AccessScope access, CancellationToken token);
    Task<PaymentResponse?> CancelAsync(Guid property, Guid payment, CancelChargeRequest request, AccessScope access, CancellationToken token);
}
public sealed class PaymentService(IPaymentStore store, ICurrentUserService user)
{
    private AccessScope Access()
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT" }.Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return new(user.UserId, user.IsInRole("SUPER_ADMIN"));
    }
    private static bool Money(string? text, out decimal amount)
    {
        amount = 0;
        return text is { Length: <= 20 } && Regex.IsMatch(text, @"^[0-9]+(?:\.[0-9]{1,2})?$") &&
            decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) && amount > 0 && amount <= 99999999.99m;
    }
    private static void Check(Dictionary<string, ApiErrorMessage[]> errors) { if (errors.Count > 0) throw new RequestValidationException(errors); }
    public Task<PaymentResponse?> CreateAsync(Guid property, CreatePaymentRequest request, CancellationToken token)
    {
        var access = Access(); Dictionary<string, ApiErrorMessage[]> errors = [];
        if (request.RequestId == Guid.Empty) errors["requestId"] = [ErrorCatalog.DuesErrorRequestId.Create()];
        if (request.UnitId == Guid.Empty) errors["unitId"] = [ErrorCatalog.DuesErrorUnit.Create()];
        if (!Money(request.Amount, out var amount)) errors["amount"] = [ErrorCatalog.DuesErrorAmount.Create()];
        if (request.Currency != "TRY") errors["currency"] = [ErrorCatalog.DuesErrorCurrency.Create()];
        if (request.PaidDate is null || request.PaidDate.Value.Year < 2000) errors["paidDate"] = [ErrorCatalog.PaymentsErrorDate.Create()];
        var validMethod = request.Method is "CASH" or "BANK_TRANSFER";
        if (!validMethod) errors["method"] = [ErrorCatalog.PaymentsErrorMethod.Create()];
        if (request.Reference?.Trim().Length > 100 || (request.Method == "BANK_TRANSFER" && string.IsNullOrWhiteSpace(request.Reference))) errors["reference"] = [ErrorCatalog.PaymentsReferenceRequired.Create()];
        if (request.BankAccountId == Guid.Empty || (request.BankAccountId is not null && request.Method != "BANK_TRANSFER")) errors["bankAccountId"] = [ErrorCatalog.PaymentBankAccountUnusable.Create()];
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 500) errors["description"] = [ErrorCatalog.DuesErrorDescription.Create()];
        List<AllocationCommand> allocations = [];
        if (request.Allocations is null || request.Allocations.Count is < 1 or > 100 || request.Allocations.Any(value => value is null)) errors["allocations"] = [ErrorCatalog.PaymentsErrorAllocations.Create()];
        else
        {
            foreach (var value in request.Allocations)
                if (value.ChargeId == Guid.Empty || value.Version < 1 || !Money(value.Amount, out var part) || value.Kind is not (null or "PRINCIPAL" or "PENALTY"))
                    errors["allocations"] = [ErrorCatalog.PaymentsErrorAllocations.Create()];
                else allocations.Add(new(value.ChargeId, part, value.Version, value.Kind == "PENALTY" ? AllocationKind.PENALTY : AllocationKind.PRINCIPAL));
            if (allocations.Select(value => (value.ChargeId, value.Kind)).Distinct().Count() != allocations.Count ||
                allocations.GroupBy(value => value.ChargeId).Any(group => group.Select(value => value.Version).Distinct().Count() > 1))
                errors["allocations"] = [ErrorCatalog.PaymentsErrorAllocations.Create()];
            if (allocations.Sum(value => value.Amount) != amount) errors["amount"] = [ErrorCatalog.PaymentsErrorTotal.Create()];
        }
        Check(errors);
        var method = Enum.Parse<PaymentMethod>(request.Method!);
        var reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim();
        if (method == PaymentMethod.BANK_TRANSFER) reference = reference!.ToUpperInvariant();
        return store.CreateAsync(property, new(request.RequestId, request.UnitId, amount, request.PaidDate!.Value, method, reference, request.Description!.Trim(), allocations.OrderBy(value => value.ChargeId).ThenBy(value => value.Kind).ToArray(), request.BankAccountId), access, token);
    }
    public Task<PaymentPage?> ListAsync(Guid property, PaymentQuery query, CancellationToken token)
    {
        var access = Access(); Dictionary<string, ApiErrorMessage[]> errors = [];
        if (query.Page is < 1 or > 1000000 || query.PageSize is < 1 or > 100) errors["page"] = [ErrorCatalog.InvalidPage.Create()];
        if (query.Search?.Trim().Length > 200) errors["search"] = [ErrorCatalog.ErrorsSearchBounds.Create()];
        if (query.UnitId == Guid.Empty || query.ChargeId == Guid.Empty) errors["unitId"] = [ErrorCatalog.DuesErrorUnit.Create()];
        if (query.From > query.To) errors["to"] = [ErrorCatalog.PaymentsErrorDateRange.Create()];
        if (!string.IsNullOrEmpty(query.Status) && query.Status is not ("RECORDED" or "CANCELLED")) errors["status"] = [ErrorCatalog.PaymentsErrorStatus.Create()];
        if (!string.IsNullOrEmpty(query.Method) && query.Method is not ("CASH" or "BANK_TRANSFER" or "ONLINE_CARD")) errors["method"] = [ErrorCatalog.PaymentsErrorMethod.Create()];
        if (query.SortBy is not ("paidDate" or "amount" or "createdAt") || query.SortDirection is not ("asc" or "desc")) errors["sortBy"] = [ErrorCatalog.InvalidSortField.Create()];
        Check(errors); return store.ListAsync(property, query, access, token);
    }
    public Task<PaymentResponse?> GetAsync(Guid property, Guid payment, CancellationToken token) => store.GetAsync(property, payment, Access(), token);
    public Task<PaymentResponse?> CancelAsync(Guid property, Guid payment, CancelChargeRequest request, CancellationToken token)
    {
        var access = Access(); Dictionary<string, ApiErrorMessage[]> errors = [];
        if (request.Version < 1) errors["version"] = [ErrorCatalog.VersionRequired.Create()];
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500) errors["reason"] = [ErrorCatalog.DuesErrorReason.Create()];
        Check(errors); return store.CancelAsync(property, payment, request, access, token);
    }
}
