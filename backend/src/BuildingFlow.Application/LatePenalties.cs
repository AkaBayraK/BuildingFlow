using System.Globalization;
using System.Text.RegularExpressions;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record LatePenaltySwitchRequest(Guid RequestId, bool Apply, DateOnly? EffectiveFrom, string? Reason);
public sealed record LatePenaltySwitchResponse(Guid Id, DateOnly EffectiveFrom, bool Apply, string Reason, string CreatedBy, DateTimeOffset CreatedAt, bool Replayed = false);
public sealed record LatePenaltyPolicyRequest(Guid RequestId, DateOnly? ValidFrom, DateOnly? ValidTo, string? MonthlyRate, string? Method,
    string? LegalBasis, string? LegalReference, Guid? ApprovalDocumentId, long Version = 0);
public sealed record LatePenaltyPolicyCommand(Guid RequestId, DateOnly ValidFrom, DateOnly? ValidTo, decimal MonthlyRate, LatePenaltyMethod Method,
    LatePenaltyLegalBasis LegalBasis, string LegalReference, Guid? ApprovalDocumentId, long Version);
public sealed record LatePenaltyPolicyResponse(Guid Id, int Year, DateOnly ValidFrom, DateOnly? ValidTo, string MonthlyRate, string Method,
    string LegalBasis, string LegalReference, Guid? ApprovalDocumentId, string Status, string? ApprovedBy, DateTimeOffset? ApprovedAt,
    string? StatusReason, string CreatedBy, DateTimeOffset CreatedAt, long Version, bool Replayed = false);
public sealed record LatePenaltyStatusRequest(long Version, string? Reason, DateOnly? ValidTo = null);
public sealed record LatePenaltySettings(Guid PropertyId, bool AppliedToday, string StatutoryRate, string StatutoryReference, string DefaultMethod,
    DateOnly? CrystallizedThrough, bool CanDraft, bool CanApprove, IReadOnlyList<LatePenaltySwitchResponse> Switches, IReadOnlyList<LatePenaltyPolicyResponse> Policies);

public sealed record LatePenaltySegmentResponse(DateOnly From, DateOnly To, int Days, string Principal, string MonthlyRate, string Method, Guid? PolicyId, string Amount);
public sealed record LatePenaltyAccrualResponse(Guid Id, DateOnly From, DateOnly To, string Amount, string Paid, string Balance, string Status,
    string? StatusReason, DateTimeOffset? StatusChangedAt, long Version, IReadOnlyList<LatePenaltySegmentResponse> Segments);
public sealed record LatePenaltyChargeDetail(Guid ChargeId, Guid UnitId, string UnitNumber, string Period, DateOnly DueDate, string Principal,
    string PrincipalPaid, string PrincipalBalance, string PenaltyAccrued, string PenaltyPaid, string PenaltyWaived, string PenaltyBalance,
    string PendingPenalty, string TotalDue, DateOnly AsOf, bool AppliedToday, IReadOnlyList<LatePenaltyAccrualResponse> Accruals,
    IReadOnlyList<LatePenaltySegmentResponse> PendingSegments);
public sealed record LatePenaltyQuery(int Page = 1, int PageSize = 10, Guid? UnitId = null, int? Year = null, string? Search = null);
public sealed record LatePenaltyRow(Guid ChargeId, Guid UnitId, string UnitNumber, string? BuildingName, string Period, DateOnly DueDate,
    int DaysOverdue, string PrincipalBalance, string PenaltyBalance, string PendingPenalty, string TotalDue);
public sealed record LatePenaltyTotals(string PrincipalOverdue, string PenaltyOutstanding, string PenaltyCollected, string PenaltyWaived, string Currency);
public sealed record LatePenaltyPage(IReadOnlyList<LatePenaltyRow> Items, int Page, int PageSize, int TotalCount, LatePenaltyTotals Totals);
public sealed record LatePenaltyPreviewLine(Guid ChargeId, string Period, DateOnly DueDate, long Version, string PrincipalBalance, string PenaltyDue,
    string SuggestedPenalty, string SuggestedPrincipal);
public sealed record LatePenaltyPreview(Guid UnitId, DateOnly PaidDate, bool Applied, string PrincipalTotal, string PenaltyTotal,
    string Unallocated, IReadOnlyList<LatePenaltyPreviewLine> Lines);
public sealed record LatePenaltyRunRequest(DateOnly? Through);
public sealed record LatePenaltyRunResult(DateOnly Through, int ChargeCount, int AccrualCount, string Amount);
public sealed record LatePenaltyWaiveRequest(long Version, string? Reason);
public sealed record LatePenaltyReportYear(int Year, string Accrued, string Collected, string Waived, string Outstanding);
public sealed record LatePenaltyReport(Guid PropertyId, string Currency, IReadOnlyList<LatePenaltyReportYear> Years,
    IReadOnlyList<LatePenaltyPolicyResponse> RateHistory, IReadOnlyList<LatePenaltySwitchResponse> Switches);
public sealed record LatePenaltyActor(AccessScope Access, bool CanDraft, bool CanApprove);

