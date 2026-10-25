using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

// Runs inside the caller's transaction while the Property row is locked.
public sealed class LatePenaltyEngine(BuildingFlowDbContext db, TimeProvider clock)
{
    public sealed record Inputs(PenaltySwitchPeriod[] Switches, PenaltyRatePeriod[] Policies)
    {
        public bool AppliedOn(DateOnly day) => Switches.LastOrDefault(value => value.EffectiveFrom <= day)?.Apply ?? false;
    }
    public sealed record ChargeState(DuesCharge Charge, List<LatePenaltyAccrual> Accruals, List<PenaltyPrincipalEvent> Events);

    public async Task<Inputs> InputsAsync(Guid property, CancellationToken token)
    {
        var switches = await db.LatePenaltySwitches.AsNoTracking().Where(value => value.PropertyId == property)
            .OrderBy(value => value.EffectiveFrom).Select(value => new PenaltySwitchPeriod(value.EffectiveFrom, value.Apply)).ToArrayAsync(token);
        var policies = await db.LatePenaltyPolicies.AsNoTracking()
            .Where(value => value.PropertyId == property && value.Status == LatePenaltyPolicyStatus.APPROVED)
            .OrderBy(value => value.ValidFrom)
            .Select(value => new PenaltyRatePeriod(value.ValidFrom, value.ValidTo, value.MonthlyRate, value.Method, value.Id)).ToArrayAsync(token);
        return new(switches, policies);
    }

    public async Task<Dictionary<Guid, ChargeState>> StatesAsync(Guid property, IReadOnlyCollection<DuesCharge> charges, Guid? excludedPayment, bool track, CancellationToken token)
    {
        var ids = charges.Select(value => value.Id).ToArray();
        var source = track ? db.LatePenaltyAccruals : db.LatePenaltyAccruals.AsNoTracking();
        var accruals = await source.Where(value => value.PropertyId == property && ids.Contains(value.ChargeId) &&
            value.Status != LatePenaltyAccrualStatus.SUPERSEDED).ToListAsync(token);
        var events = await (from allocation in db.PaymentAllocations.AsNoTracking()
                            join payment in db.Payments.AsNoTracking() on allocation.PaymentId equals payment.Id
                            where allocation.PropertyId == property && ids.Contains(allocation.ChargeId) && allocation.Kind == AllocationKind.PRINCIPAL &&
                                  !payment.IsCancelled && payment.Id != excludedPayment
                            select new { allocation.ChargeId, payment.PaidDate, allocation.Amount }).ToListAsync(token);
        return charges.ToDictionary(value => value.Id, value => new ChargeState(value,
            accruals.Where(item => item.ChargeId == value.Id).OrderBy(item => item.FromDate).ToList(),
            events.Where(item => item.ChargeId == value.Id).Select(item => new PenaltyPrincipalEvent(item.PaidDate, item.Amount)).ToList()));
    }

    public static DateOnly NextStart(ChargeState state) =>
        state.Accruals.Count == 0 ? state.Charge.DueDate.AddDays(1) : state.Accruals.Max(value => value.ToDate).AddDays(1);

    public static IReadOnlyList<PenaltySegmentResult> Pending(ChargeState state, Inputs inputs, DateOnly through) =>
        state.Charge.IsCancelled ? [] : LatePenaltyCalculator.Calculate(state.Charge.Amount, state.Charge.DueDate, NextStart(state), through,
            state.Events, inputs.Switches, inputs.Policies);

