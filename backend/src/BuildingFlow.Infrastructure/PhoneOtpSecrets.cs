using System.Security.Cryptography;
using System.Text;
using BuildingFlow.Application;
namespace BuildingFlow.Infrastructure;

public sealed class PhoneOtpSecrets : IPhoneOtpSecrets
{
    private readonly byte[]? key;
    private readonly SmsPayloadProtector? payload;
    public bool Available=>key is not null;
    public PhoneOtpSecrets(PhoneOtpOptions options)
    {
        if(!PersonCodeHasher.IsValidKey(options.HmacKey))return;
        var master=Convert.FromHexString(options.HmacKey.Trim());
        key=HKDF.DeriveKey(HashAlgorithmName.SHA256,master,32,info:Encoding.ASCII.GetBytes("buildingflow:phone-otp:proof:v1"));
        payload=SmsPayloadProtector.ForPhoneOtp(master);CryptographicOperations.ZeroMemory(master);
    }
    public string Generate()=>RandomNumberGenerator.GetInt32(0,1_000_000).ToString("D6",System.Globalization.CultureInfo.InvariantCulture);
    public string Hash(string purpose,string value)=>Convert.ToHexString(HMACSHA256.HashData(key??throw new InvalidOperationException("OTP key unavailable."),Encoding.UTF8.GetBytes(purpose+"\0"+value)));
    public bool Matches(string digest,string purpose,string value)
    {
        if(digest.Length!=64 || !digest.All(char.IsAsciiHexDigit))return false;
        return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(digest),Convert.FromHexString(Hash(purpose,value)));
    }
    public string Protect(string value)=>(payload??throw new InvalidOperationException("OTP key unavailable.")).Protect(value);
    public string Unprotect(string value)=>(payload??throw new InvalidOperationException("OTP key unavailable.")).Unprotect(value);
}
