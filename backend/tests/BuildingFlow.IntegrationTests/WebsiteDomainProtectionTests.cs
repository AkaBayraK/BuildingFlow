using BuildingFlow.Application;
using System.Net;
using System.Text.Json;
using BuildingFlow.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class WebsiteDomainProtectionTests{
 [Theory][InlineData("127.0.0.1",false)][InlineData("10.0.0.1",false)][InlineData("192.168.1.1",false)][InlineData("169.254.169.254",false)][InlineData("100.64.1.1",false)][InlineData("::1",false)][InlineData("::ffff:127.0.0.1",false)][InlineData("fd00::1",false)][InlineData("fe80::1",false)][InlineData("8.8.8.8",true)][InlineData("2606:4700:4700::1111",true)]public void TlsProbesCannotReachPrivateDestinations(string value,bool expected)=>Assert.Equal(expected,WebsiteDomainVerifier.PublicAddress(IPAddress.Parse(value)));
 [Fact]public void TxtRequiresExactNameTypeAndSecretHash(){var token=new string('a',64);var hash=WebsiteImage.Sha(System.Text.Encoding.UTF8.GetBytes(token));string Json(string name,int type,string data,int status=0)=>JsonSerializer.Serialize(new{Status=status,Answer=new[]{new{name,type,data}}});var name="_buildingflow-verify.example.com";Assert.True(WebsiteDomainVerifier.TxtMatches(Json(name+".",16,"\""+token+"\""),name,hash));Assert.False(WebsiteDomainVerifier.TxtMatches(Json("other.example.com",16,token),name,hash));Assert.False(WebsiteDomainVerifier.TxtMatches(Json(name,1,token),name,hash));Assert.False(WebsiteDomainVerifier.TxtMatches(Json(name,16,new string('b',64)),name,hash));Assert.False(WebsiteDomainVerifier.TxtMatches(Json(name,16,token,3),name,hash));}
 [Fact]public void PreviewSignatureExpiryAndVersionAreBound(){var clock=new Clock();var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Website:PreviewHmacKey",Convert.ToBase64String(new byte[32])}}).Build();var p=new WebsitePreviewProtection(config,clock);var site=Guid.NewGuid();var issued=p.Issue(site,7);Assert.Equal((site,7L),p.Check(issued.Token));Assert.Null(p.Check(issued.Token+"x"));clock.Now=clock.Now.AddMinutes(11);Assert.Null(p.Check(issued.Token));}
 private sealed class Clock:TimeProvider{public DateTimeOffset Now=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>Now;}
}
