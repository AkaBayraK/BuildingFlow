using System.Security.Cryptography;
using System.Text;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BuildingFlow.Infrastructure;

public sealed class EfMessageOutbox(BuildingFlowDbContext db) : IMessageOutbox
{
    public void Enqueue(IMessageCommand command)
    {
        var payload = Encoding.UTF8.GetString(MessageContracts.Encode(command));
        db.OutboxMessages.Add(new OutboxMessage(command.MessageId, command.PropertyId, command.CorrelationId,
            MessageContracts.TypeOf(command), command.OccurredAt, payload));
    }
}

// Publishing happens after the business commit, so delivery is at-least-once; consumers deduplicate via processed_messages.
public sealed class OutboxRelay(BuildingFlowDbContext db, IMessagePublisher publisher, TimeProvider clock)
{
    public const int BatchSize = 20;
    public const string PublishFailed = "PUBLISH_FAILED";
    public const string InvalidPayload = "INVALID_PAYLOAD";

    public async Task<int> PublishPendingAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var batch = await db.OutboxMessages.FromSql($"""
            SELECT * FROM outbox_messages
            WHERE status = 'PENDING' AND is_deleted = false AND next_attempt_at <= {now}
            ORDER BY next_attempt_at, created_at LIMIT {BatchSize} FOR UPDATE SKIP LOCKED
            """).IgnoreQueryFilters().ToListAsync(cancellationToken);
        var published = 0;
        foreach (var message in batch) {
            IMessageCommand command;
            try { command = MessageContracts.Decode(Encoding.UTF8.GetBytes(message.Payload)); }
            catch (InvalidMessageException) { message.RecordFailure(InvalidPayload, now.AddDays(365)); continue; }
            try {
                await publisher.PublishAsync(command, cancellationToken);
                message.MarkPublished(clock.GetUtcNow());
                published++;
            } catch (MessagePublicationException) {
                message.RecordFailure(PublishFailed, now.Add(Backoff(message.Attempts)));
                break;
            }
        }
        await db.SaveChangesAsync(CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
        return published;
    }

    public static TimeSpan Backoff(int attempts) => TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, Math.Clamp(attempts, 0, 9))));
}

public enum MessageProcessingResult { Processed, Duplicate, Rejected }

public sealed class MessageProcessor(BuildingFlowDbContext db, IEnumerable<IMessageHandler> handlers, TimeProvider clock)
{
    public async Task<MessageProcessingResult> ProcessAsync(string queueType, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        IMessageCommand command;
        try { command = MessageContracts.Decode(body.Span); }
        catch (InvalidMessageException) { return MessageProcessingResult.Rejected; }
        var type = MessageContracts.TypeOf(command);
        var handler = handlers.SingleOrDefault(candidate => candidate.Type == type);
        if (type != queueType || handler is null) return MessageProcessingResult.Rejected;
        var fingerprint = Convert.ToHexString(SHA256.HashData(MessageContracts.Encode(command)));
        var previous = await Previous(handler.Consumer, command.MessageId, fingerprint, cancellationToken);
        if (previous is not null) return previous.Value;
        if (!await db.Properties.AnyAsync(property => property.Id == command.PropertyId, cancellationToken)) return MessageProcessingResult.Rejected;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var inbox = new ProcessedMessage(handler.Consumer, command.MessageId, command.PropertyId, type, fingerprint, InboxStatus.PROCESSED, clock.GetUtcNow());
        db.ProcessedMessages.Add(inbox);
        try {
            await db.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            return await Previous(handler.Consumer, command.MessageId, fingerprint, cancellationToken) ?? throw new InvalidOperationException("Inbox state is inconsistent.");
        }
        try {
            if (await handler.HandleAsync(command, cancellationToken) == MessageHandlingOutcome.Skipped) {
                inbox.MarkSkipped();
                await db.SaveChangesAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            return MessageProcessingResult.Processed;
        } catch {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task<MessageProcessingResult?> Previous(string consumer, Guid messageId, string fingerprint, CancellationToken cancellationToken)
    {
        var previous = await db.ProcessedMessages.IgnoreQueryFilters().AsNoTracking()
            .Where(message => message.Consumer == consumer && message.MessageId == messageId)
            .Select(message => message.PayloadSha256).SingleOrDefaultAsync(cancellationToken);
        if (previous is null) return null;
        return previous == fingerprint ? MessageProcessingResult.Duplicate : MessageProcessingResult.Rejected;
    }
}
