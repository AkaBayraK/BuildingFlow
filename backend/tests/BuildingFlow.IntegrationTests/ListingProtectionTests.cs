using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class ListingProtectionTests{
 [Fact]public void InquiryChallengeIsBoundToSiteIpVersionAndServerClock(){var clock=new Clock();var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Website:InquiryHmacKey",Convert.ToBase64String(new byte[32])}}).Build();var p=new WebsiteInquiryProtection(config,clock);var token=p.Issue("one","127.0.0.1","v1");Assert.False(p.Check(token,"one","127.0.0.1","v1"));clock.Now=clock.Now.AddSeconds(4);Assert.True(p.Check(token,"one","127.0.0.1","v1"));Assert.False(p.Check(token,"two","127.0.0.1","v1"));Assert.False(p.Check(token,"one","127.0.0.2","v1"));Assert.False(p.Check(token,"one","127.0.0.1","v2"));Assert.False(p.Check(token+"x","one","127.0.0.1","v1"));clock.Now=clock.Now.AddHours(1);Assert.False(p.Check(token,"one","127.0.0.1","v1"));}
 [Fact]public async Task MissingProvidersFailClosed(){Assert.Null(await new UnavailableListingAuthorityVerifier().Verify(Guid.NewGuid(),Guid.NewGuid(),default));var p=new WebsiteInquiryProtection(new ConfigurationBuilder().Build(),TimeProvider.System);Assert.Throws<HierarchyConflictException>(()=>p.Issue("one","ip","version"));}

private sealed class Clock:TimeProvider{public DateTimeOffset Now=DateTimeOffset.Parse("2026-10-18T12:00:00Z");public override DateTimeOffset GetUtcNow()=>Now;}
}
