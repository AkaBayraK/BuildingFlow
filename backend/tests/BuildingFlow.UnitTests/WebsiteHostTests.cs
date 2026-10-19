using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class WebsiteHostTests{
 [Theory][InlineData("Example.COM","example.com")][InlineData("örnek.com","xn--rnek-4qa.com")][InlineData("site.example.com.","site.example.com")]public void NormalizesIdnAndCase(string input,string expected)=>Assert.Equal(expected,WebsiteDomain.Normalize(input));
 [Theory][InlineData("127.0.0.1")][InlineData("http://example.com")][InlineData("example.com:443")][InlineData("x.local")][InlineData("user@example.com")][InlineData("a..com")][InlineData("*.example.com")][InlineData("example.com/path")][InlineData(" example.com")][InlineData("example.com?x")]
 public void RejectsHostsThatAreNotBarePublicDnsNames(string input)=>Assert.Throws<ArgumentException>(()=>WebsiteDomain.Normalize(input));
 [Fact]public void OwnershipTlsAndCustomApprovalAreIndependentGates(){var d=new WebsiteDomain(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"site.example.com","CUSTOM",new string('a',64));var now=DateTimeOffset.UtcNow;d.Check(true,true,now);Assert.Equal("PENDING",d.Status);d.Approve(now);d.Check(true,false,now);Assert.Equal("PENDING",d.Status);d.Check(true,true,now);d.Primary(true);Assert.True(d.IsPrimary);d.Rotate(new string('b',64));Assert.Equal("PENDING",d.Status);Assert.False(d.IsPrimary);d.Disable();Assert.Equal("DISABLED",d.Status);}
}