public interface ILatePenaltyStore
{
    Task<LatePenaltySettings?> SettingsAsync(Guid property, LatePenaltyActor actor, CancellationToken token);
    Task<LatePenaltySwitchResponse?> SwitchAsync(Guid property, LatePenaltySwitchRequest request, LatePenaltyActor actor, CancellationToken token);
    Task<LatePenaltyPolicyResponse?> CreatePolicyAsync(Guid property, LatePenaltyPolicyCommand command, LatePenaltyActor actor, CancellationToken token);
    Task<LatePenaltyPolicyResponse?> UpdatePolicyAsync(Guid property, Guid policy, LatePenaltyPolicyCommand command, LatePenaltyActor actor, CancellationToken token);
    Task<LatePenaltyPolicyResponse?> ApprovePolicyAsync(Guid property, Guid policy, long version, LatePenaltyActor actor, CancellationToken token);
    Task<LatePenaltyPolicyResponse?> CancelPolicyAsync(Guid property, Guid policy, LatePenaltyStatusRequest request, LatePenaltyActor actor, CancellationToken token);
    Task<LatePenaltyPolicyResponse?> EndPolicyAsync(Guid property, Guid policy, LatePenaltyStatusRequest request, LatePenaltyActor actor, CancellationToken token);
    Task<LatePenaltyPage?> OverdueAsync(Guid property, LatePenaltyQuery query, LatePenaltyActor actor, CancellationToken token);
    Task<LatePenaltyChargeDetail?> ChargeAsync(Guid property, Guid charge, LatePenaltyActor actor, CancellationToken token);
    Task<LatePenaltyPreview?> PreviewAsync(Guid property, Guid unit, DateOnly paidDate, decimal? amount, LatePenaltyActor actor, CancellationToken token);
    Task<LatePenaltyRunResult?> RunAsync(Guid property, DateOnly through, LatePenaltyActor actor, CancellationToken token);
    Task<LatePenaltyAccrualResponse?> WaiveAsync(Guid property, Guid accrual, LatePenaltyWaiveRequest request, LatePenaltyActor actor, CancellationToken token);
    Task<LatePenaltyReport?> ReportAsync(Guid property, LatePenaltyActor actor, CancellationToken token);
}

public interface ILatePenaltyScheduler
{
    Task<int> AccrueThroughPreviousMonthAsync(CancellationToken token);
}

