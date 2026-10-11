using System.Collections.Concurrent;
using System.Text;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RabbitMQ.Client;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class PostgresRabbitFactAttribute : FactAttribute
{
    public PostgresRabbitFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_RABBITMQ_HOST")))
            Skip = "Dedicated PostgreSQL and RabbitMQ test environments are required.";
    }
}

[Collection(RabbitCollection.Name)]
public sealed class MessagingOutboxTests(MessagingOutboxTests.Fixture fixture) : IClassFixture<MessagingOutboxTests.Fixture>
{
    private static EmailRequested Email(Guid property) =>
        new(Guid.NewGuid(), property, Guid.NewGuid(), DateTimeOffset.UtcNow, "tr", "recipient@example.test", "Subject", "Body");

    private async Task<Guid> NewProperty()
    {
        await using var db = fixture.Db();
        var property = new Property("Messaging " + Guid.NewGuid().ToString("N")[..8], "City", "District", "Address", PropertyType.APARTMENT);
        db.Add(property); await db.SaveChangesAsync();
        return property.Id;
    }

    private async Task<List<Guid>> Drain(string type, int expected)
    {
        var ids = new List<Guid>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (ids.Count < expected) {
            var result = await fixture.Channel!.BasicGetAsync(RabbitMessageTopology.Queue(fixture.Prefix, type), autoAck: true, timeout.Token);
            if (result is null) { await Task.Delay(30, timeout.Token); continue; }
            ids.Add(MessageContracts.Decode(result.Body.Span).MessageId);
        }
        await Task.Delay(200);
        Assert.Null(await fixture.Channel!.BasicGetAsync(RabbitMessageTopology.Queue(fixture.Prefix, type), autoAck: true));
        return ids;
    }

    private async Task<int> Relay(IMessagePublisher? publisher = null)
    {
        await using var db = fixture.Db();
        return await new OutboxRelay(db, publisher ?? fixture.Publisher!, TimeProvider.System).PublishPendingAsync(default);
    }

