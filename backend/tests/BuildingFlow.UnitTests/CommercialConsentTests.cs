using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class CommercialConsentTests
{
    private static readonly string Sha = new('a', 64);
    private static SmsLegalSender Sender(bool iys = false) => new(null, "BuildingFlow Teknoloji A.Ş.", "0123456789012345", "destek@example.test", "RET BF", "4567", true, iys, iys ? "BF" : null);
    private static CommercialConsentRecord Record(CommercialConsentStatus status, CommercialConsentSource source, bool verified = true, int? version = 1, string? sha = null, SmsLegalSender? sender = null) =>
        new(sender ?? Sender(), CommercialMessageChannel.SMS, "+905551234567", verified, status, source, DateTimeOffset.UtcNow, "user-1", null, version, sha ?? (version is null ? null : Sha), Guid.NewGuid(), Sha);

    [Theory] [InlineData("123")] [InlineData("012345678901234X")] [InlineData("01234567890123456")]
    public void MersisMustBeSixteenDigits(string mersis) => Assert.Throws<ArgumentException>(() => new SmsLegalSender(null, "T", mersis, "c", "RET BF", "4567", true, false, null));

    [Theory] [InlineData("STOP")] [InlineData("ret bf")] [InlineData("RE")]
    public void OptOutKeywordStartsWithRet(string keyword) => Assert.Throws<ArgumentException>(() => new SmsLegalSender(null, "T", null, "c", keyword, "4567", true, false, null));

    [Fact] public void ShortCodeIsDigitsAndIysNeedsBrandCode()
    {
        Assert.Throws<ArgumentException>(() => new SmsLegalSender(null, "T", null, "c", "RET BF", "45A7", true, false, null));
        Assert.Throws<ArgumentException>(() => new SmsLegalSender(null, "T", null, "c", "RET BF", "4567", true, true, null));
        Assert.Throws<ArgumentException>(() => new SmsLegalSender(Guid.Empty, "T", null, "c", "RET BF", "4567", true, false, null));
        Assert.Null(new SmsLegalSender(null, "Site Yönetimi", " ", "c", "RET SITE", "4567", true, false, null).MersisNumber);
    }

    [Theory] [InlineData(CommercialConsentSource.SMS_REPLY)] [InlineData(CommercialConsentSource.PROVIDER_CALLBACK)]
    public void ApprovalCannotBeCollectedBySms(CommercialConsentSource source) => Assert.Throws<ArgumentException>(() => Record(CommercialConsentStatus.APPROVED, source));

    [Fact] public void ApprovalNeedsVerifiedRecipientAndTextEvidence()
    {
        Assert.Throws<ArgumentException>(() => Record(CommercialConsentStatus.APPROVED, CommercialConsentSource.WEB, verified: false));
        Assert.Throws<ArgumentException>(() => Record(CommercialConsentStatus.APPROVED, CommercialConsentSource.WEB, version: null));
        Assert.Throws<ArgumentException>(() => Record(CommercialConsentStatus.APPROVED, CommercialConsentSource.WEB, sha: "XYZ"));
        Assert.Equal(CommercialConsentStatus.APPROVED, Record(CommercialConsentStatus.APPROVED, CommercialConsentSource.MOBILE).Status);
    }

    [Fact] public void RejectionNeedsNoTextAndMayArriveBySmsReply()
    {
        var record = Record(CommercialConsentStatus.REJECTED, CommercialConsentSource.SMS_REPLY, verified: false, version: null);
        Assert.Null(record.TextVersion); Assert.Equal(IysSyncStatus.NOT_REQUIRED, record.IysStatus);
    }

    [Theory] [InlineData("05551234567")] [InlineData("+05551234567")] [InlineData("+90555")]
    public void RecipientMustBeE164(string phone) => Assert.Throws<ArgumentException>(() =>
        new CommercialConsentRecord(Sender(), CommercialMessageChannel.SMS, phone, false, CommercialConsentStatus.REJECTED, CommercialConsentSource.SMS_REPLY, DateTimeOffset.UtcNow, null, null, null, null, Guid.NewGuid(), Sha));

    [Fact] public void IysSyncFollowsSenderAndOnlyMovesFromPending()
    {
        var record = Record(CommercialConsentStatus.REJECTED, CommercialConsentSource.WEB, sender: Sender(iys: true));
        Assert.Equal(IysSyncStatus.PENDING, record.IysStatus);
        record.MarkIysFailed("TIMEOUT"); Assert.Equal(IysSyncStatus.FAILED, record.IysStatus);
        record.MarkIysSynced("IYS-1", DateTimeOffset.UtcNow); Assert.Equal(IysSyncStatus.SYNCED, record.IysStatus); Assert.Null(record.IysErrorCode);
        Assert.Throws<InvalidOperationException>(() => record.MarkIysFailed("LATE"));
        Assert.Throws<InvalidOperationException>(() => Record(CommercialConsentStatus.REJECTED, CommercialConsentSource.WEB).MarkIysSynced("IYS-2", DateTimeOffset.UtcNow));
    }
}
