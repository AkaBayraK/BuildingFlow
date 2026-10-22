using System.Net;
using System.Net.Http.Json;
using System.Text;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using BuildingFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace BuildingFlow.IntegrationTests;

public sealed partial class UserManagementTests
{
    private const string Contacts = "/api/v1/account/contact-verification";

    [PostgresFact] public async Task ContactStateIsSharedAndIdentityIsAuthoritativeWithoutMembership()
    {
        var id=Guid.NewGuid().ToString(); var kc=new FakeKeycloak(); kc.Add("contact.owner",[],id);
        await PhotoClient(kc,id,"RESIDENT",async web=>{
            var empty=(await web.GetFromJsonAsync<ContactVerificationDto>(Contacts))!;
            Assert.Equal(0,empty.Version);Assert.Equal("MISSING",empty.Phone.Status);Assert.Equal("UNVERIFIED",empty.Email.Status);
            kc.Users[id]=kc.Users[id] with {EmailVerified=true};
            var command=new StagePhoneRequest(Guid.NewGuid(),0,"0555 123 45 67");
            using var r=await web.PostAsJsonAsync(Contacts+"/phone",command);Assert.Equal(HttpStatusCode.OK,r.StatusCode);
            var staged=(await r.Content.ReadFromJsonAsync<StagePhoneResult>())!;
            Assert.Equal("VERIFIED",staged.Contact.Email.Status);Assert.Equal("PENDING",staged.Contact.Phone.Status);
            Assert.False(staged.Contact.Phone.CanSendCode);Assert.Null(staged.Contact.Phone.ChallengeId);Assert.Null(staged.Contact.Phone.VerifiedAt);
            Assert.Equal("NOT_REQUESTED",staged.Contact.Phone.DeliveryStatus);
            var mobile=(await web.GetFromJsonAsync<ContactVerificationDto>("/api/v1/mobile/contact-verification"))!;
            Assert.Equal(staged.Contact.Version,mobile.Version);Assert.Equal(staged.Contact.Phone,mobile.Phone);Assert.Equal(staged.Contact.Email.Status,mobile.Email.Status);
            Assert.Equal(0,(await web.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!.Version);
            var retry=await web.PostAsJsonAsync("/api/v1/mobile/contact-verification/phone",command with {Phone="+905551234567"});
            Assert.True((await retry.Content.ReadFromJsonAsync<StagePhoneResult>())!.Replayed);
            Assert.Equal(HttpStatusCode.Conflict,(await web.PostAsJsonAsync(Contacts+"/phone",command with {Phone="05559876543"})).StatusCode);
            var newer=new StagePhoneRequest(Guid.NewGuid(),staged.Contact.Version,"05551112233");
            Assert.Equal(HttpStatusCode.OK,(await web.PostAsJsonAsync(Contacts+"/phone",newer)).StatusCode);
            var old=(await (await web.PostAsJsonAsync(Contacts+"/phone",command)).Content.ReadFromJsonAsync<StagePhoneResult>())!;
            Assert.True(old.Replayed);Assert.Equal(newer.Phone,old.Contact.Phone.PendingValue);
            Assert.Equal(HttpStatusCode.Conflict,(await web.PostAsJsonAsync(Contacts+"/phone",new StagePhoneRequest(Guid.NewGuid(),0,"05551234567"))).StatusCode);
        });
        using var foreign=TestToken.Client(factory,Guid.NewGuid().ToString(),"SUPER_ADMIN");
        Assert.Equal("MISSING",(await foreign.GetFromJsonAsync<ContactVerificationDto>(Contacts))!.Phone.Status);
        await factory.WithDb(async db=>{var state=await db.Set<AccountPhoneVerification>().SingleAsync(x=>x.OwnerUserId==id);Assert.Equal(id,state.CreatedBy);Assert.Null(state.VerifiedAt);});
    }

    [PostgresFact] public async Task LegacyProfileWritesCannotTransferProofAndUnrelatedEditsDoNotInvalidateIt()
    {
        var id=Guid.NewGuid().ToString(); var at=DateTimeOffset.UtcNow;
        await factory.WithDb(async db=>{
            var profile=new MobileProfile(id);profile.Edit("Ada","KAYA","05551234567",null);
            var state=new AccountPhoneVerification(id);state.Stage(profile.Phone);state.Confirm("+905551234567",at);
            db.AddRange(profile,state);await db.SaveChangesAsync();
        });
        using var client=TestToken.Client(factory,id,"RESIDENT");
        var before=(await client.GetFromJsonAsync<ContactVerificationDto>(Contacts))!;
        Assert.Equal("VERIFIED",before.Phone.Status);Assert.Equal("UNKNOWN",before.Email.Status);Assert.False(before.Email.Available);
        var profile=(await client.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!;
        var edit=new MobileProfileCommand(Guid.NewGuid(),profile.Version,"Ada Updated","KAYA","+90 555 123 45 67","KEEP",null);
        var saved=(await (await client.PostAsJsonAsync("/api/v1/account/profile/commands",edit)).Content.ReadFromJsonAsync<MobileProfileResult>())!;
        var same=(await client.GetFromJsonAsync<ContactVerificationDto>(Contacts))!;
        Assert.Equal(before.Version,same.Version);Assert.Equal(before.Phone.VerifiedAt,same.Phone.VerifiedAt);
        var kc=new FakeKeycloak();kc.Add("proof.photo.owner",["RESIDENT"],id);
        await PhotoClient(kc,Guid.NewGuid().ToString(),"SUPER_ADMIN",async admin=>{
            var photoOnly=new UserPhotoCommand(Guid.NewGuid(),saved.Profile.Version,"REMOVE",null);
            Assert.Equal(HttpStatusCode.OK,(await admin.PostAsJsonAsync($"/api/v1/users/{id}/photo/commands",photoOnly)).StatusCode);
        });
        Assert.Equal(same.Version,(await client.GetFromJsonAsync<ContactVerificationDto>(Contacts))!.Version);
        profile=(await client.GetFromJsonAsync<MobileProfileDto>("/api/v1/mobile/profile"))!;
        edit=edit with {OperationId=Guid.NewGuid(),ExpectedVersion=profile.Version,Phone="05559876543"};
        saved=(await (await client.PostAsJsonAsync("/api/v1/mobile/profile/commands",edit)).Content.ReadFromJsonAsync<MobileProfileResult>())!;
        var changed=(await client.GetFromJsonAsync<ContactVerificationDto>(Contacts))!;
        Assert.Equal("PENDING",changed.Phone.Status);Assert.Equal("05551234567",changed.Phone.VerifiedValue);Assert.Equal(edit.Phone,changed.Phone.PendingValue);
        Assert.True(changed.Version>same.Version);
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/v1/mobile/profile/commands",edit)).StatusCode);
        Assert.Equal(changed.Version,(await client.GetFromJsonAsync<ContactVerificationDto>(Contacts))!.Version);
        edit=edit with {OperationId=Guid.NewGuid(),ExpectedVersion=saved.Profile.Version,Phone=null};
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/v1/account/profile/commands",edit)).StatusCode);
        var removed=(await client.GetFromJsonAsync<ContactVerificationDto>(Contacts))!;
        Assert.Equal("MISSING",removed.Phone.Status);Assert.Null(removed.Phone.VerifiedValue);
    }

    [PostgresFact] public async Task ContactEndpointsRejectAnonymousForgedFieldsAndConflictingConcurrentWrites()
    {
        using var anonymous=factory.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync(Contacts)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.PostAsJsonAsync(Contacts+"/phone",new StagePhoneRequest(Guid.NewGuid(),0,"05551234567"))).StatusCode);
        var id=Guid.NewGuid().ToString();using var owner=TestToken.Client(factory,id,"RESIDENT");
        foreach(var json in new[]{"{\"operationId\":\""+Guid.NewGuid()+"\",\"expectedVersion\":0,\"phone\":\"05551234567\",\"verified\":true}","null","{bad"})
            Assert.Equal(HttpStatusCode.BadRequest,(await owner.PostAsync(Contacts+"/phone",new StringContent(json,Encoding.UTF8,"application/json"))).StatusCode);
        foreach(var phone in new[]{"abc","12345",new string('5',33)})
            Assert.Equal(HttpStatusCode.BadRequest,(await owner.PostAsJsonAsync(Contacts+"/phone",new StagePhoneRequest(Guid.NewGuid(),0,phone))).StatusCode);
        var results=await Task.WhenAll(owner.PostAsJsonAsync(Contacts+"/phone",new StagePhoneRequest(Guid.NewGuid(),0,"05551234567")),owner.PostAsJsonAsync(Contacts+"/phone",new StagePhoneRequest(Guid.NewGuid(),0,"05559876543")));
        Assert.Single(results,r=>r.StatusCode==HttpStatusCode.OK);Assert.Single(results,r=>r.StatusCode==HttpStatusCode.Conflict);
        foreach(var r in results)r.Dispose();
        await factory.WithDb(async db=>{var s=await db.Set<AccountPhoneVerification>().SingleAsync(x=>x.OwnerUserId==id);s.SetEnabled(false);await db.SaveChangesAsync();});
        Assert.Equal(HttpStatusCode.Forbidden,(await owner.GetAsync(Contacts)).StatusCode);
    }
}
