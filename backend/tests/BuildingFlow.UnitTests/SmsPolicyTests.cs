using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class SmsPolicyTests
{
    private static readonly ISmsPolicyCatalog Catalog = SmsPolicyCatalog.Default;

    [Fact] public void EveryCategoryHasExactlyOnePolicy() =>
        Assert.Equal(Enum.GetValues<SmsCategory>().OrderBy(x => x), Catalog.All.Select(x => x.Category).OrderBy(x => x));

    [Fact] public void OnlyMarketingNeedsCommercialConsentFooterAndPromotion()
    {
        foreach (var policy in Catalog.All)
        {
            var marketing = policy.Category == SmsCategory.MARKETING;
            Assert.Equal(marketing, policy.RequiresCommercialConsent);
            Assert.Equal(marketing ? SmsFooterPolicy.COMMERCIAL : SmsFooterPolicy.NONE, policy.Footer);
            Assert.Equal(marketing, policy.AllowsPromotionalContent);
            Assert.Equal(marketing, policy.LegalBasis == SmsLegalBasis.COMMERCIAL_CONSENT);
            Assert.Equal(marketing, policy.Purpose == SmsPurpose.MARKETING);
            Assert.Equal(marketing, policy.Preference == SmsPreferenceRule.MARKETING);
        }
    }

    [Theory]
    [InlineData(SmsCategory.SECURITY_OTP)] [InlineData(SmsCategory.PASSWORD_RESET)] [InlineData(SmsCategory.ACCOUNT_VERIFICATION)] [InlineData(SmsCategory.EMERGENCY_NOTIFICATION)]
    public void SecurityAndEmergencyCannotBeSwitchedOff(SmsCategory category) => Assert.Equal(SmsPreferenceRule.NONE, Catalog.Get(category).Preference);

    [Theory]
    [InlineData(SmsCategory.DUES_NOTIFICATION)] [InlineData(SmsCategory.MAINTENANCE_NOTIFICATION)] [InlineData(SmsCategory.RESERVATION_NOTIFICATION)]
    public void ServiceNotificationsFollowOperationalPreference(SmsCategory category)
    {
        var policy = Catalog.Get(category);
        Assert.Equal(SmsPreferenceRule.OPERATIONAL, policy.Preference); Assert.Equal(SmsLegalBasis.SERVICE, policy.LegalBasis); Assert.Equal(SmsPurpose.OPERATIONAL, policy.Purpose);
    }

    [Fact] public void ExistingTypesKeepTheirPurposeAndNeverNeedCommercialConsent()
    {
        foreach (var type in SmsTypes.All)
        {
            var policy = Catalog.Get(type.Category);
            Assert.Equal(policy.Purpose, type.Purpose); Assert.False(policy.RequiresCommercialConsent);
        }
        Assert.Equal(SmsCategory.ACCOUNT_VERIFICATION, SmsTypes.Find(SmsTypes.PersonRegistered)!.Category);
        Assert.Equal(SmsCategory.MAINTENANCE_NOTIFICATION, SmsTypes.Find(SmsTypes.Manual)!.Category);
    }

    [Fact] public void UnknownCategoryIsRejected() => Assert.Throws<ArgumentOutOfRangeException>(() => Catalog.Get((SmsCategory)99));
}
