using System.Text.RegularExpressions;
using Xunit;
namespace BuildingFlow.UnitTests;

// KVKK boundary: the anonymous website path may read website rows and the Property availability flags only.
public sealed class WebsitePublicBoundaryTests
{
    // Identifier prefixes of every non-website table and of the authenticated management service.
    private static readonly string[] Forbidden = ["People", "Person", "Unit", "Occupanc", "Dues", "Payment", "Expense", "Budget", "Announcement", "StoredDocument",
        "Outbox", "Processed", "Sms", "Mobile", "Meter", "Consent", "PropertyMembership", "Building", "IWebsiteStore", "WebsiteService"];

    [Theory]
    [InlineData("src/BuildingFlow.Infrastructure/PublicWebsiteInquiryStore.cs")]
    [InlineData("src/BuildingFlow.Infrastructure/PublicListingStore.cs")]
    [InlineData("src/BuildingFlow.Api/PublicListingEndpoints.cs")]
    [InlineData("src/BuildingFlow.Infrastructure/PublicWebsiteDomainStore.cs")]
    [InlineData("src/BuildingFlow.Infrastructure/PublicWebsiteStore.cs")]
    [InlineData("src/BuildingFlow.Infrastructure/PublicWebsiteMediaStore.cs")]
    [InlineData("src/BuildingFlow.Api/PublicWebsiteEndpoints.cs")]
    [InlineData("src/BuildingFlow.Api/PublicWebsiteMediaEndpoints.cs")]
    public void PublicPathReadsOnlyWebsiteRowsAndPropertyAvailability(string relative)
    {
        var code = Source(relative);
        var identifiers = Regex.Matches(code, @"\b[A-Za-z_]\w*").Select(m => m.Value).Where(i => i != "BuildingFlow" && i != "BuildingLabel" && !i.StartsWith("BuildingFlowDb", StringComparison.Ordinal)).ToHashSet();
        foreach (var word in Forbidden) Assert.DoesNotContain(identifiers, i => i.StartsWith(word, StringComparison.Ordinal));
        foreach (Match m in Regex.Matches(code, @"\bdb\.(\w+)")) Assert.Contains(m.Groups[1].Value, relative.EndsWith("PublicWebsiteInquiryStore.cs",StringComparison.Ordinal) ? new[] { "Properties", "Set", "Database", "Add", "SaveChangesAsync" } : new[] { "Properties", "Set" });
        foreach (Match m in Regex.Matches(code, @"Set<(\w+)>")) Assert.True(m.Groups[1].Value.StartsWith("Website", StringComparison.Ordinal) || m.Groups[1].Value is "PropertyListing" or "ListingPhoto");
    }

    private static string Source(string relative)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "backend", relative);
            if (File.Exists(path)) return File.ReadAllText(path);
            path = Path.Combine(dir.FullName, relative);
            if (File.Exists(path)) return File.ReadAllText(path);
        }
        throw new FileNotFoundException(relative);
    }
}
