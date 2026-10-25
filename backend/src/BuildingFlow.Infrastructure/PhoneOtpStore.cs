using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using BuildingFlow.Application;
using BuildingFlow.Domain;
using Microsoft.EntityFrameworkCore;
namespace BuildingFlow.Infrastructure;

public sealed class PhoneOtpStore(BuildingFlowDbContext db,ICurrentUserService actor,IContactVerificationStore contacts,IPhoneOtpSender sender,IPhoneOtpSecrets secrets,TimeProvider clock,IKeycloakAdmin identity) : IPhoneOtpStore
{
    private static HierarchyConflictException Conflict()=>new(ErrorCatalog.MobileSyncConflict.Create());
    private static RequestValidationException Invalid()=>new(new(){["phone"]=[ErrorCatalog.UserInvalid.Create()]});
    private static bool Inactive(BaseEntity? value)=>value is {IsDeleted:true} or {IsEnabled:false};
    private string Owner()=>actor.IsAuthenticated&&!string.IsNullOrWhiteSpace(actor.UserId)?actor.UserId:throw new UnauthorizedAccessException();
    private Task Lock(string key,CancellationToken ct)=>db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key},0))",ct);
    private string CodeValue(string owner,Guid id,string target,string code)=>JsonSerializer.Serialize(new[]{owner,id.ToString("D"),target,code});
    private async Task<PhoneOtpResult> Reply(PhoneOtpCommand c,bool replay,string outcome,int retry,CancellationToken ct)=>new(c.OperationId,replay,outcome,await contacts.ReadAsync(ct),retry);
    private void Receipt(string owner,PhoneOtpCommand c,string hash,string outcome,Guid? challenge=null,int retry=0)=>db.Add(new MobileOperationReceipt(owner,c.OperationId,hash,JsonSerializer.Serialize(new{kind="PHONE_OTP_V1",outcome,challenge,retry})));

    public async Task<PhoneOtpResult> ExecuteAsync(PhoneOtpCommand c,string ip,CancellationToken ct)
    {
        var owner=Owner();
        if(c.OperationId==Guid.Empty||c.ExpectedVersion<0||c.Action is not ("SEND" or "VERIFY" or "CANCEL")||
            (c.Action=="VERIFY"?!PhoneOtpPolicy.IsCode(c.Code)||c.ChallengeId is null:c.Code is not null)||
            (c.Action=="CANCEL"&&c.ChallengeId is null)||(c.Action=="SEND"&&c.ChallengeId is not null))throw Invalid();
        if(c.Action=="SEND"&&(!sender.Available||!secrets.Available)||c.Action=="VERIFY"&&!secrets.Available)return await Reply(c,false,"UNAVAILABLE",0,ct);
        var normalizedCommand=JsonSerializer.Serialize(new{owner,c.OperationId,c.ExpectedVersion,c.Action,c.ChallengeId,c.Code});
        var fingerprint=c.Action=="VERIFY"?secrets.Hash("command",normalizedCommand):Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("phone-otp-command:v1:"+normalizedCommand)));
        Guid? dispatch=null;string outcome="UNKNOWN";int retry=0;
        await using(var tx=await db.Database.BeginTransactionAsync(ct))
        {
            await Lock("profile:"+owner,ct);await Lock(owner+":"+c.OperationId,ct);
            var profile=await db.Set<MobileProfile>().IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.OwnerUserId==owner,ct);
            var proof=await db.Set<AccountPhoneVerification>().IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.OwnerUserId==owner,ct);
            if(Inactive(profile)||Inactive(proof))throw new UnauthorizedAccessException();
            var receipt=await db.Set<MobileOperationReceipt>().IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.OwnerUserId==owner&&x.OperationId==c.OperationId,ct);
            if(receipt is not null)
            {
                if(Inactive(receipt)||receipt.Fingerprint!=fingerprint)throw Conflict();
                var result=JsonDocument.Parse(receipt.Result).RootElement;
                outcome=result.GetProperty("outcome").GetString()!;
                if(c.Action=="SEND"&&result.GetProperty("challenge").ValueKind==JsonValueKind.String)
                {
                    var previousId=result.GetProperty("challenge").GetGuid();
                    var previous=await db.Set<PhoneOtpChallenge>().IgnoreQueryFilters().SingleAsync(x=>x.Id==previousId&&x.OwnerUserId==owner,ct);
                    outcome=previous.IsUsable(clock.GetUtcNow())?previous.Status=="RESERVED"?"UNKNOWN":previous.Status:previous.Status;
                    await db.SaveChangesAsync(ct);
                }
                if(result.TryGetProperty("retry",out var wait))retry=Math.Max(0,(int)Math.Ceiling((receipt.CreatedAt.AddSeconds(wait.GetInt32())-clock.GetUtcNow()).TotalSeconds));
                await tx.CommitAsync(ct);return await Reply(c,true,outcome,retry,ct);
            }
            if((proof?.Version??0)!=c.ExpectedVersion)throw Conflict();
            var now=clock.GetUtcNow();
            if(c.Action=="SEND")
            {
                var phone=PhoneOtpPolicy.Normalize(proof is null?profile?.Phone:proof.PendingKey);
                if(proof?.VerifiedKey is not null&&proof.PendingKey is null)outcome="ALREADY_VERIFIED";
                else
                {
                    if(phone is null)throw Invalid();
                    var target=secrets.Hash("target",phone);var ipHash=secrets.Hash("ip",ip);
                    await Lock("otp-target:"+target,ct);await Lock("otp-ip:"+ipHash,ct);
                    var history=db.Set<PhoneOtpChallenge>().IgnoreQueryFilters().Where(x=>x.CreatedAt>=now.AddHours(-1));
                    var last=await history.Where(x=>x.OwnerUserId==owner).OrderByDescending(x=>x.CreatedAt).FirstOrDefaultAsync(ct);
                    if(last is not null&&now<last.ResendAfter){outcome="TOO_SOON";retry=(int)Math.Ceiling((last.ResendAfter-now).TotalSeconds);}
                    else if(await history.CountAsync(x=>x.OwnerUserId==owner,ct)>=PhoneOtpPolicy.AccountHourlyLimit ||
                        await history.CountAsync(x=>x.TargetHash==target,ct)>=PhoneOtpPolicy.TargetHourlyLimit ||
                        await history.CountAsync(x=>x.IpHash==ipHash,ct)>=PhoneOtpPolicy.IpHourlyLimit){outcome="RATE_LIMITED";retry=3600;}
                    else
                    {
                        if(profile is null)
                        {
                            var account=await identity.GetAsync(owner,ct)??throw Invalid();
                            var first=PersonNames.Given(account.FirstName);var lastName=PersonNames.Family(account.LastName);
                            if(string.IsNullOrWhiteSpace(first)||string.IsNullOrWhiteSpace(lastName)||first.Length>80||lastName.Length>80)throw Invalid();
                            profile=new(owner);profile.Edit(first,lastName,null,null);db.Add(profile);
                        }
                        await CancelPendingAsync(db,owner,ct);
                        if(proof is null){proof=new(owner);proof.Stage(phone);db.Add(proof);}
                        var challenge=new PhoneOtpChallenge(owner,target,ipHash,now);var code=secrets.Generate();
                        challenge.SetSecret(secrets.Hash("proof",CodeValue(owner,challenge.Id,target,code)),secrets.Protect(JsonSerializer.Serialize(new Payload(phone,code))));
                        db.Add(challenge);dispatch=challenge.Id;
                    }
                }
            }
            else
            {
                var challenge=await db.Set<PhoneOtpChallenge>().IgnoreQueryFilters().SingleOrDefaultAsync(x=>x.Id==c.ChallengeId&&x.OwnerUserId==owner,ct);
                if(challenge is null||Inactive(challenge))throw new UnauthorizedAccessException();
                if(c.Action=="CANCEL"){challenge.Cancel();outcome=challenge.Status;}
                else if(proof?.PendingKey is null||challenge.TargetHash!=secrets.Hash("target",proof.PendingKey)){challenge.Cancel();outcome=challenge.Status;}
                else
                {
                    outcome=challenge.Attempt(challenge.CodeMac is not null&&secrets.Matches(challenge.CodeMac,"proof",CodeValue(owner,challenge.Id,challenge.TargetHash,c.Code!)),now);
                    if(outcome=="VERIFIED")
                    {
                        proof.Confirm(proof.PendingKey,now);
                        if(profile is null)
                        {
                            // Preserve v1 profile requirements instead of inventing the account owner's name.
                            throw Invalid();
                        }
                        profile.Edit(profile.FirstName,profile.LastName,proof.VerifiedKey,profile.PhotoId);
                    }
                }
            }
            Receipt(owner,c,fingerprint,outcome,dispatch,retry);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        }
        if(dispatch is Guid id)outcome=await DispatchOnce(owner,id,ct);
        return await Reply(c,false,outcome,retry,ct);
    }

    // The reservation/UUID receipt is durable before the HTTP call. Replays NEVER dispatch,
    // even if a crash leaves RESERVED. There is no automatic queue retry for an OTP.
    private async Task<string> DispatchOnce(string owner,Guid id,CancellationToken ct)
    {
        db.ChangeTracker.Clear();await using var tx=await db.Database.BeginTransactionAsync(ct);await Lock("profile:"+owner,ct);
        var challenge=await db.Set<PhoneOtpChallenge>().SingleAsync(x=>x.Id==id&&x.OwnerUserId==owner,ct);
        var proof=await db.Set<AccountPhoneVerification>().IgnoreQueryFilters().SingleAsync(x=>x.OwnerUserId==owner,ct);
        if(!challenge.IsUsable(clock.GetUtcNow())||Inactive(proof)||proof.PendingKey is null||challenge.TargetHash!=secrets.Hash("target",proof.PendingKey))challenge.Cancel();
        else
        {
            PhoneOtpDelivery result;
            try
            {
                var payload=JsonSerializer.Deserialize<Payload>(secrets.Unprotect(challenge.ProtectedPayload!))??throw new CryptographicException();
                result=await sender.SendAsync(payload.Phone,payload.Code,ct);
            }
            catch(Exception e) when(e is CryptographicException or JsonException or HttpRequestException or OperationCanceledException){result=new("UNKNOWN");}
            challenge.Delivery(result.Status,result.ProviderMessageId);
            challenge.IsUsable(clock.GetUtcNow());
        }
        // Once the provider may have accepted a message, cancellation must not erase its recorded outcome.
        await db.SaveChangesAsync(CancellationToken.None);await tx.CommitAsync(CancellationToken.None);
        return challenge.Status=="RESERVED"?"UNKNOWN":challenge.Status;
    }
    internal static async Task CancelPendingAsync(BuildingFlowDbContext db,string owner,CancellationToken ct)
    {
        var rows=await db.Set<PhoneOtpChallenge>().IgnoreQueryFilters().Where(x=>x.OwnerUserId==owner&&(x.Status=="RESERVED"||x.Status=="ACCEPTED"||x.Status=="UNKNOWN")).ToListAsync(ct);
        foreach(var row in rows)row.Cancel();
    }
    private sealed record Payload(string Phone,string Code);
}

public sealed class PhoneOtpMaintenance(BuildingFlowDbContext db,TimeProvider clock)
{
    public async Task ExpireAsync(CancellationToken ct)
    {
        var rows=await db.Set<PhoneOtpChallenge>().IgnoreQueryFilters().Where(x=>x.ExpiresAt<=clock.GetUtcNow()&&(x.CodeMac!=null||x.ProtectedPayload!=null)).Take(100).ToListAsync(ct);
        foreach(var row in rows)row.IsUsable(clock.GetUtcNow());
        try{await db.SaveChangesAsync(ct);}catch(DbUpdateConcurrencyException){db.ChangeTracker.Clear();}
    }
}
