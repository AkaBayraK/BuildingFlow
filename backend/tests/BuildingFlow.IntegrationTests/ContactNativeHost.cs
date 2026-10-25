using System.Net;
using System.Net.Http.Headers;
using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;
namespace BuildingFlow.IntegrationTests;
public sealed class NativeContactFactAttribute:FactAttribute
{
    public NativeContactFactAttribute(){if(Environment.GetEnvironmentVariable("BUILDINGFLOW_CONTACT_NATIVE")!="1")Skip="Opt-in native acceptance; scripts/test-contact-native.py.";}
}
public sealed class ContactNativeHost(SiteApiTests.SiteApiFactory factory):IClassFixture<SiteApiTests.SiteApiFactory>
{
    [NativeContactFact] public async Task NativeContactApiWithRealOidc()
    {
        var issuer=Environment.GetEnvironmentVariable("BF_NATIVE_ISSUER")!;var secret=Environment.GetEnvironmentVariable("BF_NATIVE_ADMIN_SECRET")!;
        Assert.StartsWith("http://127.0.0.1:",issuer);Assert.EndsWith("/realms/buildingflow",issuer);Assert.True(secret.Length>=16);
        var manager=new ConfigurationManager<OpenIdConnectConfiguration>(issuer+"/.well-known/openid-configuration",new OpenIdConnectConfigurationRetriever(),new HttpDocumentRetriever{RequireHttps=false});
        var metadata=await manager.GetConfigurationAsync(default);
        using var host=factory.WithWebHostBuilder(b=>b.ConfigureServices(s=>{
            s.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,o=>{o.Authority=issuer;o.ConfigurationManager=new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);o.TokenValidationParameters.ValidIssuer=issuer;});
            s.RemoveAll<IKeycloakAdmin>();s.AddSingleton<IKeycloakAdmin>(new KeycloakAdminClient(new KeycloakAdminOptions{BaseUrl=issuer[..issuer.IndexOf("/realms/",StringComparison.Ordinal)],Realm="buildingflow",ClientId="native-test-admin",ClientSecret=secret,EmailVerificationEnabled=true},new HttpClient{Timeout=TimeSpan.FromSeconds(10)},TimeProvider.System));
            s.RemoveAll<IPhoneOtpSender>();s.AddSingleton<IPhoneOtpSender>(new NativeSender());
            s.RemoveAll<IPhoneOtpSecrets>();s.AddSingleton<IPhoneOtpSecrets>(new NativeSecrets());
        }));
        using var client=host.CreateClient();using var listener=new HttpListener();listener.Prefixes.Add("http://127.0.0.1:13048/");listener.Start();
        Console.WriteLine("[BUILDINGFLOW] BF-VERIFY05 real OIDC/API/PostgreSQL native bridge READY :13048; SMS test sender only");
        using var limit=new CancellationTokenSource(TimeSpan.FromMinutes(50));
        while(!limit.IsCancellationRequested){
            var c=await listener.GetContextAsync().WaitAsync(limit.Token);var request=c.Request;var response=c.Response;var path=request.Url!.PathAndQuery;response.Headers["Cache-Control"]="no-store";
            if(request.Headers["Origin"]=="http://127.0.0.1:13057"){
                response.Headers["Access-Control-Allow-Origin"]="http://127.0.0.1:13057";response.Headers["Access-Control-Allow-Headers"]="Authorization,Content-Type";response.Headers["Access-Control-Allow-Methods"]="GET,POST,OPTIONS";
                if(request.HttpMethod=="OPTIONS"){response.StatusCode=204;response.Close();continue;}
            }
            if(path=="/fixture-stop"&&request.Headers["Authorization"]=="Bearer fixture-only"){response.StatusCode=204;response.Close();break;}
            if(!path.StartsWith("/api/v1/mobile/",StringComparison.Ordinal)){response.StatusCode=404;response.Close();continue;}
            using var message=new HttpRequestMessage(new HttpMethod(request.HttpMethod),path);
            if(AuthenticationHeaderValue.TryParse(request.Headers["Authorization"],out var authorization))message.Headers.Authorization=authorization;
            if(request.HasEntityBody){message.Content=new StreamContent(request.InputStream);message.Content.Headers.ContentType=new("application/json");}
            using var result=await client.SendAsync(message,limit.Token);response.StatusCode=(int)result.StatusCode;response.ContentType=result.Content.Headers.ContentType?.ToString()??"application/json";
            await result.Content.CopyToAsync(response.OutputStream,limit.Token);response.Close();
        }
    }
    private sealed class NativeSender:IPhoneOtpSender
    {
        public bool Available=>true;
        public Task<PhoneOtpDelivery> SendAsync(string phone,string code,CancellationToken token)=>Task.FromResult(new PhoneOtpDelivery("ACCEPTED","native-test-only"));
    }
    // Fixed code belongs only to this ephemeral test fixture. Crypto/persistence still use the real implementation.
    private sealed class NativeSecrets:IPhoneOtpSecrets
    {
        private readonly PhoneOtpSecrets real=new(PhoneOtpTransportTests.Options());
        public bool Available=>real.Available;
        public string Generate()=>"123456";
        public string Hash(string purpose,string value)=>real.Hash(purpose,value);
        public string Protect(string value)=>real.Protect(value);
        public string Unprotect(string value)=>real.Unprotect(value);
        public bool Matches(string digest,string purpose,string value)=>real.Matches(digest,purpose,value);
    }
}
