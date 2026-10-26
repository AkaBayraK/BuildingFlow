using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class SmsPolicyResolverTests(SiteApiTests.SiteApiFactory factory) : IClassFixture<SiteApiTests.SiteApiFactory>
{
    private static readonly SmsTypeDefinition Marketing = new("TEST_MARKETING", SmsPurpose.MARKETING, ["message"], [], "{{message}}", SmsCategory.MARKETING);
    private const string Phone = "+905551234567";
    private static readonly string Sha = new('c', 64);
    private static readonly Dictionary<string, string> Body = new() { ["message"] = "Yeni hizmetimiz" };

    [Theory]
    [InlineData(SmsCategory.SECURITY_OTP)] [InlineData(SmsCategory.PASSWORD_RESET)] [InlineData(SmsCategory.ACCOUNT_VERIFICATION)] [InlineData(SmsCategory.EMERGENCY_NOTIFICATION)]
    public async Task SecurityAndEmergencySmsNeverGetAFooterOrDatabaseLookup(SmsCategory category)
    {
        var resolver = new SmsPolicyResolver(null!, SmsPolicyCatalog.Default, null!);
        var type = new SmsTypeDefinition("X", SmsPolicyCatalog.Default.Get(category).Purpose, ["message"], [], "{{message}}", category);
        Assert.Same(SmsResolution.Plain, await resolver.ResolveAsync(Guid.NewGuid(), type, Phone, default));
    }

    [PostgresFact] public async Task OperationalOptOutStopsServiceSmsButNeverSecurityOrEmergency()
    {
        var property = await NewProperty();
        await factory.WithDb(async db =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var resolver = new SmsPolicyResolver(db, SmsPolicyCatalog.Default, new CommercialConsentStore(db, TimeProvider.System));
            SmsTypeDefinition Type(SmsCategory c) => new("X", SmsPolicyCatalog.Default.Get(c).Purpose, ["message"], [], "{{message}}", c);
            Assert.True((await resolver.ResolveAsync(property, Type(SmsCategory.DUES_NOTIFICATION), Phone, default)).Allowed);
            db.Add(new SmsOperationalPreference(Guid.NewGuid().ToString(), Phone, false)); await db.SaveChangesAsync();
            foreach (var c in new[] { SmsCategory.DUES_NOTIFICATION, SmsCategory.MAINTENANCE_NOTIFICATION, SmsCategory.RESERVATION_NOTIFICATION })
                Assert.Equal(SmsPolicyErrors.OperationalOptedOut, (await resolver.ResolveAsync(property, Type(c), Phone, default)).ErrorCode);
            foreach (var c in new[] { SmsCategory.SECURITY_OTP, SmsCategory.PASSWORD_RESET, SmsCategory.ACCOUNT_VERIFICATION, SmsCategory.EMERGENCY_NOTIFICATION })
                Assert.True((await resolver.ResolveAsync(property, Type(c), Phone, default)).Allowed);
            Assert.True((await resolver.ResolveAsync(property, Type(SmsCategory.DUES_NOTIFICATION), "+905559999999", default)).Allowed);
            Assert.Equal(SmsPolicyErrors.OperationalOptedOut, (await Queue(db).EnqueueAsync(property, SmsTypes.Manual, "0555 123 45 67", new Dictionary<string, string> { ["message"] = "Su kesintisi" }, null, null, default)).ErrorCode);
            await transaction.RollbackAsync();
        });
    }

    [PostgresFact] public async Task CommercialSmsIsBlockedUntilSenderOptOutAndApprovalExist()
    {
        var property = await NewProperty();
        await factory.WithDb(async db =>
        {
            Assert.Equal(SmsPolicyErrors.LegalSenderMissing, (await Queue(db).EnqueueAsync(property, Marketing, Phone, Body, null, null, default)).ErrorCode);
            var sender = new SmsLegalSender(property, "Gül Sitesi Yönetimi", null, "0212 000 00 00", "RET GUL", "4567", false, false, null);
            db.Add(sender); await db.SaveChangesAsync();
            Assert.Equal(SmsPolicyErrors.OptOutUnavailable, (await Queue(db).EnqueueAsync(property, Marketing, Phone, Body, null, null, default)).ErrorCode);
            sender.Update(sender.Title, "0123456789012345", sender.ContactInformation, sender.OptOutKeyword, sender.OptOutShortCode, true, false, null); await db.SaveChangesAsync();
            Assert.Equal(SmsPolicyErrors.CommercialConsentRequired, (await Queue(db).EnqueueAsync(property, Marketing, Phone, Body, null, null, default)).ErrorCode);
            Assert.False(await db.SmsMessages.AnyAsync(x => x.PropertyId == property));
            var consents = new CommercialConsentStore(db, TimeProvider.System);
            await consents.RecordAsync(new(sender.Id, Phone, true, CommercialConsentStatus.APPROVED, CommercialConsentSource.WEB, Guid.NewGuid(), "u", null, 1, Sha), default);
            var queued = await Queue(db).EnqueueAsync(property, Marketing, "0555 123 45 67", Body, null, null, default);
            Assert.NotNull(queued.MessageId); await db.SaveChangesAsync();
            var message = await db.SmsMessages.SingleAsync(x => x.Id == queued.MessageId);
            Assert.Equal("Yeni hizmetimiz\nGül Sitesi Yönetimi\nSMS almak istemiyorsanız RET GUL yazıp 4567 numarasına ücretsiz gönderiniz.\nMERSIS: 0123456789012345", message.MaskedBody);
            Assert.Equal(SmsPurpose.MARKETING, message.Purpose); Assert.Equal(SmsText.Segments(message.MaskedBody), message.Segments);
            await consents.RecordAsync(new(sender.Id, Phone, false, CommercialConsentStatus.REJECTED, CommercialConsentSource.SMS_REPLY, Guid.NewGuid()), default);
            Assert.Equal(SmsPolicyErrors.OptedOut, (await Queue(db).EnqueueAsync(property, Marketing, Phone, Body, null, null, default)).ErrorCode);
        });
    }

    [PostgresFact] public async Task IysEnabledSenderNeedsSyncedApproval()
    {
        var property = await NewProperty();
        await factory.WithDb(async db =>
        {
            var sender = new SmsLegalSender(property, "Gül", "0123456789012345", "c", "RET GUL", "4567", true, true, "GUL"); db.Add(sender); await db.SaveChangesAsync();
            var approval = await new CommercialConsentStore(db, TimeProvider.System).RecordAsync(new(sender.Id, Phone, true, CommercialConsentStatus.APPROVED, CommercialConsentSource.MOBILE, Guid.NewGuid(), "u", null, 1, Sha), default);
            Assert.Equal(SmsPolicyErrors.IysNotSynced, (await Queue(db).EnqueueAsync(property, Marketing, Phone, Body, null, null, default)).ErrorCode);
            var record = await db.CommercialConsentRecords.SingleAsync(x => x.Id == approval.RecordId);
            record.MarkIysFailed("IYS_TIMEOUT"); await db.SaveChangesAsync();
            Assert.Equal(SmsPolicyErrors.IysNotSynced, (await Queue(db).EnqueueAsync(property, Marketing, Phone, Body, null, null, default)).ErrorCode);
            record.MarkIysSynced("IYS-9", DateTimeOffset.UtcNow); await db.SaveChangesAsync();
            Assert.NotNull((await Queue(db).EnqueueAsync(property, Marketing, Phone, Body, null, null, default)).MessageId);
        });
    }

    [PostgresFact] public async Task PropertySenderWinsOverPlatformAndOperationalSmsStaysPlain()
    {
        var property = await NewProperty();
        await factory.WithDb(async db =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var platform = new SmsLegalSender(null, "BuildingFlow A.Ş.", "0123456789012345", "c", "RET BF", "4567", true, false, null);
            db.Add(platform); await db.SaveChangesAsync();
            var resolver = new SmsPolicyResolver(db, SmsPolicyCatalog.Default, new CommercialConsentStore(db, TimeProvider.System));
            Assert.Equal(platform.Id, (await resolver.ResolveAsync(property, Marketing, Phone, default)).LegalSenderId);
            var own = new SmsLegalSender(property, "Gül", null, "c", "RET GUL", "4567", true, false, null); db.Add(own); await db.SaveChangesAsync();
            Assert.Equal(own.Id, (await resolver.ResolveAsync(property, Marketing, Phone, default)).LegalSenderId);
            own.SetEnabled(false); await db.SaveChangesAsync();
            Assert.Equal(platform.Id, (await resolver.ResolveAsync(property, Marketing, Phone, default)).LegalSenderId);
            var manual = await Queue(db).EnqueueAsync(property, SmsTypes.Manual, Phone, new Dictionary<string, string> { ["message"] = "Su kesintisi" }, null, null, default);
            await db.SaveChangesAsync();
            Assert.Equal("Su kesintisi", (await db.SmsMessages.SingleAsync(x => x.Id == manual.MessageId)).MaskedBody);
            await transaction.RollbackAsync();
        });
    }

    private static SmsQueue Queue(BuildingFlowDbContext db) => new(db, new SmsPayloadProtector(new ConfigurationBuilder().Build()), TimeProvider.System);
    private async Task<Guid> NewProperty()
    {
        var property = new Property("Policy " + Guid.NewGuid(), "City", "District", "Address", PropertyType.APARTMENT);
        await factory.WithDb(async db => { db.Add(property); await db.SaveChangesAsync(); });
        return property.Id;
    }
}
