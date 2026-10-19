using System.Security.Cryptography;
using System.Text;
using BuildingFlow.Application;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.Infrastructure;

// AES-GCM with a key derived from the server secret; protected SMS payloads are cleared once a message is final.
public sealed class SmsPayloadProtector(IConfiguration configuration) : ISmsProtector
{
    private const string Prefix = "v1.";
    private readonly byte[] key = HKDF.DeriveKey(HashAlgorithmName.SHA256, PersonCodeHasher.MasterKey(configuration), 32, info: Encoding.ASCII.GetBytes("buildingflow:sms-payload:v1"));

    public string Protect(string plain)
    {
        var data = Encoding.UTF8.GetBytes(plain);
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var cipher = new byte[data.Length]; var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, data, cipher, tag);
        return Prefix + Convert.ToBase64String([.. nonce, .. tag, .. cipher]);
    }

    public string Unprotect(string protectedValue)
    {
        if (!protectedValue.StartsWith(Prefix, StringComparison.Ordinal)) throw new CryptographicException("Unknown protected payload format.");
        var bytes = Convert.FromBase64String(protectedValue[Prefix.Length..]);
        var nonceSize = AesGcm.NonceByteSizes.MaxSize; var tagSize = AesGcm.TagByteSizes.MaxSize;
        if (bytes.Length < nonceSize + tagSize) throw new CryptographicException("Protected payload is truncated.");
        var plain = new byte[bytes.Length - nonceSize - tagSize];
        using var aes = new AesGcm(key, tagSize);
        aes.Decrypt(bytes.AsSpan(0, nonceSize), bytes.AsSpan(nonceSize + tagSize), bytes.AsSpan(nonceSize, tagSize), plain);
        return Encoding.UTF8.GetString(plain);
    }
}