public sealed class LatePenaltyService(ILatePenaltyStore store, ICurrentUserService user)
{
    private static readonly string[] FinanceRoles = ["SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT"];
    private LatePenaltyActor Actor()
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !FinanceRoles.Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return new(new(user.UserId, user.IsInRole("SUPER_ADMIN")),
            new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER" }.Any(user.IsInRole), new[] { "SUPER_ADMIN", "SITE_ADMIN" }.Any(user.IsInRole));
    }
    private LatePenaltyActor Approver() { var actor = Actor(); return actor.CanApprove ? actor : throw new UnauthorizedAccessException(); }
    private LatePenaltyActor Drafter() { var actor = Actor(); return actor.CanDraft ? actor : throw new UnauthorizedAccessException(); }
    private static RequestValidationException Invalid(string field, ErrorDefinition error) => new(new() { [field] = [error.Create()] });
    private static bool Rate(string? text, out decimal rate)
    {
        rate = 0;
        return text is { Length: <= 10 } && Regex.IsMatch(text, @"^[0-9](?:\.[0-9]{1,4})?$") &&
            decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out rate) && rate > 0 && rate <= LatePenaltyRules.StatutoryMonthlyRate;
    }
    public static string Rate(decimal value) => value.ToString("0.00##", CultureInfo.InvariantCulture);

    public Task<LatePenaltySettings?> SettingsAsync(Guid property, CancellationToken token) => store.SettingsAsync(property, Actor(), token);
    public Task<LatePenaltySwitchResponse?> SwitchAsync(Guid property, LatePenaltySwitchRequest request, CancellationToken token)
    {
        var actor = Approver();
        if (request.RequestId == Guid.Empty || request.EffectiveFrom is null || request.EffectiveFrom.Value.Year < 2000 ||
            string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500)
            throw Invalid("effectiveFrom", ErrorCatalog.LatePenaltySwitchInvalid);
        return store.SwitchAsync(property, request with { Reason = request.Reason.Trim() }, actor, token);
    }
    private static LatePenaltyPolicyCommand Command(LatePenaltyPolicyRequest request, bool update)
    {
        if (request.RequestId == Guid.Empty && !update || request.ValidFrom is null || request.ValidFrom.Value.Year < 2000 ||
            request.ValidTo < request.ValidFrom || !Rate(request.MonthlyRate, out var rate) ||
            request.Method != LatePenaltyRules.DefaultMethod.ToString() || request.LegalBasis != nameof(LatePenaltyLegalBasis.KMK_20) || rate != LatePenaltyRules.StatutoryMonthlyRate ||
            string.IsNullOrWhiteSpace(request.LegalReference) || request.LegalReference.Trim().Length > 500 ||
            request.ApprovalDocumentId == Guid.Empty || (update && request.Version < 1))
            throw Invalid("policy", ErrorCatalog.LatePenaltyInvalid);
        return new(request.RequestId, request.ValidFrom.Value, request.ValidTo, rate, Enum.Parse<LatePenaltyMethod>(request.Method!),
            Enum.Parse<LatePenaltyLegalBasis>(request.LegalBasis!), request.LegalReference.Trim(), request.ApprovalDocumentId, request.Version);
    }
    public Task<LatePenaltyPolicyResponse?> CreatePolicyAsync(Guid property, LatePenaltyPolicyRequest request, CancellationToken token) =>
        store.CreatePolicyAsync(property, Command(request, false), Drafter(), token);
    public Task<LatePenaltyPolicyResponse?> UpdatePolicyAsync(Guid property, Guid policy, LatePenaltyPolicyRequest request, CancellationToken token) =>
        store.UpdatePolicyAsync(property, policy, Command(request, true), Drafter(), token);
    public Task<LatePenaltyPolicyResponse?> ApprovePolicyAsync(Guid property, Guid policy, LatePenaltyStatusRequest request, CancellationToken token)
    {
        var actor = Approver();
        if (request.Version < 1) throw Invalid("version", ErrorCatalog.VersionRequired);
        return store.ApprovePolicyAsync(property, policy, request.Version, actor, token);
    }
    public Task<LatePenaltyPolicyResponse?> CancelPolicyAsync(Guid property, Guid policy, LatePenaltyStatusRequest request, CancellationToken token) =>
        store.CancelPolicyAsync(property, policy, Reasoned(request), Drafter(), token);
    public Task<LatePenaltyPolicyResponse?> EndPolicyAsync(Guid property, Guid policy, LatePenaltyStatusRequest request, CancellationToken token)
    {
        var actor = Approver(); request = Reasoned(request);
        if (request.ValidTo is null) throw Invalid("validTo", ErrorCatalog.LatePenaltyInvalid);
        return store.EndPolicyAsync(property, policy, request, actor, token);
    }
    private static LatePenaltyStatusRequest Reasoned(LatePenaltyStatusRequest request)
    {
        if (request.Version < 1) throw Invalid("version", ErrorCatalog.VersionRequired);
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500) throw Invalid("reason", ErrorCatalog.DuesErrorReason);
        return request with { Reason = request.Reason.Trim() };
    }
    public Task<LatePenaltyPage?> OverdueAsync(Guid property, LatePenaltyQuery query, CancellationToken token)
    {
        var actor = Actor();
        if (query.Page is < 1 or > 1000000 || query.PageSize is < 1 or > 100) throw Invalid("page", ErrorCatalog.InvalidPage);
        if (query.UnitId == Guid.Empty) throw Invalid("unitId", ErrorCatalog.DuesErrorUnit);
        if (query.Year is < 2000 or > 9998 || query.Search?.Trim().Length > 200) throw Invalid("search", ErrorCatalog.ErrorsSearchBounds);
        return store.OverdueAsync(property, query, actor, token);
    }
    public Task<LatePenaltyChargeDetail?> ChargeAsync(Guid property, Guid charge, CancellationToken token) => store.ChargeAsync(property, charge, Actor(), token);
    public Task<LatePenaltyPreview?> PreviewAsync(Guid property, Guid unit, DateOnly? paidDate, string? amount, CancellationToken token)
    {
        var actor = Actor();
        if (unit == Guid.Empty) throw Invalid("unitId", ErrorCatalog.DuesErrorUnit);
        if (paidDate is null || paidDate.Value.Year < 2000) throw Invalid("paidDate", ErrorCatalog.PaymentsErrorDate);
        decimal? value = null;
        if (!string.IsNullOrEmpty(amount))
        {
            if (amount.Length > 20 || !Regex.IsMatch(amount, @"^[0-9]+(?:\.[0-9]{1,2})?$") ||
                !decimal.TryParse(amount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0 || parsed > 99999999.99m)
                throw Invalid("amount", ErrorCatalog.DuesErrorAmount);
            value = parsed;
        }
        return store.PreviewAsync(property, unit, paidDate.Value, value, actor, token);
    }
    public Task<LatePenaltyRunResult?> RunAsync(Guid property, LatePenaltyRunRequest request, CancellationToken token)
    {
        var actor = Actor();
        if (request.Through is null || request.Through.Value.Year < 2000) throw Invalid("through", ErrorCatalog.LatePenaltyDate);
        return store.RunAsync(property, request.Through.Value, actor, token);
    }
    public Task<LatePenaltyAccrualResponse?> WaiveAsync(Guid property, Guid accrual, LatePenaltyWaiveRequest request, CancellationToken token)
    {
        var actor = Approver();
        if (request.Version < 1) throw Invalid("version", ErrorCatalog.VersionRequired);
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500) throw Invalid("reason", ErrorCatalog.DuesErrorReason);
        return store.WaiveAsync(property, accrual, request with { Reason = request.Reason.Trim() }, actor, token);
    }
    public Task<LatePenaltyReport?> ReportAsync(Guid property, CancellationToken token) => store.ReportAsync(property, Actor(), token);
}
