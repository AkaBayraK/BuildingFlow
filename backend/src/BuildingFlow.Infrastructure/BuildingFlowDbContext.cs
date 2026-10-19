using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingFlow.Infrastructure;

public sealed class BuildingFlowDbContext : DbContext
{
    public BuildingFlowDbContext(DbContextOptions<BuildingFlowDbContext> options) : base(options)
    {
        // Give the audit interceptor time to turn a parent delete into a soft delete,
        // including when required child relationships are already tracked.
        ChangeTracker.CascadeDeleteTiming = Microsoft.EntityFrameworkCore.ChangeTracking.CascadeTiming.OnSaveChanges;
    }
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyMembership> PropertyMemberships => Set<PropertyMembership>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<PersonMessage> PersonMessages => Set<PersonMessage>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<UnitOccupancy> UnitOccupancies => Set<UnitOccupancy>();
    public DbSet<DuesAssessment> DuesAssessments => Set<DuesAssessment>();
    public DbSet<DuesCharge> DuesCharges => Set<DuesCharge>();
    public DbSet<DuesChargeParty> DuesChargeParties => Set<DuesChargeParty>();
    public DbSet<DuesCancellationReason> DuesCancellationReasons => Set<DuesCancellationReason>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<PropertyBudget> PropertyBudgets => Set<PropertyBudget>();
    public DbSet<BudgetRevision> BudgetRevisions => Set<BudgetRevision>();

    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<StoredDocument> StoredDocuments => Set<StoredDocument>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<SmsAccount> SmsAccounts => Set<SmsAccount>();
    public DbSet<SmsCreditTransaction> SmsCreditTransactions => Set<SmsCreditTransaction>();
    public DbSet<SmsTemplate> SmsTemplates => Set<SmsTemplate>();
    public DbSet<SmsMessage> SmsMessages => Set<SmsMessage>();
    public DbSet<MobileRegistration> MobileRegistrations => Set<MobileRegistration>();
    public DbSet<PersonLinkAttempt> PersonLinkAttempts => Set<PersonLinkAttempt>();

    public DbSet<Meter> Meters => Set<Meter>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();
    public DbSet<MeterReadingRevision> MeterReadingRevisions => Set<MeterReadingRevision>();

    public DbSet<ConsentType> ConsentTypes => Set<ConsentType>();
    public DbSet<ConsentText> ConsentTexts => Set<ConsentText>();
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BuildingFlowDbContext).Assembly);
    }
}
