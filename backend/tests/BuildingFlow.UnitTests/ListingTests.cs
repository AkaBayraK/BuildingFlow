using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class ListingTests{
 private static PropertyListing Draft(){var l=new PropertyListing(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),new string('a',64));l.Edit(null,"SALE","Public title","Public description",100,"3+1",150,120,null,null,"[]");return l;}
 [Fact]public void ApprovalCannotReplaceVerifiedAuthority(){var l=Draft();var now=DateTimeOffset.UtcNow;Assert.Throws<ArgumentException>(()=>l.Transition("SUBMIT",null,now));l.Consent("WRITTEN_STATEMENT","private/key",new string('b',64),"application/pdf",20,null);l.Transition("SUBMIT",null,now);Assert.Throws<ArgumentException>(()=>l.Transition("APPROVE","",now));l.Transition("APPROVE","Reviewed",now);Assert.Throws<ArgumentException>(()=>l.Transition("PUBLISH",null,now,now.AddDays(30)));Assert.Throws<ArgumentException>(()=>l.Transition("PUBLISH",null,now,now.AddDays(91),"verified"));l.Transition("PUBLISH",null,now,now.AddDays(30),"verified");Assert.Equal("PUBLISHED",l.Status);l.Edit(null,"SALE","Changed","Body",200,"2+1",100,80,null,null,"[]");Assert.Equal("PENDING_APPROVAL",l.Status);Assert.Null(l.VerificationReference);Assert.Null(l.PublishUntil);}
 [Theory][InlineData(0,100,80,"3+1")][InlineData(1.001,100,80,"3+1")][InlineData(1,80,100,"3+1")][InlineData(1,100,80,"studio")]
 public void InvalidPriceAreaAndRoomsFail(decimal price,decimal gross,decimal net,string rooms){var l=Draft();Assert.Throws<ArgumentException>(()=>l.Edit(null,"SALE","Title","Body",price,rooms,gross,net,null,null,"[]"));}
 [Fact]public void RetentionClearsAllContactAndIpFields(){var x=new WebsiteInquiry(Guid.NewGuid(),Guid.NewGuid(),null,"Name","Phone","mail@example.test","Private text","version","hash",DateTimeOffset.UtcNow);x.Anonymize();Assert.Null(x.Phone);Assert.Null(x.Email);Assert.Equal("",x.IpHash);Assert.Equal("[ANONYMIZED]",x.Message);Assert.False(x.IsEnabled);}
 private sealed class Clock:TimeProvider{public DateTimeOffset Now=DateTimeOffset.Parse("2026-10-18T12:00:00Z");public override DateTimeOffset GetUtcNow()=>Now;}
}