    [PostgresRabbitFact] public async Task EnqueueCommitsOrRollsBackTogetherWithTheBusinessWrite()
    {
        await using var db = fixture.Db();
        var rolledBack = new Property("Rolled back", "City", "District", "Address", PropertyType.APARTMENT);
        var lost = Email(rolledBack.Id);
        await using (var transaction = await db.Database.BeginTransactionAsync()) {
            db.Add(rolledBack); new EfMessageOutbox(db).Enqueue(lost); await db.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        db.ChangeTracker.Clear();
        Assert.False(await db.Properties.IgnoreQueryFilters().AnyAsync(p => p.Id == rolledBack.Id));
        Assert.False(await db.OutboxMessages.AnyAsync(m => m.MessageId == lost.MessageId));

        var property = new Property("Committed", "City", "District", "Address", PropertyType.APARTMENT);
        var kept = Email(property.Id);
        db.Add(property); new EfMessageOutbox(db).Enqueue(kept); await db.SaveChangesAsync();
        Assert.Throws<InvalidMessageException>(() => new EfMessageOutbox(db).Enqueue(kept with { Recipient = "broken" }));
        Assert.DoesNotContain(db.ChangeTracker.Entries<OutboxMessage>(), e => e.State == EntityState.Added);
        var row = await db.OutboxMessages.AsNoTracking().SingleAsync(m => m.MessageId == kept.MessageId);
        Assert.Equal(OutboxStatus.PENDING, row.Status); Assert.Equal(0, row.Attempts); Assert.Null(row.PublishedAt);
        Assert.Equal("outbox-fixture", row.CreatedBy); Assert.Equal(kept, MessageContracts.Decode(Encoding.UTF8.GetBytes(row.Payload)));
        Assert.Equal(1, await Relay()); Assert.Equal([kept.MessageId], await Drain(MessageContracts.Email, 1));
    }

    [PostgresRabbitFact] public async Task RelayPublishesCommittedMessagesOnceAndMarksThemPublished()
    {
        var property = await NewProperty();
        var commands = Enumerable.Range(0, 3).Select(_ => Email(property)).ToArray();
        await using (var db = fixture.Db()) { foreach (var c in commands) new EfMessageOutbox(db).Enqueue(c); await db.SaveChangesAsync(); }
        Assert.Equal(3, await Relay());
        Assert.Equal(0, await Relay());
        Assert.Equal(commands.Select(c => c.MessageId).Order(), (await Drain(MessageContracts.Email, 3)).Order());
        await using var check = fixture.Db();
        var rows = await check.OutboxMessages.AsNoTracking().Where(m => m.PropertyId == property).ToListAsync();
        Assert.All(rows, r => { Assert.Equal(OutboxStatus.PUBLISHED, r.Status); Assert.NotNull(r.PublishedAt); Assert.Equal(1, r.Attempts); Assert.Equal(2, r.Version); });
    }

    [PostgresRabbitFact] public async Task ConcurrentRelaysSkipLockedRowsAndNeverDoublePublish()
    {
        var property = await NewProperty();
        var commands = Enumerable.Range(0, 45).Select(_ => Email(property)).ToArray();
        await using (var db = fixture.Db()) { foreach (var c in commands) new EfMessageOutbox(db).Enqueue(c); await db.SaveChangesAsync(); }
        var total = 0;
        for (var round = 0; round < 10 && total < commands.Length; round++)
            total += (await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => Relay()))).Sum();
        Assert.Equal(commands.Length, total);
        var ids = await Drain(MessageContracts.Email, commands.Length);
        Assert.Equal(commands.Length, ids.Distinct().Count());
    }

    [PostgresRabbitFact] public async Task UnavailableBrokerKeepsMessagesPendingWithSafeBackoff()
    {
        var property = await NewProperty();
        var first = Email(property); var second = Email(property);
        await using (var db = fixture.Db()) { new EfMessageOutbox(db).Enqueue(first); await db.SaveChangesAsync(); new EfMessageOutbox(db).Enqueue(second); await db.SaveChangesAsync(); }
        await using var broken = new RabbitMessagePublisher(new RabbitMessagingOptions { Host = "127.0.0.1", Port = 1, Username = "u", Password = "p" });
        var before = DateTimeOffset.UtcNow;
        Assert.Equal(0, await Relay(broken));
        await using var check = fixture.Db();
        var rows = await check.OutboxMessages.AsNoTracking().Where(m => m.PropertyId == property).ToDictionaryAsync(m => m.MessageId);
        Assert.Equal(OutboxStatus.PENDING, rows[first.MessageId].Status); Assert.Equal(1, rows[first.MessageId].Attempts);
        Assert.Equal(OutboxRelay.PublishFailed, rows[first.MessageId].LastErrorCode); Assert.True(rows[first.MessageId].NextAttemptAt > before);
        Assert.Equal(0, rows[second.MessageId].Attempts);
        await check.Database.ExecuteSqlInterpolatedAsync($"UPDATE outbox_messages SET next_attempt_at = now() WHERE property_id = {property}");
        Assert.Equal(2, await Relay());
        Assert.Equal(2, (await Drain(MessageContracts.Email, 2)).Count);
    }

    [PostgresRabbitFact] public async Task ProcessorDeduplicatesAndRejectsConflictingOrForeignMessages()
    {
        var property = await NewProperty();
        var handler = new TestHandler();
        var email = Email(property); var body = MessageContracts.Encode(email);
        Assert.Equal(MessageProcessingResult.Processed, await Process(handler, body));
        Assert.Equal(MessageProcessingResult.Duplicate, await Process(handler, body));
        Assert.Equal(MessageProcessingResult.Rejected, await Process(handler, MessageContracts.Encode(email with { Subject = "Changed" })));
        Assert.Equal(MessageProcessingResult.Rejected, await Process(handler, "not-json"u8.ToArray()));
        Assert.Equal(MessageProcessingResult.Rejected, await Process(handler, body, MessageContracts.Sms));
        Assert.Equal(MessageProcessingResult.Rejected, await Process(handler, MessageContracts.Encode(Email(Guid.NewGuid()))));
        Assert.Equal(1, handler.Calls);
        await using var db = fixture.Db();
        var inbox = await db.ProcessedMessages.AsNoTracking().SingleAsync(m => m.MessageId == email.MessageId);
        Assert.Equal(InboxStatus.PROCESSED, inbox.Status); Assert.Equal(TestHandler.Name, inbox.Consumer); Assert.Equal(property, inbox.PropertyId);
    }

    [PostgresRabbitFact] public async Task HandlerFailureLeavesNoInboxRowAndRetryCanSucceed()
    {
        var property = await NewProperty();
        var handler = new TestHandler { FailuresLeft = 1 };
        var body = MessageContracts.Encode(Email(property));
        await Assert.ThrowsAsync<TimeoutException>(() => Process(handler, body));
        await using (var db = fixture.Db()) Assert.False(await db.ProcessedMessages.AnyAsync(m => m.PropertyId == property));
        Assert.Equal(MessageProcessingResult.Processed, await Process(handler, body));
        Assert.Equal(2, handler.Calls);
    }

    [PostgresRabbitFact] public async Task ConcurrentDuplicateDeliveriesRunTheHandlerOnce()
    {
        var property = await NewProperty();
        var handler = new TestHandler { Delay = TimeSpan.FromMilliseconds(300) };
        var body = MessageContracts.Encode(Email(property));
        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Process(handler, body)));
        Assert.Single(results, r => r == MessageProcessingResult.Processed);
        Assert.Equal(4, results.Count(r => r == MessageProcessingResult.Duplicate));
        Assert.Equal(1, handler.Calls);
    }

    [PostgresRabbitFact] public async Task SkippedChannelIsRecordedAsSkipped()
    {
        var property = await NewProperty();
        var email = Email(property);
        Assert.Equal(MessageProcessingResult.Processed, await Process(new TestHandler { Outcome = MessageHandlingOutcome.Skipped }, MessageContracts.Encode(email)));
        await using var db = fixture.Db();
        Assert.Equal(InboxStatus.SKIPPED, (await db.ProcessedMessages.AsNoTracking().SingleAsync(m => m.MessageId == email.MessageId)).Status);
    }

    [PostgresRabbitFact] public async Task ConsumerAcknowledgesHandledMessagesAndDeadLettersInvalidOnes()
    {
        var property = await NewProperty();
        var handler = new TestHandler();
        await using var provider = fixture.Services(handler);
        await using var consumer = new RabbitMessageConsumer(fixture.Options!, provider.GetRequiredService<IServiceScopeFactory>());
        await consumer.StartAsync([MessageContracts.Email], default);
        var email = Email(property);
        await using (var db = fixture.Db()) { new EfMessageOutbox(db).Enqueue(email); await db.SaveChangesAsync(); }
        Assert.Equal(1, await Relay());
        await fixture.Channel!.BasicPublishAsync(RabbitMessageTopology.Exchange(fixture.Prefix), MessageContracts.Email, "{\"payload\":1}"u8.ToArray());
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        BasicGetResult? dead = null;
        while (handler.Calls == 0 || dead is null) {
            dead ??= await fixture.Channel!.BasicGetAsync(RabbitMessageTopology.DeadQueue(fixture.Prefix, MessageContracts.Email), autoAck: true, timeout.Token);
            await Task.Delay(50, timeout.Token);
        }
        Assert.Equal("{\"payload\":1}", Encoding.UTF8.GetString(dead!.Body.Span));
        Assert.Equal(1, handler.Calls); Assert.Equal(email, handler.Last);
        await Task.Delay(300);
        Assert.Null(await fixture.Channel!.BasicGetAsync(RabbitMessageTopology.Queue(fixture.Prefix, MessageContracts.Email), autoAck: true));
        await using var check = fixture.Db();
        Assert.True(await check.ProcessedMessages.AnyAsync(m => m.MessageId == email.MessageId));
    }

    private async Task<MessageProcessingResult> Process(TestHandler handler, byte[] body, string type = MessageContracts.Email)
    {
        await using var db = fixture.Db();
        return await new MessageProcessor(db, [handler], TimeProvider.System).ProcessAsync(type, body, default);
    }

    public sealed class TestHandler : IMessageHandler
    {
        public const string Name = "test-email";
        private int calls;
        public int FailuresLeft;
        public TimeSpan Delay;
        public MessageHandlingOutcome Outcome = MessageHandlingOutcome.Delivered;
        public int Calls => calls;
        public IMessageCommand? Last { get; private set; }
        public string Type => MessageContracts.Email;
        public string Consumer => Name;
        public async Task<MessageHandlingOutcome> HandleAsync(IMessageCommand command, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref calls); Last = command;
            if (Delay > TimeSpan.Zero) await Task.Delay(Delay, cancellationToken);
            if (Interlocked.Decrement(ref FailuresLeft) >= 0) throw new TimeoutException();
            return Outcome;
        }
    }

    private sealed class Actor : ICurrentUserService
    {
        public string? UserId => "outbox-fixture";
        public string? Username => "fixture";
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => false;
    }

    public sealed class Fixture : IAsyncLifetime
    {
        private string? admin;
        private string? schema;
        private string? connection;
        public RabbitMessagingOptions? Options { get; private set; }
        public RabbitMessagePublisher? Publisher { get; private set; }
        public IConnection? Connection { get; private set; }
        public IChannel? Channel { get; private set; }
        public string Prefix => Options!.Prefix;

        public BuildingFlowDbContext Db() => new(new DbContextOptionsBuilder<BuildingFlowDbContext>()
            .UseNpgsql(connection, o => o.MigrationsHistoryTable("__EFMigrationsHistory", schema))
            .AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options);

        public ServiceProvider Services(IMessageHandler handler)
        {
            var services = new ServiceCollection();
            services.AddSingleton(TimeProvider.System);
            services.AddScoped(_ => Db());
            services.AddSingleton(handler);
            services.AddScoped<MessageProcessor>();
            return services.BuildServiceProvider();
        }

        public async Task InitializeAsync()
        {
            admin = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE");
            var host = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_RABBITMQ_HOST");
            if (string.IsNullOrWhiteSpace(admin) || string.IsNullOrWhiteSpace(host)) return;
            var config = new NpgsqlConnectionStringBuilder(admin);
            if (config.Database is null || !config.Database.Contains("test", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Dedicated test database required.");
            schema = "messaging_outbox_" + Guid.NewGuid().ToString("N");
            await using (var db = new NpgsqlConnection(admin)) {
                await db.OpenAsync();
                await using var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", db); await create.ExecuteNonQueryAsync();
            }
            config.SearchPath = schema + ",public"; connection = config.ConnectionString;
            await using (var db = Db()) await db.Database.MigrateAsync();
            Options = new() {
                Host = host, Port = int.TryParse(Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_RABBITMQ_PORT"), out var port) ? port : 5672,
                Username = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_RABBITMQ_USERNAME") ?? "",
                Password = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_RABBITMQ_PASSWORD") ?? "",
                Prefix = "buildingflow-test-" + Guid.NewGuid().ToString("N"),
                RetryDelays = [TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(600)], MaxAttempts = 3
            };
            Connection = await Options.Factory().CreateConnectionAsync(); Channel = await Connection.CreateChannelAsync();
            await RabbitMessageTopology.Declare(Channel, Options, default); Publisher = new(Options);
        }

        public async Task DisposeAsync()
        {
            if (Publisher is not null) await Publisher.DisposeAsync();
            if (Channel is not null) try { await Channel.DisposeAsync(); } catch (Exception) { }
            if (Connection is not null) try { await Connection.DisposeAsync(); } catch (Exception) { }
            if (Options is not null) await RabbitTestTopology.DeleteAsync(Options);
            if (schema is not null) {
                await using var db = new NpgsqlConnection(admin); await db.OpenAsync();
                await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", db); await drop.ExecuteNonQueryAsync();
            }
        }
    }
}

public sealed class MessagingMigrationTests
{
    [PostgresFact] public async Task UpgradePreservesRowsAddsBaseEntityTablesAndBlocksDestructiveDown()
    {
        var original = Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE")!;
        var config = new NpgsqlConnectionStringBuilder(original);
        if (config.Database is null || !config.Database.Contains("test", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Dedicated test database required.");
        var schema = "messaging_migration_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(original); await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin)) await create.ExecuteNonQueryAsync();
        try {
            config.SearchPath = schema + ",public";
            await using var db = new BuildingFlowDbContext(new DbContextOptionsBuilder<BuildingFlowDbContext>()
                .UseNpgsql(config.ConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .AddInterceptors(new AuditInterceptor(new Actor(), TimeProvider.System)).Options);
            var previous = db.Database.GetMigrations().Single(v => v.EndsWith("_AnnouncementsAndDocuments", StringComparison.Ordinal));
            var migrator = db.GetService<IMigrator>(); await migrator.MigrateAsync(previous);
            var p = new Property("Upgrade preserved", "City", "District", "Address", PropertyType.APARTMENT); db.Add(p); await db.SaveChangesAsync();
            var before = await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM properties p WHERE id={p.Id}").SingleAsync();
            await db.Database.MigrateAsync(); await db.Database.MigrateAsync();
            Assert.Equal(before, await db.Database.SqlQuery<string>($"SELECT row_to_json(p)::text AS \"Value\" FROM properties p WHERE id={p.Id}").SingleAsync());
            Assert.False(db.Database.HasPendingModelChanges());
            await migrator.MigrateAsync(previous); await db.Database.MigrateAsync();
            foreach (var table in new[] { "outbox_messages", "processed_messages" }) {
                var columns = await db.Database.SqlQuery<string>($"SELECT column_name::text AS \"Value\" FROM information_schema.columns WHERE table_schema={schema} AND table_name={table} AND is_nullable='NO'").ToListAsync();
                foreach (var c in new[] { "id", "version", "created_at", "created_by", "modified_at", "modified_by", "is_deleted", "is_enabled" }) Assert.Contains(c, columns);
            }
            var email = new EmailRequested(Guid.NewGuid(), p.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, "en", "a@example.test", "S", "B");
            new EfMessageOutbox(db).Enqueue(email); await db.SaveChangesAsync();
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE outbox_messages SET type={"clr.Type"} WHERE message_id={email.MessageId}"));
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE outbox_messages SET status={"PUBLISHED"} WHERE message_id={email.MessageId}"));
            await Assert.ThrowsAsync<PostgresException>(() => migrator.MigrateAsync(previous));
            Assert.Equal(db.Database.GetMigrations().Where(v => string.CompareOrdinal(v, "20261011043002_MessagingOutboxAndInbox") <= 0), await db.Database.GetAppliedMigrationsAsync());
        } finally {
            await using var cleanup = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin); await cleanup.ExecuteNonQueryAsync();
        }
    }

    private sealed class Actor : ICurrentUserService
    {
        public string? UserId => "messaging-migration";
        public string? Username => "fixture";
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => false;
    }
}