    public sealed record Suggestion(DuesCharge Charge, decimal PenaltyDue, decimal Penalty, decimal Principal);
    public static decimal PenaltyDue(ChargeState state, Inputs inputs, DateOnly paidDate) =>
        state.Accruals.Sum(value => value.Balance) + LatePenaltyCalculator.Round(Pending(state, inputs, paidDate).Sum(value => value.Amount));
    // TBK m.100/101 order: oldest debt first, its penalty before its principal. Manual collections
    // show it only as a suggestion; online payments, which have no operator, apply it.
    public static (IReadOnlyList<Suggestion> Lines, decimal Remaining) Suggest(IEnumerable<DuesCharge> charges,
        IReadOnlyDictionary<Guid, ChargeState> states, Inputs inputs, DateOnly paidDate, decimal amount)
    {
        var remaining = amount; List<Suggestion> lines = [];
        foreach (var charge in charges.Where(value => !value.IsCancelled).OrderBy(value => value.DueDate).ThenBy(value => value.Period).ThenBy(value => value.Id))
        {
            var due = PenaltyDue(states[charge.Id], inputs, paidDate);
            if (charge.Balance == 0 && due == 0) continue;
            var penalty = Math.Min(remaining, due); remaining -= penalty;
            var principal = Math.Min(remaining, charge.Balance); remaining -= principal;
            lines.Add(new(charge, due, penalty, principal));
        }
        return (lines, remaining);
    }

    public LatePenaltyAccrual? Accrue(ChargeState state, Inputs inputs, DateOnly through)
    {
        if (through > BusinessDate.Today(clock)) throw new RequestValidationException(new() { ["through"] = [ErrorCatalog.LatePenaltyDate.Create()] });
        var segments = Pending(state, inputs, through);
        if (segments.Count == 0 || LatePenaltyCalculator.Round(segments.Sum(value => value.Amount)) <= 0) return null;
        var accrual = new LatePenaltyAccrual(state.Charge, segments);
        db.LatePenaltyAccruals.Add(accrual); state.Accruals.Add(accrual); return accrual;
    }

    // A principal change dated `paidDate` affects days after it. Unpaid accruals covering
    // those days are superseded (kept as history) and recalculated over the same range.
    public async Task RebuildAsync(ChargeState state, Inputs inputs, DateOnly paidDate, string reason, CancellationToken token)
    {
        var affected = state.Accruals.Where(value => value.ToDate > paidDate).ToArray();
        if (affected.Length == 0) return;
        if (affected.Any(value => value.Status != LatePenaltyAccrualStatus.ACTIVE || value.PaidAmount > 0))
            throw new HierarchyConflictException(ErrorCatalog.LatePenaltyRebuildBlocked.Create());
        var from = affected.Min(value => value.FromDate); var through = affected.Max(value => value.ToDate);
        foreach (var accrual in affected) { accrual.Supersede(reason, clock.GetUtcNow()); state.Accruals.Remove(accrual); }
        await db.SaveChangesAsync(token);
        if (state.Charge.IsCancelled) return;
        var segments = LatePenaltyCalculator.Calculate(state.Charge.Amount, state.Charge.DueDate, from, through, state.Events, inputs.Switches, inputs.Policies);
        if (segments.Count == 0 || LatePenaltyCalculator.Round(segments.Sum(value => value.Amount)) <= 0) return;
        var rebuilt = new LatePenaltyAccrual(state.Charge, segments);
        db.LatePenaltyAccruals.Add(rebuilt); state.Accruals.Add(rebuilt);
    }

    public async Task<DateOnly?> CrystallizedThroughAsync(Guid property, CancellationToken token) =>
        await db.LatePenaltyAccruals.Where(value => value.PropertyId == property && value.Status != LatePenaltyAccrualStatus.SUPERSEDED)
            .MaxAsync(value => (DateOnly?)value.ToDate, token);

    public async Task<(int Charges, int Accruals, decimal Amount)> AccrueOutstandingAsync(Guid property, DateOnly through, CancellationToken token)
    {
        var inputs = await InputsAsync(property, token);
        if (!inputs.Switches.Any(value => value.Apply)) return (0, 0, 0m);
        var charges = await db.DuesCharges.Where(value => value.PropertyId == property && !value.IsCancelled && value.DueDate < through &&
            value.PaidAmount < value.Amount).ToListAsync(token);
        var states = await StatesAsync(property, charges, null, true, token);
        var created = states.Values.Select(state => Accrue(state, inputs, through)).Where(value => value is not null).Select(value => value!).ToArray();
        await db.SaveChangesAsync(token);
        return (charges.Count, created.Length, created.Sum(value => value.Amount));
    }
}
