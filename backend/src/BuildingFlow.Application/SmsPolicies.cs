using System.Text.Json.Serialization;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public enum SmsCategory
{
    SECURITY_OTP, PASSWORD_RESET, ACCOUNT_VERIFICATION, DUES_NOTIFICATION,
    MAINTENANCE_NOTIFICATION, RESERVATION_NOTIFICATION, EMERGENCY_NOTIFICATION, MARKETING
}
// SECURITY: identity/account safety; SERVICE: performance of the management relation (KMK/KVKK m.5/2-c);
// VITAL_INTEREST: life/property safety (KVKK m.5/2-b); COMMERCIAL_CONSENT: 6563 m.6 prior approval.
public enum SmsLegalBasis { SECURITY, SERVICE, VITAL_INTEREST, COMMERCIAL_CONSENT }
// NONE: cannot be switched off by the recipient; OPERATIONAL: follows the recipient's operational SMS choice; MARKETING: needs an active commercial approval.
public enum SmsPreferenceRule { NONE, OPERATIONAL, MARKETING }
// NONE: no opt-out footer (adding one to an OTP would misclassify it); COMMERCIAL: sender identity + free opt-out are mandatory or the SMS is not sent.
public enum SmsFooterPolicy { NONE, COMMERCIAL }

public sealed record SmsCategoryPolicy(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] SmsCategory Category,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] SmsPurpose Purpose,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] SmsLegalBasis LegalBasis,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] SmsPreferenceRule Preference,
    bool RequiresCommercialConsent,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] SmsFooterPolicy Footer,
    bool AllowsPromotionalContent);

// Kept behind an interface so the catalogue can later be served by Aka Integration Hub without touching callers.
public interface ISmsPolicyCatalog
{
    IReadOnlyList<SmsCategoryPolicy> All { get; }
    SmsCategoryPolicy Get(SmsCategory category);
}

public sealed class SmsPolicyCatalog : ISmsPolicyCatalog
{
    public static SmsPolicyCatalog Default { get; } = new();
    public IReadOnlyList<SmsCategoryPolicy> All { get; } = Array.AsReadOnly(new[]
    {
        Service(SmsCategory.SECURITY_OTP, SmsPurpose.TRANSACTIONAL, SmsLegalBasis.SECURITY, SmsPreferenceRule.NONE),
        Service(SmsCategory.PASSWORD_RESET, SmsPurpose.TRANSACTIONAL, SmsLegalBasis.SECURITY, SmsPreferenceRule.NONE),
        Service(SmsCategory.ACCOUNT_VERIFICATION, SmsPurpose.TRANSACTIONAL, SmsLegalBasis.SECURITY, SmsPreferenceRule.NONE),
        Service(SmsCategory.DUES_NOTIFICATION, SmsPurpose.OPERATIONAL, SmsLegalBasis.SERVICE, SmsPreferenceRule.OPERATIONAL),
        Service(SmsCategory.MAINTENANCE_NOTIFICATION, SmsPurpose.OPERATIONAL, SmsLegalBasis.SERVICE, SmsPreferenceRule.OPERATIONAL),
        Service(SmsCategory.RESERVATION_NOTIFICATION, SmsPurpose.OPERATIONAL, SmsLegalBasis.SERVICE, SmsPreferenceRule.OPERATIONAL),
        Service(SmsCategory.EMERGENCY_NOTIFICATION, SmsPurpose.OPERATIONAL, SmsLegalBasis.VITAL_INTEREST, SmsPreferenceRule.NONE),
        new SmsCategoryPolicy(SmsCategory.MARKETING, SmsPurpose.MARKETING, SmsLegalBasis.COMMERCIAL_CONSENT, SmsPreferenceRule.MARKETING, true, SmsFooterPolicy.COMMERCIAL, true)
    });
    public SmsCategoryPolicy Get(SmsCategory category) =>
        All.SingleOrDefault(policy => policy.Category == category) ?? throw new ArgumentOutOfRangeException(nameof(category));
    private static SmsCategoryPolicy Service(SmsCategory category, SmsPurpose purpose, SmsLegalBasis basis, SmsPreferenceRule preference) =>
        new(category, purpose, basis, preference, false, SmsFooterPolicy.NONE, false);
}
