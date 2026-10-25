using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
namespace BuildingFlow.Infrastructure;

public sealed class OnlinePaymentStore(BuildingFlowDbContext db, PaymentStore payments, LatePenaltyEngine penalties, PaymentProviderRegistry providers,
    IMobileSessionStore sessions, ICurrentUserService user, TimeProvider clock, ILogger<OnlinePaymentStore> log) : IOnlinePaymentStore, IOnlinePaymentScheduler
{
    private static readonly PaymentIntentStatus[] Collected = [PaymentIntentStatus.SUCCEEDED, PaymentIntentStatus.REVIEW, PaymentIntentStatus.REFUNDED, PaymentIntentStatus.CHARGED_BACK];
    private static string Money(decimal value) => DuesService.Money(value);
    private static string Period(DateOnly value) => value.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    private static HierarchyConflictException Conflict(ErrorDefinition error) => new(error.Create());
    private DateTimeOffset Now => clock.GetUtcNow();
    private string Actor => user.UserId ?? throw new UnauthorizedAccessException();
    private IQueryable<Property> Properties(AccessScope access)
    {
        var parents = db.Properties.Where(value => !value.IsDeleted);
        return access.ReadAll ? parents : parents.Where(value => db.PropertyMemberships.Any(member =>
            !member.IsDeleted && member.PropertyId == value.Id && member.UserId == access.UserId && member.IsEnabled));
    }
    private async Task<Property> Lock(Guid property, CancellationToken token)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        return await db.Properties.SingleAsync(value => value.Id == property, token);
    }
    private async Task<Property?> LockScoped(Guid property, AccessScope access, CancellationToken token) =>
        await Properties(access).AnyAsync(value => value.Id == property, token) ? await Lock(property, token) : null;
    private void Touch(BaseEntity entity) => db.Entry(entity).Property(value => value.IsEnabled).IsModified = true;
    private Task<PaymentIntent?> Intent(Guid property, Guid id, CancellationToken token) =>
        db.PaymentIntents.Include(value => value.Lines).SingleOrDefaultAsync(value => value.PropertyId == property && value.Id == id, token);
    private async Task<(IPaymentProviderAdapter Adapter, ProviderMerchant Merchant)> Provider(PaymentIntent intent, CancellationToken token)
    {
        var merchant = await db.PaymentMerchantAccounts.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(value => value.PropertyId == intent.PropertyId && value.Id == intent.MerchantAccountId, token);
        var adapter = providers.Find(intent.ProviderCode) ?? throw Conflict(ErrorCatalog.PaymentProviderUnavailable);
        return (adapter, new(merchant.MerchantId, merchant.SecretReference));
    }
    private void Issue(Guid property, Guid? intent, ReconciliationIssueType type, string detail)
    {
        if (intent is not null && (db.PaymentReconciliationItems.Local.Any(value => value.IntentId == intent && value.Type == type && value.Status == ReconciliationIssueStatus.OPEN) ||
            db.PaymentReconciliationItems.Any(value => value.IntentId == intent && value.Type == type && value.Status == ReconciliationIssueStatus.OPEN))) return;
        db.PaymentReconciliationItems.Add(new(property, intent, type, detail));
    }

    // ---------- Resident flow ----------
    private Task<Property> Resident(ResidentContext context, CancellationToken token) =>
        MobileFinanceQueries.ResidentPropertyAsync(db, sessions, user, context.PropertyId, context.UnitId, context.Role, token);
    private IQueryable<DuesCharge> Visible(ResidentContext context) =>
        MobileFinanceQueries.ResidentCharges(db, context.PropertyId, context.UnitId, context.Role, Actor);
    private async Task<IPaymentProviderAdapter?> EnabledAdapter(string code, CancellationToken token) =>
        await db.PaymentProviders.AnyAsync(value => value.Code == code && value.IsEnabled, token) ? providers.Find(code) : null;
    private async Task<PaymentMerchantAccount?> Merchant(Guid property, Guid unit, CancellationToken token)
    {
        var building = await db.Units.IgnoreQueryFilters().Where(value => value.PropertyId == property && value.Id == unit).Select(value => value.BuildingId).SingleAsync(token);
        var candidates = await db.PaymentMerchantAccounts.AsNoTracking()
            .Where(value => value.PropertyId == property && value.IsEnabled && value.IsDefault && (value.BuildingId == null || value.BuildingId == building)).ToListAsync(token);
        return candidates.FirstOrDefault(value => value.BuildingId != null) ?? candidates.FirstOrDefault(value => value.BuildingId == null);
    }

    public async Task<ResidentPaymentOptions> OptionsAsync(ResidentContext context, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        await Resident(context, token);
        var today = BusinessDate.Today(clock);
        var settings = await db.PropertyPaymentSettings.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == context.PropertyId, token);
        var merchant = await Merchant(context.PropertyId, context.UnitId, token);
        var adapter = merchant is null ? null : await EnabledAdapter(merchant.ProviderCode, token);
        var online = settings?.OnlineEnabled == true && adapter is not null;
        var charges = await Visible(context).Where(value => !value.IsCancelled).ToListAsync(token);
        var descriptions = await Descriptions(context.PropertyId, charges.Select(value => value.AssessmentId), token);
        var inputs = await penalties.InputsAsync(context.PropertyId, token);
        var states = await penalties.StatesAsync(context.PropertyId, charges, null, false, token);
        var payable = LatePenaltyEngine.Suggest(charges, states, inputs, today, decimal.MaxValue).Lines
            .Select(value => new PayableCharge(value.Charge.Id, Period(value.Charge.Period), value.Charge.Kind.ToString(), value.Charge.Category?.ToString(),
                descriptions[value.Charge.AssessmentId], value.Charge.DueDate, Money(value.Charge.Balance), Money(value.PenaltyDue), Money(value.Charge.Balance + value.PenaltyDue))).ToArray();
        IReadOnlyList<ResidentBankAccount> accounts = [];
        if (settings?.ShowIbanToResidents == true)
        {
            var building = await db.Units.IgnoreQueryFilters().Where(value => value.PropertyId == context.PropertyId && value.Id == context.UnitId).Select(value => value.BuildingId).SingleAsync(token);
            var active = await db.PaymentBankAccounts.AsNoTracking().Where(value => value.PropertyId == context.PropertyId && value.Status == BankAccountStatus.ACTIVE &&
                value.ValidFrom <= today && (value.ValidTo == null || value.ValidTo >= today) && (value.BuildingId == null || value.BuildingId == building)).ToListAsync(token);
            // A block's own accounts replace the management-wide ones for that block.
            var scoped = active.Any(value => value.BuildingId != null) ? active.Where(value => value.BuildingId != null) : active;
            accounts = scoped.OrderByDescending(value => value.IsDefault).ThenBy(value => value.BankName)
                .Select(value => new ResidentBankAccount(value.BankName, value.AccountHolder, value.Iban, value.Purpose.ToString())).ToArray();
        }
        var unitNumber = await db.Units.IgnoreQueryFilters().Where(value => value.PropertyId == context.PropertyId && value.Id == context.UnitId).Select(value => value.Number).SingleAsync(token);
        await transaction.CommitAsync(token);
        return new(context.PropertyId, context.UnitId, online, online && settings!.PartialAllowed, online ? adapter!.DisplayName : null,
            "Daire " + unitNumber, accounts, payable, Now);
    }
    private async Task<Dictionary<Guid, string>> Descriptions(Guid property, IEnumerable<Guid> assessments, CancellationToken token)
    {
        var ids = assessments.Distinct().ToArray();
        return await db.DuesAssessments.AsNoTracking().Where(value => value.PropertyId == property && ids.Contains(value.Id)).ToDictionaryAsync(value => value.Id, value => value.Description, token);
    }
    private static string IntentFingerprint(IntentCommand command) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
        command.Context.UnitId, command.Context.Role, charges = command.ChargeIds.Order().ToArray(), amount = command.Amount is null ? null : Money(command.Amount.Value) }))));

    public async Task<ResidentIntentResponse> CreateIntentAsync(IntentCommand command, CancellationToken token)
    {
        var context = command.Context;
        await Resident(context, token);
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(context.PropertyId, token);
        var fingerprint = IntentFingerprint(command);
        var previous = await db.PaymentIntents.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == context.PropertyId && value.RequestId == command.RequestId, token);
        if (previous is not null)
        {
            if (previous.PayerUserId != Actor || previous.RequestFingerprint != fingerprint) throw Conflict(ErrorCatalog.PaymentsErrorRequestConflict);
            await transaction.CommitAsync(token);
            return (await ResidentResponses(context.PropertyId, [previous.Id], token))[0] with { Replayed = true };
        }
        var settings = await db.PropertyPaymentSettings.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == context.PropertyId, token);
        var merchant = settings?.OnlineEnabled == true ? await Merchant(context.PropertyId, context.UnitId, token) : null;
        if (merchant is null) throw Conflict(ErrorCatalog.PaymentOnlineDisabled);
        var adapter = await EnabledAdapter(merchant.ProviderCode, token) ?? throw Conflict(ErrorCatalog.PaymentProviderUnavailable);
        var charges = await db.DuesCharges.Where(value => Visible(context).Any(item => item.Id == value.Id) && command.ChargeIds.Contains(value.Id)).ToListAsync(token);
        if (charges.Count != command.ChargeIds.Count || charges.Any(value => value.IsCancelled))
            throw new RequestValidationException(new() { ["chargeIds"] = [ErrorCatalog.PaymentIntentInvalid.Create()] });
        var now = Now; var today = BusinessDate.Today(clock);
        if (await db.PaymentIntentLines.AnyAsync(line => line.PropertyId == context.PropertyId && command.ChargeIds.Contains(line.ChargeId) &&
                db.PaymentIntents.Any(intent => intent.Id == line.IntentId && intent.Status == PaymentIntentStatus.PENDING && intent.ExpiresAt > now), token))
            throw Conflict(ErrorCatalog.PaymentIntentActive);
        var inputs = await penalties.InputsAsync(context.PropertyId, token);
        var states = await penalties.StatesAsync(context.PropertyId, charges, null, false, token);
        var total = charges.Sum(value => value.Balance + LatePenaltyEngine.PenaltyDue(states[value.Id], inputs, today));
        if (total == 0) throw Conflict(ErrorCatalog.PaymentNothingDue);
        var amount = command.Amount ?? total;
        if (amount > total) throw new RequestValidationException(new() { ["amount"] = [ErrorCatalog.PaymentPartialAmount.Create()] });
        if (amount < total && !settings!.PartialAllowed) throw Conflict(ErrorCatalog.PaymentPartialDisabled);
        var intent = new PaymentIntent(context.PropertyId, context.UnitId, command.RequestId, fingerprint, Actor, context.Role, merchant, now);
        foreach (var line in LatePenaltyEngine.Suggest(charges, states, inputs, today, amount).Lines)
        {
            if (line.Penalty > 0) intent.AddLine(line.Charge, AllocationKind.PENALTY, line.Penalty);
            if (line.Principal > 0) intent.AddLine(line.Charge, AllocationKind.PRINCIPAL, line.Principal);
        }
        db.PaymentIntents.Add(intent); Touch(parent);
        var failed = false;
        try
        {
            var unit = await db.Units.IgnoreQueryFilters().Where(value => value.PropertyId == context.PropertyId && value.Id == context.UnitId).Select(value => value.Number).SingleAsync(token);
            var checkout = await adapter.CreateCheckoutAsync(new(intent.Id, new(merchant.MerchantId, merchant.SecretReference), intent.Amount, intent.Currency,
                $"{parent.Name} · Daire {unit}", intent.ExpiresAt), token);
            intent.Open(checkout.Reference, checkout.CheckoutUrl);
        }
        catch (PaymentProviderException error)
        {
            intent.Fail(PaymentIntentStatus.FAILED, "PROVIDER_" + error.Code, now); failed = true;
            log.LogWarning("BuildingFlow.Payments: checkout could not be opened ({Provider}, {Code})", intent.ProviderCode, error.Code);
        }
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        if (failed) throw Conflict(ErrorCatalog.PaymentProviderFailure);
        log.LogInformation("BuildingFlow.Payments: intent opened ({Provider})", intent.ProviderCode);
        return (await ResidentResponses(context.PropertyId, [intent.Id], token))[0];
    }

    public async Task<ResidentIntentResponse?> ResidentIntentAsync(ResidentContext context, Guid id, bool cancel, CancellationToken token)
    {
        await Resident(context, token);
        var intent = await db.PaymentIntents.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == context.PropertyId && value.UnitId == context.UnitId &&
            value.Id == id && value.PayerUserId == Actor, token);
        if (intent is null) return null;
        if (intent.Status == PaymentIntentStatus.PENDING) await SyncAsync(context.PropertyId, id, cancel, token);
        else if (cancel) throw Conflict(ErrorCatalog.PaymentIntentState);
        return (await ResidentResponses(context.PropertyId, [id], token))[0];
    }
    public async Task<ResidentIntentPage> ResidentIntentsAsync(ResidentContext context, int page, CancellationToken token)
    {
        await Resident(context, token);
        var intents = db.PaymentIntents.AsNoTracking().Where(value => value.PropertyId == context.PropertyId && value.UnitId == context.UnitId && value.PayerUserId == Actor);
        var total = await intents.CountAsync(token);
        var ids = await intents.OrderByDescending(value => value.CreatedAt).ThenBy(value => value.Id).Skip((page - 1) * 20).Take(20).Select(value => value.Id).ToArrayAsync(token);
        return new(ids.Length == 0 ? [] : await ResidentResponses(context.PropertyId, ids, token), page, total);
    }
    public async Task<PaymentReceiptResponse?> ResidentReceiptAsync(ResidentContext context, Guid payment, CancellationToken token)
    {
        await Resident(context, token);
        var visible = Visible(context);
        if (!await db.PaymentAllocations.AnyAsync(value => value.PropertyId == context.PropertyId && value.UnitId == context.UnitId && value.PaymentId == payment &&
                visible.Any(charge => charge.Id == value.ChargeId), token)) return null;
        return await Receipt(context.PropertyId, payment, token);
    }

    private async Task<IReadOnlyList<IntentLineResponse>> Lines(Guid property, PaymentIntent intent, CancellationToken token)
    {
        var ids = intent.Lines.Select(value => value.ChargeId).Distinct().ToArray();
        var charges = await (from charge in db.DuesCharges.AsNoTracking().Where(value => value.PropertyId == property && ids.Contains(value.Id))
                             join assessment in db.DuesAssessments.AsNoTracking() on charge.AssessmentId equals assessment.Id
                             select new { charge.Id, charge.Period, charge.Kind, charge.Category, charge.DueDate, assessment.Description }).ToDictionaryAsync(value => value.Id, token);
        return intent.Lines.OrderBy(value => charges[value.ChargeId].DueDate).ThenBy(value => charges[value.ChargeId].Period).ThenBy(value => value.ChargeId).ThenByDescending(value => value.Kind)
            .Select(value => { var charge = charges[value.ChargeId];
                return new IntentLineResponse(value.ChargeId, Period(charge.Period), charge.Kind.ToString(), charge.Category?.ToString(), charge.Description, value.Kind.ToString(), Money(value.Amount)); }).ToArray();
    }
    private async Task<ResidentIntentResponse[]> ResidentResponses(Guid property, Guid[] ids, CancellationToken token)
    {
        var intents = await db.PaymentIntents.AsNoTracking().Include(value => value.Lines).Where(value => value.PropertyId == property && ids.Contains(value.Id)).ToListAsync(token);
        var paymentIds = intents.Where(value => value.PaymentId != null).Select(value => value.PaymentId!.Value).ToArray();
        var receipts = await db.PaymentReceipts.AsNoTracking().Where(value => paymentIds.Contains(value.PaymentId)).ToDictionaryAsync(value => value.PaymentId, value => value.Number, token);
        List<ResidentIntentResponse> result = [];
        foreach (var intent in ids.Select(id => intents.Single(value => value.Id == id)))
            result.Add(new(intent.Id, intent.Status.ToString(), Money(intent.Amount), intent.Currency, intent.ExpiresAt,
                intent.Status == PaymentIntentStatus.PENDING ? intent.CheckoutUrl : null, intent.PaidDate, intent.PaymentId,
                intent.PaymentId is { } payment && receipts.TryGetValue(payment, out var number) ? number : null, intent.FailureCode, intent.CreatedAt,
                await Lines(property, intent, token)));
        return result.ToArray();
    }

    // ---------- Provider verification and state machine ----------
    // The provider is asked before the Property lock is taken; the result is applied under the lock and
    // re-checked against the intent's current state, so concurrent webhook, poll and job calls converge.
    private async Task<string> SyncAsync(Guid property, Guid id, bool cancel, CancellationToken token)
    {
        var snapshot = await db.PaymentIntents.AsNoTracking().SingleAsync(value => value.PropertyId == property && value.Id == id, token);
        if (snapshot.ProviderReference is null) return "NO_REFERENCE";
        var (adapter, merchant) = await Provider(snapshot, token);
        ProviderPaymentStatus status;
        try { status = await adapter.GetStatusAsync(merchant, snapshot.ProviderReference, token); }
        catch (PaymentProviderException) { if (cancel) throw Conflict(ErrorCatalog.PaymentProviderFailure); return "PROVIDER_UNREACHABLE"; }
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await Lock(property, token);
        var intent = (await Intent(property, id, token))!;
        var outcome = await ApplyAsync(parent, intent, status, transaction, token);
        if (cancel && intent.Status == PaymentIntentStatus.PENDING)
        {
            if (status.State is not (ProviderPaymentState.PENDING or ProviderPaymentState.UNKNOWN)) throw Conflict(ErrorCatalog.PaymentIntentState);
            intent.Fail(PaymentIntentStatus.CANCELLED, "PAYER_CANCELLED", Now); outcome = "CANCELLED";
        }
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return outcome;
    }

    private async Task<string> ApplyAsync(Property parent, PaymentIntent intent, ProviderPaymentStatus status,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, CancellationToken token)
    {
        var now = Now;
        switch (intent.Status)
        {
            case PaymentIntentStatus.PENDING:
                switch (status.State)
                {
                    case ProviderPaymentState.SUCCEEDED: return await SettleAsync(parent, intent, status, token);
                    case ProviderPaymentState.FAILED: intent.Fail(PaymentIntentStatus.FAILED, status.FailureCode ?? "PROVIDER_FAILED", now); return "FAILED";
                    case ProviderPaymentState.EXPIRED: intent.Fail(PaymentIntentStatus.EXPIRED, "EXPIRED", now); return "EXPIRED";
                    case ProviderPaymentState.UNKNOWN when intent.ExpiresAt.AddHours(24) < now:
                        Issue(intent.PropertyId, intent.Id, ReconciliationIssueType.PROVIDER_MISMATCH, "Sağlayıcı ödeme oturumunu tanımıyor; süresi 24 saatten önce doldu.");
                        return "UNKNOWN";
                    case ProviderPaymentState.REFUNDED or ProviderPaymentState.CHARGED_BACK:
                        Issue(intent.PropertyId, intent.Id, ReconciliationIssueType.PROVIDER_MISMATCH, "Sağlayıcı bekleyen ödeme için iade/ters ibraz bildirdi.");
                        return "MISMATCH";
                    default: return "PENDING";
                }
            case PaymentIntentStatus.SUCCEEDED or PaymentIntentStatus.REVIEW when status.State == ProviderPaymentState.CHARGED_BACK:
                return await ChargeBackAsync(parent, intent, transaction, token);
            case PaymentIntentStatus.SUCCEEDED or PaymentIntentStatus.REVIEW when status.State == ProviderPaymentState.REFUNDED:
                Issue(intent.PropertyId, intent.Id, ReconciliationIssueType.REFUND_UNCONFIRMED, "Sağlayıcı iade bildirdi ancak onaylı iade kaydı yok.");
                return "REFUND_UNCONFIRMED";
            case PaymentIntentStatus.FAILED or PaymentIntentStatus.EXPIRED or PaymentIntentStatus.CANCELLED when status.State == ProviderPaymentState.SUCCEEDED:
                // Money taken after the intent closed; it is never silently allocated or dropped.
                Issue(intent.PropertyId, intent.Id, ReconciliationIssueType.PROVIDER_MISMATCH, "Kapanmış ödeme niyeti için sağlayıcı başarılı tahsilat bildirdi; iade veya manuel inceleme gerekir.");
                return "MISMATCH";
            default: return "IGNORED";
        }
    }

    private async Task<string> SettleAsync(Property parent, PaymentIntent intent, ProviderPaymentStatus status, CancellationToken token)
    {
        var now = Now; var today = BusinessDate.Today(clock);
        var paidDate = status.CompletedAt is { } done ? BusinessDate.Of(done) : today;
        if (paidDate > today) paidDate = today;
        var transactionId = string.IsNullOrWhiteSpace(status.TransactionId) ? intent.ProviderReference! : status.TransactionId;
        var fee = Math.Clamp(decimal.Round(status.Fee ?? 0m, 2), 0m, intent.Amount);
        if (status.Amount != intent.Amount)
        {
            intent.Succeed(transactionId, paidDate, fee, null, now);
            db.PaymentLedgerEntries.Add(new(intent.PropertyId, intent.UnitId, PaymentLedgerType.UNALLOCATED_RECEIPT, status.Amount is > 0 ? decimal.Round(status.Amount.Value, 2) : intent.Amount,
                paidDate, nameof(PaymentMethod.ONLINE_CARD), null, intent.Id));
            Issue(intent.PropertyId, intent.Id, ReconciliationIssueType.PROVIDER_MISMATCH, "Sağlayıcı tutarı ödeme niyetiyle eşleşmiyor.");
            return "REVIEW";
        }
        var command = Command(intent, paidDate, transactionId, intent.Lines.Select(value => new AllocationCommand(value.ChargeId, value.Amount, value.ChargeVersion, value.Kind)));
        if (!await CanAllocateAsync(intent.PropertyId, command, token))
        {
            intent.Succeed(transactionId, paidDate, fee, null, now);
            db.PaymentLedgerEntries.Add(new(intent.PropertyId, intent.UnitId, PaymentLedgerType.UNALLOCATED_RECEIPT, intent.Amount, paidDate, nameof(PaymentMethod.ONLINE_CARD), null, intent.Id));
            Issue(intent.PropertyId, intent.Id, ReconciliationIssueType.DEBT_CHANGED, "Ödeme sırasında borç değişti (iptal, başka tahsilat veya tazminat düzeltmesi); tutar dağıtılamadı.");
            log.LogWarning("BuildingFlow.Payments: verified payment moved to review");
            return "REVIEW";
        }
        var payment = await payments.RecordAsync(parent, command, PaymentStore.FingerprintOf(command), false, PaymentLedgerType.COLLECTION, intent.Id, token)
            ?? throw Conflict(ErrorCatalog.PaymentReviewUnallocatable);
        intent.Succeed(transactionId, paidDate, fee, payment, now);
        log.LogInformation("BuildingFlow.Payments: verified payment settled");
        return "SUCCEEDED";
    }
    private static PaymentCommand Command(PaymentIntent intent, DateOnly paidDate, string reference, IEnumerable<AllocationCommand> allocations) =>
        new(intent.Id, intent.UnitId, intent.Amount, paidDate, PaymentMethod.ONLINE_CARD, reference, "Online ödeme · " + intent.ProviderCode,
            allocations.OrderBy(value => value.ChargeId).ThenBy(value => value.Kind).ToArray());
    // Read-only pre-check mirroring the recorder's rules, so a verified payment that no longer fits goes to
    // REVIEW without leaving half-applied penalty changes behind.
    private async Task<bool> CanAllocateAsync(Guid property, PaymentCommand command, CancellationToken token)
    {
        var ids = command.Allocations.Select(value => value.ChargeId).Distinct().ToArray();
        var charges = await db.DuesCharges.AsNoTracking().Where(value => value.PropertyId == property && value.UnitId == command.UnitId && ids.Contains(value.Id)).ToListAsync(token);
        if (charges.Count != ids.Length || charges.Any(value => value.IsCancelled)) return false;
        var inputs = await penalties.InputsAsync(property, token);
        var states = await penalties.StatesAsync(property, charges, null, false, token);
        if (states.Values.Any(state => state.Accruals.Any(accrual => accrual.ToDate > command.PaidDate))) return false;
        return charges.All(charge =>
            command.Allocations.Where(value => value.ChargeId == charge.Id && value.Kind == AllocationKind.PRINCIPAL).Sum(value => value.Amount) <= charge.Balance &&
            command.Allocations.Where(value => value.ChargeId == charge.Id && value.Kind == AllocationKind.PENALTY).Sum(value => value.Amount) <=
                LatePenaltyEngine.PenaltyDue(states[charge.Id], inputs, command.PaidDate));
    }

    private async Task<string> ChargeBackAsync(Property parent, PaymentIntent intent, Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, CancellationToken token)
    {
        intent.ChargeBack(Now);
        db.PaymentLedgerEntries.Add(new(intent.PropertyId, intent.UnitId, PaymentLedgerType.CHARGEBACK, -intent.Amount, BusinessDate.Today(clock),
            nameof(PaymentMethod.ONLINE_CARD), intent.PaymentId, intent.Id));
        await db.SaveChangesAsync(token);
        if (intent.PaymentId is not { } paymentId) return "CHARGED_BACK";
        // The chargeback itself is final; reopening the debts may be blocked by later penalty payments.
        await transaction.CreateSavepointAsync("chargeback", token);
        try
        {
            var payment = await db.Payments.Include(value => value.Allocations).SingleAsync(value => value.Id == paymentId, token);
            if (!payment.IsCancelled) await payments.ReverseAsync(parent, payment, "Ters ibraz (chargeback)", token);
            await db.SaveChangesAsync(token);
        }
        catch (HierarchyConflictException)
        {
            await transaction.RollbackToSavepointAsync("chargeback", token);
            db.ChangeTracker.Clear();
            Issue(intent.PropertyId, intent.Id, ReconciliationIssueType.CHARGEBACK_UNREVERSED, "Ters ibraz kaydedildi ancak sonraki tazminat ödemeleri nedeniyle tahsilat otomatik geri alınamadı.");
        }
        return "CHARGED_BACK";
    }

    public async Task<bool> HandleWebhookAsync(string provider, IReadOnlyDictionary<string, string> headers, byte[] body, CancellationToken token)
    {
        var adapter = providers.Find(provider.ToUpperInvariant());
        var verified = adapter?.VerifyWebhook(headers, body, Now);
        if (adapter is null || verified is null) { log.LogWarning("BuildingFlow.Payments: webhook rejected"); return false; }
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var evt = new PaymentProviderEvent(adapter.Code, verified.EventId, verified.EventType, verified.Reference, verified.OccurredAt);
        db.PaymentProviderEvents.Add(evt);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { log.LogInformation("BuildingFlow.Payments: duplicate webhook ignored"); return true; }
        var target = await db.PaymentIntents.AsNoTracking().SingleOrDefaultAsync(value => value.ProviderCode == adapter.Code && value.ProviderReference == verified.Reference, token);
        if (target is null) { evt.Processed(null, "ORPHAN", Now); await db.SaveChangesAsync(token); await transaction.CommitAsync(token); return true; }
        var (_, merchant) = await Provider(target, token);
        // Webhook bodies only trigger verification; the state applied always comes from the provider query.
        var status = await adapter.GetStatusAsync(merchant, verified.Reference, token);
        var parent = await Lock(target.PropertyId, token);
        var intent = (await Intent(target.PropertyId, target.Id, token))!;
        var outcome = await ApplyAsync(parent, intent, status, transaction, token);
        var stored = await db.PaymentProviderEvents.SingleAsync(value => value.Id == evt.Id, token);
        stored.Processed(intent, outcome, Now);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return true;
    }

    public async Task<int> SyncPendingAsync(CancellationToken token)
    {
        var cutoff = Now.AddMinutes(-1);
        var pending = await db.PaymentIntents.AsNoTracking().Where(value => value.Status == PaymentIntentStatus.PENDING && value.CreatedAt < cutoff)
            .OrderBy(value => value.CreatedAt).Take(200).Select(value => new { value.PropertyId, value.Id }).ToListAsync(token);
        var changed = 0;
        foreach (var item in pending)
        {
            db.ChangeTracker.Clear();
            try { if (await SyncAsync(item.PropertyId, item.Id, false, token) is not ("PENDING" or "PROVIDER_UNREACHABLE" or "IGNORED")) changed++; }
            catch (Exception error) when (error is HierarchyConflictException or DbUpdateException)
            { log.LogWarning("BuildingFlow.Payments: pending intent sync failed ({ExceptionType})", error.GetType().Name); }
        }
        return changed;
    }

    // ---------- Management ----------
    private sealed class IntentRow
    {
        public required PaymentIntent Intent { get; init; }
        public required string UnitNumber { get; init; }
        public string? BuildingName { get; init; }
        public string? ReceiptNumber { get; init; }
    }
    private IQueryable<IntentRow> IntentRows(Guid property, IQueryable<PaymentIntent> intents) =>
        from intent in intents
        join unit in db.Units.IgnoreQueryFilters().AsNoTracking().Where(value => value.PropertyId == property) on intent.UnitId equals unit.Id
        select new IntentRow { Intent = intent, UnitNumber = unit.Number,
            BuildingName = db.Buildings.IgnoreQueryFilters().Where(building => building.PropertyId == property && building.Id == unit.BuildingId).Select(building => building.Name).FirstOrDefault(),
            ReceiptNumber = db.PaymentReceipts.Where(receipt => receipt.PaymentId == intent.PaymentId).Select(receipt => receipt.Number).FirstOrDefault() };
    private async Task<OnlinePaymentResponse> Response(Guid property, IntentRow row, bool detail, CancellationToken token)
    {
        var intent = row.Intent; var actor = user.UserId;
        IReadOnlyList<IntentEventResponse>? events = null; IReadOnlyList<RefundResponse>? refunds = null; IReadOnlyList<ReconciliationItemResponse>? issues = null;
        if (detail)
        {
            events = await db.PaymentProviderEvents.AsNoTracking().Where(value => value.IntentId == intent.Id).OrderBy(value => value.CreatedAt)
                .Select(value => new IntentEventResponse(value.EventType, value.Outcome, value.OccurredAt, value.ProcessedAt)).ToListAsync(token);
            refunds = (await db.PaymentRefunds.AsNoTracking().Where(value => value.PropertyId == property && value.IntentId == intent.Id).OrderBy(value => value.CreatedAt).ToListAsync(token))
                .Select(Refund).ToArray();
            issues = (await db.PaymentReconciliationItems.AsNoTracking().Where(value => value.PropertyId == property && value.IntentId == intent.Id).OrderBy(value => value.CreatedAt).ToListAsync(token))
                .Select(IssueResponse).ToArray();
        }
        return new(intent.Id, intent.UnitId, row.UnitNumber, row.BuildingName, Money(intent.Amount), intent.Currency, intent.Status.ToString(), intent.ProviderCode,
            intent.CreatedAt, intent.ExpiresAt, intent.StatusChangedAt, intent.PaidDate, intent.FeeAmount is { } fee ? Money(fee) : null, intent.PaymentId,
            row.ReceiptNumber, intent.FailureCode, intent.Version, await Lines(property, intent, token), events, refunds, issues);
    }
    private RefundResponse Refund(PaymentRefund value, bool replayed) => new(value.Id, value.IntentId, Money(value.Amount), value.Reason, value.Status.ToString(),
        value.CreatedBy == user.UserId, value.CreatedAt, value.DecidedAt, value.DecisionNote, value.FailureCode, value.Version, replayed);
    private RefundResponse Refund(PaymentRefund value) => Refund(value, false);
    private static ReconciliationItemResponse IssueResponse(PaymentReconciliationItem value) => new(value.Id, value.IntentId, value.Type.ToString(), value.Status.ToString(),
        value.Detail, value.CreatedAt, value.ResolvedAt, value.ResolutionNote, value.Version);

    public async Task<OnlinePaymentPage?> ListAsync(Guid property, OnlinePaymentQuery query, PaymentActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        var intents = db.PaymentIntents.AsNoTracking().Include(value => value.Lines).Where(value => value.PropertyId == property);
        if (query.Status is not null) { var status = Enum.Parse<PaymentIntentStatus>(query.Status); intents = intents.Where(value => value.Status == status); }
        if (query.UnitId is not null) intents = intents.Where(value => value.UnitId == query.UnitId);
        if (query.From is not null) { var from = new DateTimeOffset(query.From.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3)); intents = intents.Where(value => value.CreatedAt >= from); }
        if (query.To is not null) { var to = new DateTimeOffset(query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3)); intents = intents.Where(value => value.CreatedAt < to); }
        var count = await intents.CountAsync(token);
        var sums = await intents.GroupBy(_ => 1).Select(group => new {
            Succeeded = group.Sum(value => value.Status == PaymentIntentStatus.SUCCEEDED ? value.Amount : 0m),
            Review = group.Sum(value => value.Status == PaymentIntentStatus.REVIEW ? value.Amount : 0m),
            Refunded = group.Sum(value => value.Status == PaymentIntentStatus.REFUNDED ? value.Amount : 0m),
            Fees = group.Sum(value => value.FeeAmount ?? 0m),
            Pending = group.Count(value => value.Status == PaymentIntentStatus.PENDING), ReviewCount = group.Count(value => value.Status == PaymentIntentStatus.REVIEW)
        }).SingleOrDefaultAsync(token);
        var rows = await IntentRows(property, intents).OrderByDescending(value => value.Intent.CreatedAt).ThenBy(value => value.Intent.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(token);
        List<OnlinePaymentResponse> items = [];
        foreach (var row in rows) items.Add(await Response(property, row, false, token));
        await transaction.CommitAsync(token);
        return new(items, query.Page, query.PageSize, count, new(Money(sums?.Succeeded ?? 0m), Money(sums?.Review ?? 0m), Money(sums?.Refunded ?? 0m),
            Money(sums?.Fees ?? 0m), sums?.Pending ?? 0, sums?.ReviewCount ?? 0, "TRY"));
    }
    public async Task<OnlinePaymentResponse?> GetAsync(Guid property, Guid id, PaymentActor actor, CancellationToken token)
    {
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        var row = await IntentRows(property, db.PaymentIntents.AsNoTracking().Include(value => value.Lines).Where(value => value.PropertyId == property && value.Id == id)).SingleOrDefaultAsync(token);
        return row is null ? null : await Response(property, row, true, token);
    }
    public async Task<OnlinePaymentResponse?> SettleReviewAsync(Guid property, Guid id, long version, PaymentActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockScoped(property, actor.Access, token); if (parent is null) return null;
        if (!parent.IsEnabled) throw Conflict(ErrorCatalog.PaymentsInactiveProperty);
        var intent = await Intent(property, id, token); if (intent is null) return null;
        if (intent.Version != version) throw Conflict(ErrorCatalog.RecordChanged);
        if (intent.Status != PaymentIntentStatus.REVIEW) throw Conflict(ErrorCatalog.PaymentIntentState);
        var ids = intent.Lines.Select(value => value.ChargeId).Distinct().ToArray();
        var charges = await db.DuesCharges.AsNoTracking().Where(value => value.PropertyId == property && value.UnitId == intent.UnitId && ids.Contains(value.Id)).ToListAsync(token);
        var inputs = await penalties.InputsAsync(property, token);
        var states = await penalties.StatesAsync(property, charges, null, false, token);
        var (lines, remaining) = LatePenaltyEngine.Suggest(charges, states, inputs, intent.PaidDate!.Value, intent.Amount);
        if (remaining > 0) throw Conflict(ErrorCatalog.PaymentReviewUnallocatable);
        var allocations = lines.SelectMany(value => new[] {
            value.Penalty > 0 ? new AllocationCommand(value.Charge.Id, value.Penalty, value.Charge.Version, AllocationKind.PENALTY) : null,
            value.Principal > 0 ? new AllocationCommand(value.Charge.Id, value.Principal, value.Charge.Version, AllocationKind.PRINCIPAL) : null }).OfType<AllocationCommand>();
        var command = Command(intent, intent.PaidDate.Value, intent.ProviderTransactionId!, allocations);
        // The money was already entered in the ledger as an unallocated receipt when it arrived.
        var payment = await payments.RecordAsync(parent, command, PaymentStore.FingerprintOf(command), false, null, intent.Id, token)
            ?? throw Conflict(ErrorCatalog.PaymentReviewUnallocatable);
        intent.Settle(payment, Now);
        foreach (var item in await db.PaymentReconciliationItems.Where(value => value.IntentId == id && value.Type == ReconciliationIssueType.DEBT_CHANGED &&
            value.Status == ReconciliationIssueStatus.OPEN).ToListAsync(token))
            item.Resolve(Actor, "Güncel borçlara yeniden dağıtıldı.", Now);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return await GetAsync(property, id, actor, token);
    }

    public async Task<RefundResponse?> RequestRefundAsync(Guid property, Guid id, RefundRequest request, PaymentActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockScoped(property, actor.Access, token); if (parent is null) return null;
        var previous = await db.PaymentRefunds.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == property && value.RequestId == request.RequestId, token);
        if (previous is not null)
        {
            if (previous.IntentId != id || previous.Reason != request.Reason) throw Conflict(ErrorCatalog.PaymentsErrorRequestConflict);
            await transaction.CommitAsync(token); return Refund(previous, true);
        }
        var intent = await Intent(property, id, token); if (intent is null) return null;
        if (intent.Status is not (PaymentIntentStatus.SUCCEEDED or PaymentIntentStatus.REVIEW) || providers.Find(intent.ProviderCode)?.SupportsRefund != true ||
            await db.PaymentRefunds.AnyAsync(value => value.IntentId == id && value.Status == PaymentRefundStatus.REQUESTED, token))
            throw Conflict(ErrorCatalog.PaymentRefundState);
        var refund = new PaymentRefund(intent, request.RequestId, request.Reason!);
        db.PaymentRefunds.Add(refund); Touch(parent);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return Refund(refund);
    }
    public async Task<RefundResponse?> DecideRefundAsync(Guid property, Guid id, bool approve, string? note, long version, PaymentActor actor, CancellationToken token)
    {
        string? failure = null;
        await using (var transaction = await db.Database.BeginTransactionAsync(token))
        {
            var parent = await LockScoped(property, actor.Access, token); if (parent is null) return null;
            var refund = await db.PaymentRefunds.SingleOrDefaultAsync(value => value.PropertyId == property && value.Id == id, token);
            if (refund is null) return null;
            if (refund.Version != version) throw Conflict(ErrorCatalog.RecordChanged);
            if (refund.Status != PaymentRefundStatus.REQUESTED) throw Conflict(ErrorCatalog.PaymentRefundState);
            if (refund.CreatedBy == Actor) throw Conflict(ErrorCatalog.PaymentSecondApprover);
            if (!approve)
            {
                refund.Reject(Actor, note!, Now); Touch(parent);
                await db.SaveChangesAsync(token); await transaction.CommitAsync(token); return Refund(refund);
            }
            var intent = (await Intent(property, refund.IntentId, token))!;
            if (!intent.CanMoveTo(PaymentIntentStatus.REFUNDED)) throw Conflict(ErrorCatalog.PaymentRefundState);
            if (intent.PaymentId is { } paymentId)
            {
                var payment = await db.Payments.Include(value => value.Allocations).SingleAsync(value => value.Id == paymentId, token);
                if (!payment.IsCancelled) await payments.ReverseAsync(parent, payment, "Online ödeme iadesi: " + refund.Reason, token);
            }
            intent.Refund(Now);
            db.PaymentLedgerEntries.Add(new(property, intent.UnitId, PaymentLedgerType.ONLINE_REFUND, -intent.Amount, BusinessDate.Today(clock),
                nameof(PaymentMethod.ONLINE_CARD), intent.PaymentId, intent.Id, refund.Id));
            var (adapter, merchant) = await Provider(intent, token);
            try
            {
                // Called last, inside the lock: a provider rejection rolls every local change back.
                var providerRefund = await adapter.RefundAsync(merchant, intent.ProviderReference!, intent.Amount, token);
                refund.Succeed(Actor, providerRefund, Now); Touch(parent);
                await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
                log.LogInformation("BuildingFlow.Payments: refund completed");
                return Refund(refund);
            }
            catch (PaymentProviderException error) { failure = "PROVIDER_" + error.Code; await transaction.RollbackAsync(token); }
        }
        db.ChangeTracker.Clear();
        await using var retry = await db.Database.BeginTransactionAsync(token);
        await Lock(property, token);
        var failed = await db.PaymentRefunds.SingleAsync(value => value.Id == id, token);
        failed.Fail(Actor, failure!, Now);
        await db.SaveChangesAsync(token); await retry.CommitAsync(token);
        log.LogWarning("BuildingFlow.Payments: refund rejected by provider ({Code})", failure);
        return Refund(failed);
    }

    public async Task<PagedRows<ReconciliationItemResponse>?> IssuesAsync(Guid property, string? status, int page, int pageSize, PaymentActor actor, CancellationToken token)
    {
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        var items = db.PaymentReconciliationItems.AsNoTracking().Where(value => value.PropertyId == property);
        if (status is not null) { var parsed = Enum.Parse<ReconciliationIssueStatus>(status); items = items.Where(value => value.Status == parsed); }
        var count = await items.CountAsync(token);
        var rows = await items.OrderBy(value => value.Status).ThenByDescending(value => value.CreatedAt).ThenBy(value => value.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        return new(rows.Select(IssueResponse).ToArray(), page, pageSize, count);
    }
    public async Task<ReconciliationItemResponse?> ResolveIssueAsync(Guid property, Guid id, string note, long version, PaymentActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockScoped(property, actor.Access, token); if (parent is null) return null;
        var item = await db.PaymentReconciliationItems.SingleOrDefaultAsync(value => value.PropertyId == property && value.Id == id, token);
        if (item is null) return null;
        if (item.Version != version) throw Conflict(ErrorCatalog.RecordChanged);
        if (item.Status != ReconciliationIssueStatus.OPEN) throw Conflict(ErrorCatalog.PaymentReconciliationState);
        item.Resolve(Actor, note, Now); Touch(parent);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return IssueResponse(item);
    }
    public async Task<ReconciliationRunResult?> ReconcileAsync(Guid property, DateOnly date, PaymentActor actor, CancellationToken token)
    {
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        var pending = await db.PaymentIntents.AsNoTracking().Where(value => value.PropertyId == property && value.Status == PaymentIntentStatus.PENDING)
            .Select(value => value.Id).Take(500).ToListAsync(token);
        int settled = 0, expired = 0;
        foreach (var id in pending)
        {
            db.ChangeTracker.Clear();
            var outcome = await SyncAsync(property, id, false, token);
            if (outcome is "SUCCEEDED" or "REVIEW") settled++;
            if (outcome is "EXPIRED" or "FAILED") expired++;
        }
        db.ChangeTracker.Clear();
        var opened = 0;
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await Lock(property, token);
        var merchants = await db.PaymentMerchantAccounts.IgnoreQueryFilters().AsNoTracking().Where(value => value.PropertyId == property && !value.IsDeleted).ToListAsync(token);
        var collected = await db.PaymentIntents.AsNoTracking().Where(value => value.PropertyId == property && value.PaidDate == date && Collected.Contains(value.Status)).ToListAsync(token);
        foreach (var merchant in merchants)
        {
            var adapter = providers.Find(merchant.ProviderCode); if (adapter is null) continue;
            IReadOnlyList<ProviderSettlementLine> lines;
            try { lines = await adapter.SettlementAsync(new(merchant.MerchantId, merchant.SecretReference), date, token); }
            catch (PaymentProviderException) { continue; }
            var mine = collected.Where(value => value.MerchantAccountId == merchant.Id).ToArray();
            foreach (var line in lines.Where(value => value.Kind == "PAYMENT"))
            {
                var match = mine.FirstOrDefault(value => value.ProviderReference == line.Reference);
                if (match is not null && match.Amount == line.Amount && (match.FeeAmount ?? 0) == line.Fee) continue;
                var intentId = match?.Id ?? await db.PaymentIntents.Where(value => value.PropertyId == property && value.ProviderReference == line.Reference).Select(value => (Guid?)value.Id).FirstOrDefaultAsync(token);
                var before = db.PaymentReconciliationItems.Local.Count;
                Issue(property, intentId, ReconciliationIssueType.PROVIDER_MISMATCH, $"{date:yyyy-MM-dd} sağlayıcı mutabakat satırı kayıtla eşleşmiyor.");
                opened += db.PaymentReconciliationItems.Local.Count - before;
            }
            foreach (var intent in mine.Where(value => lines.All(line => line.Kind != "PAYMENT" || line.Reference != value.ProviderReference)))
            {
                var before = db.PaymentReconciliationItems.Local.Count;
                Issue(property, intent.Id, ReconciliationIssueType.PROVIDER_MISMATCH, $"{date:yyyy-MM-dd} tahsilatı sağlayıcı mutabakat raporunda yok.");
                opened += db.PaymentReconciliationItems.Local.Count - before;
            }
        }
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return new(pending.Count, settled, expired, opened);
    }
    public async Task<SettlementReport?> SettlementAsync(Guid property, DateOnly from, DateOnly to, PaymentActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        var online = nameof(PaymentMethod.ONLINE_CARD);
        var ledger = await db.PaymentLedgerEntries.AsNoTracking().Where(value => value.PropertyId == property && value.Method == online && value.OccurredOn >= from && value.OccurredOn <= to)
            .Select(value => new { value.OccurredOn, value.Type, value.Amount }).ToListAsync(token);
        var fees = await db.PaymentIntents.AsNoTracking().Where(value => value.PropertyId == property && value.PaidDate >= from && value.PaidDate <= to)
            .Select(value => new { Date = value.PaidDate!.Value, Fee = value.FeeAmount ?? 0m }).ToListAsync(token);
        var open = await db.PaymentReconciliationItems.CountAsync(value => value.PropertyId == property && value.Status == ReconciliationIssueStatus.OPEN, token);
        await transaction.CommitAsync(token);
        var days = ledger.Select(value => value.OccurredOn).Concat(fees.Select(value => value.Date)).Distinct().Order().Select(day =>
        {
            var entries = ledger.Where(value => value.OccurredOn == day).ToArray();
            var collectedAmount = entries.Where(value => value.Type is PaymentLedgerType.COLLECTION or PaymentLedgerType.UNALLOCATED_RECEIPT).Sum(value => value.Amount);
            var refunded = -entries.Where(value => value.Type == PaymentLedgerType.ONLINE_REFUND).Sum(value => value.Amount);
            var charged = -entries.Where(value => value.Type == PaymentLedgerType.CHARGEBACK).Sum(value => value.Amount);
            var fee = fees.Where(value => value.Date == day).Sum(value => value.Fee);
            return new SettlementDay(day, entries.Count(value => value.Type is PaymentLedgerType.COLLECTION or PaymentLedgerType.UNALLOCATED_RECEIPT),
                Money(collectedAmount), Money(fee), Money(refunded), Money(charged), Money(collectedAmount - refunded - charged - fee));
        }).ToArray();
        return new(property, from, to, "TRY", days, open);
    }
    public async Task<LedgerPage?> LedgerAsync(Guid property, DateOnly? from, DateOnly? to, int page, int pageSize, PaymentActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        var entries = db.PaymentLedgerEntries.AsNoTracking().Where(value => value.PropertyId == property);
        if (from is not null) entries = entries.Where(value => value.OccurredOn >= from);
        if (to is not null) entries = entries.Where(value => value.OccurredOn <= to);
        var count = await entries.CountAsync(token);
        var credits = await entries.Where(value => value.Amount > 0).SumAsync(value => (decimal?)value.Amount, token) ?? 0m;
        var debits = await entries.Where(value => value.Amount < 0).SumAsync(value => (decimal?)value.Amount, token) ?? 0m;
        var rows = await (from entry in entries
                          join unit in db.Units.IgnoreQueryFilters().AsNoTracking().Where(value => value.PropertyId == property) on entry.UnitId equals unit.Id
                          orderby entry.OccurredOn descending, entry.CreatedAt descending, entry.Id
                          select new { entry, unit.Number, Receipt = db.PaymentReceipts.Where(receipt => receipt.PaymentId == entry.PaymentId).Select(receipt => receipt.Number).FirstOrDefault() })
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        await transaction.CommitAsync(token);
        return new(rows.Select(value => new LedgerEntryResponse(value.entry.Id, value.entry.UnitId, value.Number, value.entry.Type.ToString(), Money(value.entry.Amount),
                value.entry.OccurredOn, value.entry.Method, value.entry.PaymentId, value.entry.IntentId, value.entry.RefundId, value.Receipt, value.entry.CreatedAt)).ToArray(),
            page, pageSize, count, new(Money(credits), Money(debits), Money(credits + debits), "TRY"));
    }
    public async Task<PaymentReceiptResponse?> ReceiptAsync(Guid property, Guid payment, PaymentActor actor, CancellationToken token) =>
        await Properties(actor.Access).AnyAsync(value => value.Id == property, token) ? await Receipt(property, payment, token) : null;
    private async Task<PaymentReceiptResponse?> Receipt(Guid property, Guid paymentId, CancellationToken token)
    {
        var row = await (from receipt in db.PaymentReceipts.AsNoTracking().Where(value => value.PropertyId == property && value.PaymentId == paymentId)
                         join payment in db.Payments.AsNoTracking() on receipt.PaymentId equals payment.Id
                         join parent in db.Properties.IgnoreQueryFilters().AsNoTracking() on payment.PropertyId equals parent.Id
                         join unit in db.Units.IgnoreQueryFilters().AsNoTracking().Where(value => value.PropertyId == property) on payment.UnitId equals unit.Id
                         select new { receipt, payment, parent.Name, unit.Number,
                             Building = db.Buildings.IgnoreQueryFilters().Where(building => building.PropertyId == property && building.Id == unit.BuildingId).Select(building => building.Name).FirstOrDefault() })
            .SingleOrDefaultAsync(token);
        if (row is null) return null;
        var lines = await (from allocation in db.PaymentAllocations.AsNoTracking().Where(value => value.PropertyId == property && value.PaymentId == paymentId)
                           join charge in db.DuesCharges.AsNoTracking() on allocation.ChargeId equals charge.Id
                           join assessment in db.DuesAssessments.AsNoTracking() on charge.AssessmentId equals assessment.Id
                           orderby charge.Period, charge.Id, allocation.Kind
                           select new { charge.Period, charge.Kind, charge.Category, assessment.Description, AllocationKind = allocation.Kind, allocation.Amount }).ToListAsync(token);
        return new(row.receipt.Number, row.receipt.CreatedAt, row.Name, row.Number, row.Building, row.payment.PaidDate, row.payment.Method.ToString(),
            Money(row.payment.Amount), row.payment.Currency, row.payment.IsCancelled ? "CANCELLED" : "RECORDED",
            lines.Select(value => new ReceiptLineResponse(Period(value.Period), value.Kind.ToString(), value.Category?.ToString(), value.Description,
                value.AllocationKind.ToString(), Money(value.Amount))).ToArray());
    }
}
