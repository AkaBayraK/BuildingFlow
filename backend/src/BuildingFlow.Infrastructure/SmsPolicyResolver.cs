using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed class SmsPolicyResolver(BuildingFlowDbContext db, ISmsPolicyCatalog catalog, ICommercialConsentStore consents) : ISmsPolicyResolver
{
    public async Task<SmsResolution> ResolveAsync(Guid property, SmsTypeDefinition type, string? phone, CancellationToken token)
    {
        var policy = catalog.Get(type.Category);
        if (policy.Footer == SmsFooterPolicy.NONE && !policy.RequiresCommercialConsent) return SmsResolution.Plain;
        // The Property's own legal sender wins; otherwise the platform company sends in its own name.
        var sender = await db.SmsLegalSenders.AsNoTracking().Where(x => x.IsEnabled && (x.PropertyId == property || x.PropertyId == null))
            .OrderBy(x => x.PropertyId == null).FirstOrDefaultAsync(token);
        if (sender is null) return SmsResolution.Blocked(SmsPolicyErrors.LegalSenderMissing);
        if (!SmsLegalFooter.CanOptOut(sender)) return SmsResolution.Blocked(SmsPolicyErrors.OptOutUnavailable, sender.Id);
        if (policy.RequiresCommercialConsent && phone is not null)
        {
            var current = await consents.CurrentAsync(sender.Id, phone, token);
            if (current is null) return SmsResolution.Blocked(SmsPolicyErrors.CommercialConsentRequired, sender.Id);
            if (current.Status != nameof(CommercialConsentStatus.APPROVED)) return SmsResolution.Blocked(SmsPolicyErrors.OptedOut, sender.Id);
            if (sender.IysIntegrationEnabled && current.IysStatus != nameof(IysSyncStatus.SYNCED)) return SmsResolution.Blocked(SmsPolicyErrors.IysNotSynced, sender.Id);
        }
        return new(true, null, policy.Footer == SmsFooterPolicy.COMMERCIAL ? SmsLegalFooter.Build(sender) : "", sender.Id);
    }
}
