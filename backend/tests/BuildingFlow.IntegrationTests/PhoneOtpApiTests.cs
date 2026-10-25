using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed partial class UserManagementTests
{
    private const string OtpPath="/api/v1/account/contact-verification/phone/commands";
    private async Task OtpClient(string owner,OtpSender sender,OtpClock clock,Func<HttpClient,Task> action,string ip="192.0.2.88")
    {
        var identity=new FakeKeycloak();identity.Add("otp.owner",[],owner);
        using var host=factory.WithWebHostBuilder(b=>b.ConfigureServices(s=>{
            s.RemoveAll<IKeycloakAdmin>();s.AddSingleton<IKeycloakAdmin>(identity);
            s.RemoveAll<IPhoneOtpSender>();s.AddSingleton<IPhoneOtpSender>(sender);
            s.RemoveAll<IPhoneOtpSecrets>();s.AddSingleton<IPhoneOtpSecrets>(new PhoneOtpSecrets(PhoneOtpTransportTests.Options()));
            s.RemoveAll<TimeProvider>();s.AddSingleton<TimeProvider>(clock);s.AddSingleton<IStartupFilter>(new OtpIp(ip));
        }));
        using var client=host.CreateClient();using var signed=TestToken.Client(factory,owner,"RESIDENT");client.DefaultRequestHeaders.Authorization=signed.DefaultRequestHeaders.Authorization;
        await action(client);
    }
    private static async Task<ContactVerificationDto> OtpStage(HttpClient client,string phone="05551234567",long version=0)
    {
        var response=await client.PostAsJsonAsync(Contacts+"/phone",new StagePhoneRequest(Guid.NewGuid(),version,phone));Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StagePhoneResult>())!.Contact;
    }
    private static async Task<PhoneOtpResult> OtpCommand(HttpClient client,PhoneOtpCommand command)
    {
        using var response=await client.PostAsJsonAsync(OtpPath,command);Assert.Equal(HttpStatusCode.OK,response.StatusCode);return (await response.Content.ReadFromJsonAsync<PhoneOtpResult>())!;
    }
    [PostgresFact] public async Task PhoneOtpRoundTripReplayWrongCodeAndSharedProfileAreAtomic()
    {
        var owner=Guid.NewGuid().ToString();var sender=new OtpSender();var clock=new OtpClock();
        await OtpClient(owner,sender,clock,async client=>{
            var staged=await OtpStage(client);
            var send=new PhoneOtpCommand(Guid.NewGuid(),staged.Version,"SEND");var sent=await OtpCommand(client,send);Assert.Equal("ACCEPTED",sent.Outcome);Assert.Equal(1,sender.Calls);
            Assert.True((await OtpCommand(client,send)).Replayed);Assert.Equal(1,sender.Calls);
            var p=(await client.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!;
            using(var edit=await client.PostAsJsonAsync("/api/v1/mobile/profile/commands",new MobileProfileCommand(Guid.NewGuid(),p.Version,"Updated",p.LastName,"+90 555 123 45 67","KEEP",null)))Assert.Equal(HttpStatusCode.OK,edit.StatusCode);
            var unchanged=(await client.GetFromJsonAsync<ContactVerificationDto>(Contacts))!;
            Assert.Equal(sent.Contact.Phone.ChallengeId,unchanged.Phone.ChallengeId);Assert.Equal("ACCEPTED",unchanged.Phone.DeliveryStatus);Assert.Null(unchanged.Phone.VerifiedAt);
            var originalHeader=client.DefaultRequestHeaders.Authorization;
            using(var other=TestToken.Client(factory,Guid.NewGuid().ToString(),"SUPER_ADMIN"))
            {
                client.DefaultRequestHeaders.Authorization=other.DefaultRequestHeaders.Authorization;
                Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync(OtpPath,new PhoneOtpCommand(Guid.NewGuid(),0,"VERIFY",sent.Contact.Phone.ChallengeId,sender.Messages.Last().Code))).StatusCode);
                client.DefaultRequestHeaders.Authorization=originalHeader;
            }
            var code=sender.Messages.Last().Code;var wrong=code=="000000"?"111111":"000000";
            var attempt=new PhoneOtpCommand(Guid.NewGuid(),sent.Contact.Version,"VERIFY",sent.Contact.Phone.ChallengeId,wrong);
            Assert.Equal("REJECTED",(await OtpCommand(client,attempt)).Outcome);Assert.True((await OtpCommand(client,attempt)).Replayed);
            Assert.Equal(4,(await client.GetFromJsonAsync<ContactVerificationDto>(Contacts))!.Phone.AttemptsRemaining);
            Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(OtpPath,attempt with{Code=code})).StatusCode);
            sender.Enabled=false; // A previously delivered proof can complete without another provider call.
            var verify=attempt with{OperationId=Guid.NewGuid(),Code=code};var verified=await OtpCommand(client,verify);
            Assert.Equal("VERIFIED",verified.Outcome);Assert.Equal("VERIFIED",verified.Contact.Phone.Status);Assert.True((await OtpCommand(client,verify)).Replayed);
            var mobile=(await client.GetFromJsonAsync<ContactVerificationDto>("/api/v1/mobile/contact-verification"))!;Assert.Equal(verified.Contact.Version,mobile.Version);Assert.Equal(verified.Contact.Phone.VerifiedAt,mobile.Phone.VerifiedAt);
            Assert.Equal("+905551234567",(await client.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!.Phone);
        });
        await factory.WithDb(async db=>{
            var challenge=await db.Set<PhoneOtpChallenge>().SingleAsync(x=>x.OwnerUserId==owner);Assert.Null(challenge.CodeMac);Assert.Null(challenge.ProtectedPayload);Assert.Equal(owner,challenge.CreatedBy);
            var receipts=await db.Set<MobileOperationReceipt>().Where(x=>x.OwnerUserId==owner).ToListAsync();Assert.All(receipts,x=>{Assert.DoesNotContain("Code",x.Result);Assert.DoesNotContain("0555",x.Result);});
            Assert.False(await db.PropertyMemberships.AnyAsync(x=>x.UserId==owner));
        });
    }
    [PostgresFact] public async Task PhoneOtpConcurrentSendAndVerificationUseOneEffectAndNoPropertyCredit()
    {
        var owner=Guid.NewGuid().ToString();var sender=new OtpSender();var clock=new OtpClock();
        await OtpClient(owner,sender,clock,async client=>{
            var staged=await OtpStage(client,"05552223344");var send=new PhoneOtpCommand(Guid.NewGuid(),staged.Version,"SEND");
            var replies=await Task.WhenAll(OtpCommand(client,send),OtpCommand(client,send));Assert.Equal(1,sender.Calls);Assert.Single(replies,x=>x.Replayed);
            var state=(await client.GetFromJsonAsync<ContactVerificationDto>(Contacts))!;
            var verify=new PhoneOtpCommand(Guid.NewGuid(),state.Version,"VERIFY",state.Phone.ChallengeId,sender.Messages.Last().Code);
            var responses=await Task.WhenAll(client.PostAsJsonAsync(OtpPath,verify),client.PostAsJsonAsync(OtpPath,verify with{OperationId=Guid.NewGuid()}));
            Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.OK);Assert.Single(responses,x=>x.StatusCode==HttpStatusCode.Conflict);
            foreach(var response in responses)response.Dispose();
        });
        await factory.WithDb(async db=>Assert.False(await db.SmsCreditTransactions.AnyAsync(x=>x.CreatedBy==owner)));
    }
    [PostgresFact] public async Task PhoneOtpExpiryLockCancelResendAndLegacyPhoneChangeInvalidateProof()
    {
        var owner=Guid.NewGuid().ToString();var sender=new OtpSender();var clock=new OtpClock();
        await OtpClient(owner,sender,clock,async client=>{
            var staged=await OtpStage(client,"05553334455");var sent=await OtpCommand(client,new(Guid.NewGuid(),staged.Version,"SEND"));
            Assert.Equal("TOO_SOON",(await OtpCommand(client,new(Guid.NewGuid(),sent.Contact.Version,"SEND"))).Outcome);
            var correct=sender.Messages.Last().Code;
            for(var n=0;n<5;n++)Assert.Equal(n==4?"LOCKED":"REJECTED",(await OtpCommand(client,new(Guid.NewGuid(),sent.Contact.Version,"VERIFY",sent.Contact.Phone.ChallengeId,correct=="000000"?"111111":"000000"))).Outcome);
            Assert.Equal("LOCKED",(await OtpCommand(client,new(Guid.NewGuid(),sent.Contact.Version,"VERIFY",sent.Contact.Phone.ChallengeId,correct))).Outcome);
            clock.Advance(61);sent=await OtpCommand(client,new(Guid.NewGuid(),sent.Contact.Version,"SEND"));var previous=sent.Contact.Phone.ChallengeId;
            clock.Advance(61);var next=await OtpCommand(client,new(Guid.NewGuid(),sent.Contact.Version,"SEND"));
            Assert.Equal("CANCELLED",(await OtpCommand(client,new(Guid.NewGuid(),next.Contact.Version,"VERIFY",previous,sender.Messages.Last().Code))).Outcome);
            clock.Advance(300);Assert.Equal("EXPIRED",(await OtpCommand(client,new(Guid.NewGuid(),next.Contact.Version,"VERIFY",next.Contact.Phone.ChallengeId,sender.Messages.Last().Code))).Outcome);
            sent=await OtpCommand(client,new(Guid.NewGuid(),next.Contact.Version,"SEND"));
            var profile=(await client.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!;
            var edit=new MobileProfileCommand(Guid.NewGuid(),profile.Version,profile.FirstName,profile.LastName,"05559990011","KEEP",null);
            Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/v1/mobile/profile/commands",edit)).StatusCode);
            var changed=(await client.GetFromJsonAsync<ContactVerificationDto>(Contacts))!;Assert.Equal("CANCELLED",changed.Phone.DeliveryStatus);Assert.Null(changed.Phone.VerifiedAt);
            Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(OtpPath,new PhoneOtpCommand(Guid.NewGuid(),sent.Contact.Version,"VERIFY",sent.Contact.Phone.ChallengeId,sender.Messages.Last().Code))).StatusCode);
        });
    }
    [PostgresFact] public async Task PhoneOtpAccountAndTargetHourlyLimitsAreSharedAcrossRequests()
    {
        var owner=Guid.NewGuid().ToString();var sender=new OtpSender();var clock=new OtpClock();
        await OtpClient(owner,sender,clock,async client=>{
            var staged=await OtpStage(client,"05554445566");
            for(var n=0;n<5;n++){Assert.Equal("ACCEPTED",(await OtpCommand(client,new(Guid.NewGuid(),staged.Version,"SEND"))).Outcome);clock.Advance(61);}
            Assert.Equal("RATE_LIMITED",(await OtpCommand(client,new(Guid.NewGuid(),staged.Version,"SEND"))).Outcome);Assert.Equal(5,sender.Calls);
        });
        await OtpClient(Guid.NewGuid().ToString(),sender,clock,async client=>{
            var staged=await OtpStage(client,"05554445566");Assert.Equal("RATE_LIMITED",(await OtpCommand(client,new(Guid.NewGuid(),staged.Version,"SEND"))).Outcome);Assert.Equal(5,sender.Calls);
        });
    }
    [PostgresFact] public async Task PhoneOtpDisabledAnonymousForeignOwnerAndForgedPayloadFailClosed()
    {
        using var anonymous=factory.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.PostAsJsonAsync(OtpPath,new PhoneOtpCommand(Guid.NewGuid(),0,"SEND"))).StatusCode);
        var owner=Guid.NewGuid().ToString();var sender=new OtpSender{Enabled=false};var clock=new OtpClock();
        await OtpClient(owner,sender,clock,async client=>{
            Assert.Equal("UNAVAILABLE",(await OtpCommand(client,new(Guid.NewGuid(),0,"SEND"))).Outcome);Assert.Equal(0,sender.Calls);
            using var forged=await client.PostAsJsonAsync(OtpPath,new{operationId=Guid.NewGuid(),expectedVersion=0,action="SEND",userId="foreign",verified=true});Assert.Equal(HttpStatusCode.BadRequest,forged.StatusCode);
            sender.Enabled=true;
            Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync(OtpPath,new PhoneOtpCommand(Guid.NewGuid(),0,"VERIFY",Guid.NewGuid(),"123456"))).StatusCode);
        });
    }
    [PostgresFact] public async Task PhoneOtpIpLimitIsSharedAcrossAccountsAndTargets()
    {
        var sender=new OtpSender();var clock=new OtpClock();
        for(var n=0;n<21;n++)await OtpClient(Guid.NewGuid().ToString(),sender,clock,async client=>{
            var staged=await OtpStage(client,"+90577"+n.ToString("D7"));
            Assert.Equal(n==20?"RATE_LIMITED":"ACCEPTED",(await OtpCommand(client,new(Guid.NewGuid(),staged.Version,"SEND"))).Outcome);
        },"192.0.2.199");
        Assert.Equal(20,sender.Calls);
    }
    [PostgresFact] public async Task PhoneOtpUnknownDeliveryIsNeverAutomaticallyRetriedAndExpiredPayloadIsErased()
    {
        var owner=Guid.NewGuid().ToString();var sender=new OtpSender{Outcome="UNKNOWN"};var clock=new OtpClock();
        await OtpClient(owner,sender,clock,async client=>{
            var staged=await OtpStage(client,"05556667788");var send=new PhoneOtpCommand(Guid.NewGuid(),staged.Version,"SEND");
            Assert.Equal("UNKNOWN",(await OtpCommand(client,send)).Outcome);Assert.True((await OtpCommand(client,send)).Replayed);Assert.Equal(1,sender.Calls);
            var state=(await client.GetFromJsonAsync<ContactVerificationDto>(Contacts))!;
            sender.Enabled=false;
            Assert.Equal("CANCELLED",(await OtpCommand(client,new(Guid.NewGuid(),state.Version,"CANCEL",state.Phone.ChallengeId))).Outcome);
        });
        await factory.WithDb(async db=>{
            var expired=new PhoneOtpChallenge(owner,new string('A',64),new string('B',64),DateTimeOffset.UtcNow);expired.SetSecret(new string('C',64),"encrypted");
            // Simulate a pre-existing reservation without logging or decrypting its payload.
            db.Add(expired);await db.SaveChangesAsync();
            var future=new OtpClock();future.Advance(360);
            await new PhoneOtpMaintenance(db,future).ExpireAsync(default);
            Assert.Null(expired.CodeMac);Assert.Null(expired.ProtectedPayload);Assert.Equal("EXPIRED",expired.Status);
        });
    }
    private sealed class OtpSender:IPhoneOtpSender
    {
        public bool Enabled=true;public string Outcome="ACCEPTED";public bool Available=>Enabled;public int Calls;public ConcurrentQueue<(string Phone,string Code)> Messages=new();
        public Task<PhoneOtpDelivery> SendAsync(string phone,string code,CancellationToken ct){Interlocked.Increment(ref Calls);Messages.Enqueue((phone,code));return Task.FromResult(new PhoneOtpDelivery(Outcome,Outcome=="ACCEPTED"?"12345":null));}
    }
    private sealed class OtpClock:TimeProvider
    {private readonly DateTimeOffset initial=DateTimeOffset.UtcNow;private long seconds;public override DateTimeOffset GetUtcNow()=>initial.AddSeconds(Interlocked.Read(ref seconds));public void Advance(int n)=>Interlocked.Add(ref seconds,n);}
    private sealed class OtpIp(string ip):IStartupFilter
    {public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)=>app=>{app.Use((context,go)=>{context.Connection.RemoteIpAddress=IPAddress.Parse(ip);return go();});next(app);};}
}
