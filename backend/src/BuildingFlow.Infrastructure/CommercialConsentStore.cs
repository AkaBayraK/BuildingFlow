using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed class CommercialConsentStore(BuildingFlowDbContext db, TimeProvider clock) : ICommercialConsentStore
{
    private static HierarchyConflictException Conflict(ErrorDefinition error) => new(error.Create());

    public async Task<CommercialConsentState> RecordAsync(CommercialConsentCommand c, CancellationToken token)
    {
        var recipient = SmsText.NormalizePhone(c.Recipient) ?? throw new ArgumentException("Recipient must be a phone number.", nameof(c));
        var fingerprint = Fingerprint(c with { Recipient = recipient });
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        // The sender row lock serialises concurrent approve/reject so the newest row is always the last decision.
        var sender = await db.SmsLegalSenders.FromSql($"SELECT * FROM sms_legal_senders WHERE id = {c.SenderId} AND is_deleted = false FOR UPDATE")
            .SingleOrDefaultAsync(token) ?? throw new KeyNotFoundException("Legal sender not found.");
        var previous = await db.CommercialConsentRecords.AsNoTracking().SingleOrDefaultAsync(x => x.OperationId == c.OperationId, token);
        if (previous is not null)
        {
            if (previous.EvidenceSha256 != fingerprint) throw Conflict(ErrorCatalog.ConsentOperationConflict);
            return Map(previous, true);
        }
        var record = new CommercialConsentRecord(sender, CommercialMessageChannel.SMS, recipient, c.RecipientVerified, c.Status, c.Source, clock.GetUtcNow(),
            c.OwnerUserId, c.PersonId, c.TextVersion, c.TextSha256, c.OperationId, fingerprint);
        db.CommercialConsentRecords.Add(record);
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return Map(record, false);
    }

    public async Task<CommercialConsentState?> CurrentAsync(Guid senderId, string recipient, CancellationToken token)
    {
        var phone = SmsText.NormalizePhone(recipient);
        if (phone is null) return null;
        var row = await Rows(senderId, phone).FirstOrDefaultAsync(token);
        return row is null ? null : Map(row, false);
    }

    public async Task<PagedRows<CommercialConsentState>> HistoryAsync(Guid senderId, string recipient, int page, int pageSize, CancellationToken token)
    {
        if (page is < 1 or > 1_000_000 || pageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(page));
        var phone = SmsText.NormalizePhone(recipient);
        if (phone is null) return new([], page, pageSize, 0);
        var query = Rows(senderId, phone);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        return new(rows.Select(x => Map(x, false)).ToArray(), page, pageSize, await query.CountAsync(token));
    }

    private IQueryable<CommercialConsentRecord> Rows(Guid senderId, string phone) => db.CommercialConsentRecords.AsNoTracking()
        .Where(x => x.SenderId == senderId && x.Channel == CommercialMessageChannel.SMS && x.Recipient == phone)
        .OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id);

    private static string Fingerprint(CommercialConsentCommand c) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("commercial-consent-v1:" +
        JsonSerializer.Serialize(new { c.SenderId, c.Recipient, c.RecipientVerified, Status = c.Status.ToString(), Source = c.Source.ToString(), c.OwnerUserId, c.PersonId, c.TextVersion, c.TextSha256 }))));

    private static CommercialConsentState Map(CommercialConsentRecord x, bool replayed) => new(x.Id, x.SenderId, SmsText.MaskPhone(x.Recipient), x.Status.ToString(),
        x.Source.ToString(), x.OccurredAt, x.TextVersion, x.IysStatus.ToString(), replayed);
}
