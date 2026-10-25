using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace BuildingFlow.Infrastructure;

public sealed class PaymentConfigurationStore(BuildingFlowDbContext db, PaymentProviderRegistry providers, IPaymentSecretResolver secrets,
    ICurrentUserService user, TimeProvider clock) : IPaymentConfigurationStore
{
    private string Actor => user.UserId ?? throw new UnauthorizedAccessException();
    private IQueryable<Property> Properties(AccessScope access)
    {
        var parents = db.Properties.Where(value => !value.IsDeleted);
        return access.ReadAll ? parents : parents.Where(value => db.PropertyMemberships.Any(member =>
            !member.IsDeleted && member.PropertyId == value.Id && member.UserId == access.UserId && member.IsEnabled));
    }
    private async Task<Property?> LockProperty(Guid property, AccessScope access, CancellationToken token)
    {
        if (!await Properties(access).AnyAsync(value => value.Id == property, token)) return null;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM properties WHERE id = {property} FOR UPDATE", token);
        var parent = await Properties(access).SingleOrDefaultAsync(value => value.Id == property, token);
        if (parent is { IsEnabled: false }) throw new HierarchyConflictException(ErrorCatalog.PaymentsInactiveProperty.Create());
        return parent;
    }
    private void Touch(BaseEntity entity) => db.Entry(entity).Property(value => value.IsEnabled).IsModified = true;
    private static HierarchyConflictException Conflict(ErrorDefinition error) => new(error.Create());
    private static void Expect(BaseEntity entity, long version)
    {
        if (entity.Version != version) throw Conflict(ErrorCatalog.RecordChanged);
    }
    private async Task Save(CancellationToken token, ErrorDefinition unique)
    {
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { throw Conflict(unique); }
    }
    private async Task CheckBuilding(Guid property, Guid? building, CancellationToken token)
    {
        if (building is not null && !await db.Buildings.AnyAsync(value => value.PropertyId == property && value.Id == building && value.IsEnabled, token))
            throw new RequestValidationException(new() { ["buildingId"] = [ErrorCatalog.PaymentBankAccountInvalid.Create()] });
    }

    public async Task<IReadOnlyList<PaymentProviderResponse>> ProvidersAsync(CancellationToken token)
    {
        var rows = await db.PaymentProviders.AsNoTracking().ToDictionaryAsync(value => value.Code, token);
        PaymentProviderResponse Row(string code, string name, IPaymentProviderAdapter? adapter) => rows.TryGetValue(code, out var row)
            ? new(code, name, adapter is not null, row.IsEnabled && adapter is not null, adapter?.SupportsRefund ?? false, "SANDBOX", row.Version)
            : new(code, name, adapter is not null, false, adapter?.SupportsRefund ?? false, "SANDBOX", 0);
        return providers.All.Select(value => Row(value.Code, value.DisplayName, value))
            .Concat(PaymentProviderRegistry.Planned.Where(value => providers.Find(value.Key) is null).Select(value => Row(value.Key, value.Value, null)))
            .OrderBy(value => value.Code).ToArray();
    }
    public async Task<PaymentProviderResponse?> UpdateProviderAsync(string code, UpdatePaymentProviderRequest request, CancellationToken token)
    {
        var adapter = providers.Find(code);
        if (adapter is null) return PaymentProviderRegistry.Planned.ContainsKey(code) ? throw Conflict(ErrorCatalog.PaymentProviderUnavailable) : null;
        var row = await db.PaymentProviders.SingleOrDefaultAsync(value => value.Code == code, token);
        if (row is null)
        {
            if (request.Version != 0) throw Conflict(ErrorCatalog.RecordChanged);
            db.PaymentProviders.Add(new PaymentProvider(code, request.Enabled));
        }
        else { Expect(row, request.Version); row.SetEnabled(request.Enabled); }
        await Save(token, ErrorCatalog.RecordChanged);
        return (await ProvidersAsync(token)).Single(value => value.Code == code);
    }

    private static PaymentSettingsResponse Settings(Guid property, PropertyPaymentSettings? value, PaymentActor actor) => value is null
        ? new(property, false, false, false, true, "BF", nameof(CommissionPolicy.ABSORBED), 0, actor.CanApprove, actor.CanPropose)
        : new(property, value.OnlineEnabled, value.PartialAllowed, value.ShowIbanToResidents, value.RequireDualApproval, value.ReceiptPrefix,
            value.CommissionPolicy.ToString(), value.Version, actor.CanApprove, actor.CanPropose);
    public async Task<PaymentSettingsResponse?> SettingsAsync(Guid property, PaymentActor actor, CancellationToken token)
    {
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        return Settings(property, await db.PropertyPaymentSettings.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == property, token), actor);
    }
    public async Task<PaymentSettingsResponse?> UpdateSettingsAsync(Guid property, PaymentSettingsRequest request, PaymentActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property, actor.Access, token); if (parent is null) return null;
        var settings = await db.PropertyPaymentSettings.SingleOrDefaultAsync(value => value.PropertyId == property, token);
        if (settings is null)
        {
            if (request.Version != 0) throw Conflict(ErrorCatalog.RecordChanged);
            settings = new PropertyPaymentSettings(property); db.PropertyPaymentSettings.Add(settings);
        }
        else Expect(settings, request.Version);
        settings.Update(request.OnlineEnabled, request.PartialAllowed, request.ShowIbanToResidents, request.RequireDualApproval, request.ReceiptPrefix!);
        Touch(parent); await Save(token, ErrorCatalog.RecordChanged); await transaction.CommitAsync(token);
        return Settings(property, settings, actor);
    }

    private sealed class AccountRow
    {
        public required PaymentBankAccount Account { get; init; }
        public string? BuildingName { get; init; }
    }
    private BankAccountResponse Account(AccountRow row, bool replayed = false)
    {
        var value = row.Account;
        return new(value.Id, value.BuildingId, row.BuildingName, value.BankName, value.AccountHolder, value.Iban, value.Currency, value.Purpose.ToString(),
            value.IsDefault, value.ValidFrom, value.ValidTo, value.Status.ToString(), value.CreatedBy == user.UserId, value.CreatedAt, value.DecidedAt,
            value.StatusReason, value.Version, replayed);
    }
    private async Task<BankAccountResponse> AccountAsync(Guid property, Guid id, bool replayed, CancellationToken token)
    {
        var row = await (from account in db.PaymentBankAccounts.AsNoTracking().Where(value => value.PropertyId == property && value.Id == id)
                         select new AccountRow { Account = account, BuildingName = db.Buildings.IgnoreQueryFilters().Where(building => building.PropertyId == property && building.Id == account.BuildingId)
                             .Select(building => building.Name).FirstOrDefault() }).SingleAsync(token);
        return Account(row, replayed);
    }
    public async Task<PagedRows<BankAccountResponse>?> BankAccountsAsync(Guid property, int page, int pageSize, string? status, PaymentActor actor, CancellationToken token)
    {
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        var accounts = db.PaymentBankAccounts.AsNoTracking().Where(value => value.PropertyId == property);
        if (status is not null) { var parsed = Enum.Parse<BankAccountStatus>(status); accounts = accounts.Where(value => value.Status == parsed); }
        var count = await accounts.CountAsync(token);
        var rows = await accounts.OrderByDescending(value => value.IsDefault).ThenBy(value => value.Status).ThenByDescending(value => value.CreatedAt).ThenBy(value => value.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(account => new AccountRow { Account = account, BuildingName = db.Buildings.IgnoreQueryFilters().Where(building => building.PropertyId == property && building.Id == account.BuildingId)
                .Select(building => building.Name).FirstOrDefault() }).ToListAsync(token);
        return new(rows.Select(value => Account(value)).ToArray(), page, pageSize, count);
    }
    public async Task<BankAccountResponse?> CreateBankAccountAsync(Guid property, BankAccountCommand command, PaymentActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property, actor.Access, token); if (parent is null) return null;
        var previous = await db.PaymentBankAccounts.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == property && value.RequestId == command.RequestId, token);
        if (previous is not null)
        {
            if (previous.Iban != command.Iban || previous.BuildingId != command.BuildingId || previous.Purpose != command.Purpose || previous.BankName != command.BankName ||
                previous.AccountHolder != command.AccountHolder || previous.ValidFrom != command.ValidFrom || previous.ValidTo != command.ValidTo)
                throw Conflict(ErrorCatalog.PaymentsErrorRequestConflict);
            await transaction.CommitAsync(token); return await AccountAsync(property, previous.Id, true, token);
        }
        await CheckBuilding(property, command.BuildingId, token);
        var account = new PaymentBankAccount(property, command.RequestId, command.BuildingId, command.BankName, command.AccountHolder, command.Iban,
            command.Purpose, command.ValidFrom, command.ValidTo);
        var settings = await db.PropertyPaymentSettings.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == property, token);
        // Without dual approval an approver's own proposal is active at once; a MANAGER proposal always waits.
        if (!(settings?.RequireDualApproval ?? true) && actor.CanApprove) account.Approve(Actor, false, clock.GetUtcNow());
        db.PaymentBankAccounts.Add(account); Touch(parent);
        await Save(token, ErrorCatalog.PaymentIbanDuplicate); await transaction.CommitAsync(token);
        return await AccountAsync(property, account.Id, false, token);
    }
    private async Task<(Property Parent, PaymentBankAccount Account)?> LockedAccount(Guid property, Guid id, long version, PaymentActor actor, CancellationToken token)
    {
        var parent = await LockProperty(property, actor.Access, token); if (parent is null) return null;
        var account = await db.PaymentBankAccounts.SingleOrDefaultAsync(value => value.PropertyId == property && value.Id == id, token);
        if (account is null) return null;
        Expect(account, version); return (parent, account);
    }
    public async Task<BankAccountResponse?> DecideBankAccountAsync(Guid property, Guid id, bool approve, string? reason, long version, PaymentActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (await LockedAccount(property, id, version, actor, token) is not var (parent, account)) return null;
        var dual = (await db.PropertyPaymentSettings.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == property, token))?.RequireDualApproval ?? true;
        if (account.Status != BankAccountStatus.PENDING_APPROVAL) throw Conflict(ErrorCatalog.PaymentBankAccountState);
        if (approve && dual && account.CreatedBy == Actor) throw Conflict(ErrorCatalog.PaymentSecondApprover);
        if (approve) account.Approve(Actor, dual, clock.GetUtcNow()); else account.Reject(Actor, reason!, clock.GetUtcNow());
        Touch(parent); await Save(token, ErrorCatalog.PaymentIbanDuplicate); await transaction.CommitAsync(token);
        return await AccountAsync(property, id, false, token);
    }
    public async Task<BankAccountResponse?> CloseBankAccountAsync(Guid property, Guid id, DateOnly validTo, string reason, long version, PaymentActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (await LockedAccount(property, id, version, actor, token) is not var (parent, account)) return null;
        if (account.Status != BankAccountStatus.ACTIVE || validTo < account.ValidFrom || validTo < BusinessDate.Today(clock).AddDays(-1) || (account.ValidTo is not null && validTo > account.ValidTo))
            throw Conflict(ErrorCatalog.PaymentBankAccountState);
        // Payments already received keep pointing at this row; closing only shortens validity.
        if (await db.Payments.AnyAsync(value => value.PropertyId == property && value.BankAccountId == id && value.PaidDate > validTo && !value.IsCancelled, token))
            throw Conflict(ErrorCatalog.PaymentBankAccountState);
        account.Close(validTo, reason); Touch(parent);
        await Save(token, ErrorCatalog.RecordChanged); await transaction.CommitAsync(token);
        return await AccountAsync(property, id, false, token);
    }
    public async Task<BankAccountResponse?> DefaultBankAccountAsync(Guid property, Guid id, long version, PaymentActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (await LockedAccount(property, id, version, actor, token) is not var (parent, account)) return null;
        if (account.Status != BankAccountStatus.ACTIVE) throw Conflict(ErrorCatalog.PaymentBankAccountState);
        foreach (var other in await db.PaymentBankAccounts.Where(value => value.PropertyId == property && value.BuildingId == account.BuildingId && value.IsDefault && value.Id != id).ToListAsync(token))
            other.MarkDefault(false);
        await db.SaveChangesAsync(token);
        account.MarkDefault(true); Touch(parent);
        await Save(token, ErrorCatalog.RecordChanged); await transaction.CommitAsync(token);
        return await AccountAsync(property, id, false, token);
    }

    private sealed class MerchantRow
    {
        public required PaymentMerchantAccount Merchant { get; init; }
        public string? BuildingName { get; init; }
    }
    private MerchantAccountResponse Merchant(MerchantRow row, bool replayed = false)
    {
        var value = row.Merchant;
        return new(value.Id, value.BuildingId, row.BuildingName, value.ProviderCode, value.Label, value.MerchantId, value.SecretReference,
            secrets.Has(value.SecretReference), value.Environment.ToString(), value.IsDefault, value.IsEnabled, value.LastCheckedAt, value.LastCheckSucceeded, value.Version, replayed);
    }
    private IQueryable<MerchantRow> MerchantRows(Guid property) =>
        db.PaymentMerchantAccounts.AsNoTracking().Where(value => value.PropertyId == property)
            .Select(merchant => new MerchantRow { Merchant = merchant, BuildingName = db.Buildings.IgnoreQueryFilters().Where(building => building.PropertyId == property && building.Id == merchant.BuildingId)
                .Select(building => building.Name).FirstOrDefault() });
    private async Task<MerchantAccountResponse> MerchantAsync(Guid property, Guid id, bool replayed, CancellationToken token) =>
        Merchant(await MerchantRows(property).SingleAsync(value => value.Merchant.Id == id, token), replayed);
    public async Task<PagedRows<MerchantAccountResponse>?> MerchantsAsync(Guid property, int page, int pageSize, PaymentActor actor, CancellationToken token)
    {
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        var rows = MerchantRows(property);
        var count = await rows.CountAsync(token);
        var items = await rows.OrderByDescending(value => value.Merchant.IsDefault).ThenByDescending(value => value.Merchant.IsEnabled)
            .ThenByDescending(value => value.Merchant.CreatedAt).ThenBy(value => value.Merchant.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        return new(items.Select(value => Merchant(value)).ToArray(), page, pageSize, count);
    }
    public async Task<MerchantAccountResponse?> CreateMerchantAsync(Guid property, MerchantAccountRequest request, PaymentActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var parent = await LockProperty(property, actor.Access, token); if (parent is null) return null;
        var previous = await db.PaymentMerchantAccounts.AsNoTracking().SingleOrDefaultAsync(value => value.PropertyId == property && value.RequestId == request.RequestId, token);
        if (previous is not null)
        {
            if (previous.ProviderCode != request.ProviderCode || previous.MerchantId != request.MerchantId || previous.Label != request.Label ||
                previous.SecretReference != request.SecretReference || previous.BuildingId != request.BuildingId)
                throw Conflict(ErrorCatalog.PaymentsErrorRequestConflict);
            await transaction.CommitAsync(token); return await MerchantAsync(property, previous.Id, true, token);
        }
        if (providers.Find(request.ProviderCode!) is null) throw Conflict(ErrorCatalog.PaymentProviderUnavailable);
        await CheckBuilding(property, request.BuildingId, token);
        var merchant = new PaymentMerchantAccount(property, request.RequestId, request.BuildingId, request.ProviderCode!, request.Label!, request.MerchantId!,
            request.SecretReference!, PaymentEnvironment.SANDBOX);
        db.PaymentMerchantAccounts.Add(merchant); Touch(parent);
        await Save(token, ErrorCatalog.RecordChanged); await transaction.CommitAsync(token);
        return await MerchantAsync(property, merchant.Id, false, token);
    }
    private async Task<(Property Parent, PaymentMerchantAccount Merchant)?> LockedMerchant(Guid property, Guid id, long version, PaymentActor actor, CancellationToken token)
    {
        var parent = await LockProperty(property, actor.Access, token); if (parent is null) return null;
        var merchant = await db.PaymentMerchantAccounts.SingleOrDefaultAsync(value => value.PropertyId == property && value.Id == id, token);
        if (merchant is null) return null;
        Expect(merchant, version); return (parent, merchant);
    }
    public async Task<MerchantAccountResponse?> DefaultMerchantAsync(Guid property, Guid id, long version, PaymentActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (await LockedMerchant(property, id, version, actor, token) is not var (parent, merchant)) return null;
        if (!merchant.IsEnabled) throw Conflict(ErrorCatalog.PaymentBankAccountState);
        foreach (var other in await db.PaymentMerchantAccounts.Where(value => value.PropertyId == property && value.BuildingId == merchant.BuildingId && value.IsDefault && value.Id != id).ToListAsync(token))
            other.MarkDefault(false);
        await db.SaveChangesAsync(token);
        merchant.MarkDefault(true); Touch(parent);
        await Save(token, ErrorCatalog.RecordChanged); await transaction.CommitAsync(token);
        return await MerchantAsync(property, id, false, token);
    }
    public async Task<MerchantAccountResponse?> DeactivateMerchantAsync(Guid property, Guid id, long version, PaymentActor actor, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (await LockedMerchant(property, id, version, actor, token) is not var (parent, merchant)) return null;
        if (!merchant.IsEnabled) throw Conflict(ErrorCatalog.PaymentBankAccountState);
        merchant.Deactivate(); Touch(parent);
        await Save(token, ErrorCatalog.RecordChanged); await transaction.CommitAsync(token);
        return await MerchantAsync(property, id, false, token);
    }
    public async Task<MerchantAccountResponse?> TestMerchantAsync(Guid property, Guid id, PaymentActor actor, CancellationToken token)
    {
        if (!await Properties(actor.Access).AnyAsync(value => value.Id == property, token)) return null;
        var merchant = await db.PaymentMerchantAccounts.SingleOrDefaultAsync(value => value.PropertyId == property && value.Id == id, token);
        if (merchant is null) return null;
        var adapter = providers.Find(merchant.ProviderCode) ?? throw Conflict(ErrorCatalog.PaymentProviderUnavailable);
        bool succeeded;
        try { succeeded = await adapter.TestConnectionAsync(new(merchant.MerchantId, merchant.SecretReference), token); }
        catch (PaymentProviderException) { succeeded = false; }
        merchant.RecordCheck(succeeded, clock.GetUtcNow());
        await Save(token, ErrorCatalog.RecordChanged);
        return await MerchantAsync(property, id, false, token);
    }
}
