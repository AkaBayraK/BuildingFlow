namespace BuildingFlow.Domain;

public static class PhoneOtpRules
{
    public const int Digits=6,MaxAttempts=5,AccountHourlyLimit=5,TargetHourlyLimit=5,IpHourlyLimit=20;
    public static readonly TimeSpan Lifetime=TimeSpan.FromMinutes(5),ResendDelay=TimeSpan.FromSeconds(60);
}

public sealed class PhoneOtpChallenge : BaseEntity
{
    private PhoneOtpChallenge() { }
    public PhoneOtpChallenge(string owner,string target,string ip,DateTimeOffset now)
    {OwnerUserId=Required(owner,nameof(owner),200);TargetHash=Required(target,nameof(target),64);IpHash=Required(ip,nameof(ip),64);ExpiresAt=now.Add(PhoneOtpRules.Lifetime);ResendAfter=now.Add(PhoneOtpRules.ResendDelay);}
    public string OwnerUserId {get;private set;}=null!;
    public string TargetHash {get;private set;}=null!;
    public string IpHash {get;private set;}=null!;
    public string? CodeMac {get;private set;}
    public string? ProtectedPayload {get;private set;}
    public string Status {get;private set;}="RESERVED";
    public string? ProviderMessageId {get;private set;}
    public DateTimeOffset ExpiresAt {get;private set;}
    public DateTimeOffset ResendAfter {get;private set;}
    public int Attempts {get;private set;}
    public bool Terminal=>Status is "VERIFIED" or "CANCELLED" or "EXPIRED" or "LOCKED" or "FAILED";
    public void SetSecret(string mac,string payload){CodeMac=Required(mac,nameof(mac),64);ProtectedPayload=Required(payload,nameof(payload),2048);}
    public void Delivery(string status,string? messageId)
    {
        if(Terminal)return;
        if(status is not ("ACCEPTED" or "FAILED" or "UNKNOWN"))throw new ArgumentException("Invalid delivery state.");
        Status=status;ProviderMessageId=messageId;ProtectedPayload=null;
        if(status=="FAILED")CodeMac=null;
    }
    public string Attempt(bool matches,DateTimeOffset now)
    {
        if(!IsUsable(now))return Status;
        if(matches){Finish("VERIFIED");return Status;}
        Attempts++;if(Attempts>=PhoneOtpRules.MaxAttempts)Finish("LOCKED");
        return Status=="LOCKED" ? Status : "REJECTED";
    }
    public bool IsUsable(DateTimeOffset now)
    {if(now>=ExpiresAt && !Terminal)Finish("EXPIRED");return !Terminal;}
    public void Cancel(){if(!Terminal)Finish("CANCELLED");}
    private void Finish(string state){Status=state;CodeMac=null;ProtectedPayload=null;}
}
