using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Infrastructure;
using Xunit;
namespace BuildingFlow.IntegrationTests;

// Explicit opt-in browser fixture. Never reachable from the application/production host.
public sealed class ContactBrowserFactAttribute : FactAttribute
{
    public ContactBrowserFactAttribute(){if(Environment.GetEnvironmentVariable("BUILDINGFLOW_CONTACT_BROWSER")!="1")Skip="Opt-in isolated browser harness; run scripts/test-contact-web.py.";}
}
public sealed partial class UserManagementTests
{
    [ContactBrowserFact] public async Task PhoneContactBrowserHost()
    {
        Assert.Contains("test",Environment.GetEnvironmentVariable("BUILDINGFLOW_TEST_DATABASE"),StringComparison.OrdinalIgnoreCase);
        const string owner="77777777-7777-4777-8777-777777777777";
        var sender=new OtpSender();var clock=new OtpClock();
        await OtpClient(owner,sender,clock,async client=>{
            await client.PostAsJsonAsync("/api/v1/account/profile/commands",new MobileProfileCommand(Guid.NewGuid(),0,"Ada","DEMO","05551234567","KEEP",null));
            using var listener=new HttpListener();listener.Prefixes.Add("http://127.0.0.1:13048/");listener.Start();
            Console.WriteLine("[BUILDINGFLOW] BF-VERIFY04 real isolated PostgreSQL/API browser bridge READY :13048");
            using var limit=new CancellationTokenSource(TimeSpan.FromMinutes(25));
            while(!limit.IsCancellationRequested){
                var context=await listener.GetContextAsync().WaitAsync(limit.Token);var request=context.Request;var response=context.Response;
                response.Headers["Cache-Control"]="no-store";
                if(request.Headers["Authorization"]!="Bearer fixture-only"){response.StatusCode=401;response.Close();continue;}
                var path=request.Url!.PathAndQuery;
                if(path=="/fixture-stop"){response.StatusCode=204;response.Close();break;}
                if(path=="/fixture-external"){
                    var current=(await client.GetFromJsonAsync<ContactVerificationDto>(Contacts))!;
                    var send=await OtpCommand(client,new(Guid.NewGuid(),current.Version,"SEND"));
                    var verified=await client.PostAsJsonAsync("/api/v1/mobile/contact-verification/phone/commands",new PhoneOtpCommand(Guid.NewGuid(),send.Contact.Version,"VERIFY",send.Contact.Phone.ChallengeId,sender.Messages.Last().Code));
                    response.StatusCode=(int)verified.StatusCode;await verified.Content.CopyToAsync(response.OutputStream);response.Close();continue;
                }
                if(!path.StartsWith("/api/v1/account",StringComparison.Ordinal)){response.StatusCode=404;response.Close();continue;}
                using var message=new HttpRequestMessage(new HttpMethod(request.HttpMethod),path);
                if(request.HasEntityBody){message.Content=new StreamContent(request.InputStream);message.Content.Headers.ContentType=new("application/json");}
                // The signed test JWT stays server-side. Only the fixture's own fixed subject is used.
                using var result=await client.SendAsync(message,limit.Token);response.StatusCode=(int)result.StatusCode;response.ContentType=result.Content.Headers.ContentType?.ToString()??"application/json";
                await result.Content.CopyToAsync(response.OutputStream,limit.Token);response.Close();
            }
        });
    }
}
