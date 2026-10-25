using System.Net;
using System.Security.Cryptography;
using System.Xml.Linq;
using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed class PhoneOtpTransportTests
{
    [Fact] public async Task RealLoopbackHttpServerChecksNetgsmXmlAndSafeFailuresWithoutSendingSms()
    {
        var builder=WebApplication.CreateBuilder();builder.Logging.ClearProviders();builder.Configuration["AllowedHosts"]="127.0.0.1";builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var app=builder.Build();string reply="<xml><main><code>0</code><jobID>12345</jobID></main></xml>";int requests=0;
        app.MapPost("/sms/send/otp",async context=>{
            requests++;Assert.False(context.Request.QueryString.HasValue);
            var body=await XDocument.LoadAsync(context.Request.Body,LoadOptions.None,default);
            Assert.True(body.Root?.Element("header")?.Element("usercode")?.Value=="fixture-user");
            Assert.True(body.Root?.Element("header")?.Element("password")?.Value=="fixture-password");
            Assert.True(body.Root?.Element("body")?.Element("no")?.Value=="5551234567");
            Assert.True(body.Root?.Element("body")?.Element("msg")?.Value.Contains("123456")==true);
            context.Response.ContentType="application/xml";
            if(reply=="STALL")
            {
                await context.Response.WriteAsync("<xml>");await context.Response.Body.FlushAsync();
                try{await Task.Delay(Timeout.InfiniteTimeSpan,context.RequestAborted);}catch(OperationCanceledException){}
                return;
            }
            await context.Response.WriteAsync(reply);
        });
        await app.StartAsync();var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var http=new HttpClient(new LoopbackOnly(new Uri(address))){Timeout=TimeSpan.FromSeconds(5)};
        var sender=new NetgsmPhoneOtpSender(Options(),http);
        Assert.Equal("ACCEPTED",(await sender.SendAsync("+905551234567","123456",default)).Status);
        reply="<xml><main><code>30</code><error>private-marker</error></main></xml>";
        var failure=await sender.SendAsync("+905551234567","123456",default);Assert.Equal("FAILED",failure.Status);Assert.Null(failure.ProviderMessageId);
        reply="not-xml private-marker";Assert.Equal("UNKNOWN",(await sender.SendAsync("+905551234567","123456",default)).Status);
        reply="<!DOCTYPE xml [<!ENTITY xxe SYSTEM 'file:///etc/passwd'>]><xml><main><code>&xxe;</code></main></xml>";
        Assert.Equal("UNKNOWN",(await sender.SendAsync("+905551234567","123456",default)).Status);Assert.Equal(4,requests);
        reply="STALL";
        using var shortHttp=new HttpClient(new LoopbackOnly(new Uri(address))){Timeout=TimeSpan.FromMilliseconds(150)};
        Assert.Equal("UNKNOWN",(await new NetgsmPhoneOtpSender(Options(),shortHttp).SendAsync("+905551234567","123456",default)).Status);
        Assert.Equal(5,requests);
        await app.StopAsync();
    }
    [Fact] public async Task TimeoutIsUnknownAndDisabledTransportMakesNoHttpCall()
    {
        using var http=new HttpClient(new Failure());
        Assert.Equal("UNKNOWN",(await new NetgsmPhoneOtpSender(Options(),http).SendAsync("+905551234567","123456",default)).Status);
        Assert.Equal("UNAVAILABLE",(await new NetgsmPhoneOtpSender(new(),http).SendAsync("+905551234567","123456",default)).Status);
    }
    [Fact] public void HmacAndCipherArePurposeBoundAndNeverFallBackToADevelopmentSecret()
    {
        Assert.False(new PhoneOtpSecrets(new()).Available);var secrets=new PhoneOtpSecrets(Options());
        var digest=secrets.Hash("proof","owner/challenge/phone/code");
        Assert.True(secrets.Matches(digest,"proof","owner/challenge/phone/code"));Assert.False(secrets.Matches(digest,"command","owner/challenge/phone/code"));
        Assert.False(secrets.Matches(digest,"proof","other/challenge/phone/code"));
        var a=secrets.Protect("private-marker");var b=secrets.Protect("private-marker");Assert.NotEqual(a,b);Assert.DoesNotContain("private-marker",a);Assert.Equal("private-marker",secrets.Unprotect(a));
        var wrong=new PhoneOtpSecrets(new(){HmacKey=new string('B',64)});Assert.ThrowsAny<CryptographicException>(()=>wrong.Unprotect(a));
        for(var n=0;n<100;n++)Assert.True(PhoneOtpPolicy.IsCode(secrets.Generate()));
    }
    internal static PhoneOtpOptions Options()=>new(){Enabled=true,Usercode="fixture-user",Password="fixture-password",Sender="BLDGFLOW",HmacKey=new string('A',64)};
    private sealed class Failure:HttpMessageHandler{protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct)=>throw new TaskCanceledException();}
    private sealed class LoopbackOnly(Uri address):DelegatingHandler(new HttpClientHandler{AllowAutoRedirect=false})
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {Assert.Equal("https://api.netgsm.com.tr/sms/send/otp",request.RequestUri!.AbsoluteUri);request.RequestUri=new Uri(address,request.RequestUri.PathAndQuery);return base.SendAsync(request,token);}
    }
}
