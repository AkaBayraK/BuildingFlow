using System.Text.Json.Serialization;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public static class PhoneOtpPolicy
{
    public const int Digits=PhoneOtpRules.Digits, MaxAttempts=PhoneOtpRules.MaxAttempts, AccountHourlyLimit=PhoneOtpRules.AccountHourlyLimit, TargetHourlyLimit=PhoneOtpRules.TargetHourlyLimit, IpHourlyLimit=PhoneOtpRules.IpHourlyLimit;
    public static readonly TimeSpan Lifetime=PhoneOtpRules.Lifetime, ResendDelay=PhoneOtpRules.ResendDelay;
    public static string? Normalize(string? input)
    {
        var key=PhoneContactKey.FromLegacy(input);
        return key is {Length:13} && key.StartsWith("+905",StringComparison.Ordinal) && key.AsSpan(1).IndexOfAnyExceptInRange('0','9')<0 ? key : null;
    }
    public static bool IsCode(string? code)=>code is {Length:Digits} && code.All(char.IsAsciiDigit);
}

public sealed record PhoneOtpDelivery(string Status,string? ProviderMessageId=null);
public interface IPhoneOtpSender
{
    bool Available {get;}
    Task<PhoneOtpDelivery> SendAsync(string phone,string code,CancellationToken token);
}
public interface IPhoneOtpSecrets
{
    bool Available {get;}
    string Generate();
    string Hash(string purpose,string value);
    string Protect(string value);
    string Unprotect(string value);
    bool Matches(string digest,string purpose,string value);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PhoneOtpCommand(Guid OperationId,long ExpectedVersion,string Action,Guid? ChallengeId=null,string? Code=null);
public sealed record PhoneOtpResult(Guid OperationId,bool Replayed,string Outcome,ContactVerificationDto Contact,int RetryAfterSeconds=0);
public interface IPhoneOtpStore
{
    Task<PhoneOtpResult> ExecuteAsync(PhoneOtpCommand command,string ip,CancellationToken token);
}
