using System.Security.Cryptography;
using System.Text;
using BuildingFlow.Application;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.Infrastructure;

public sealed class PersonCodeHasher : IPersonCodeHasher
{
    public const string KeySetting = "PersonCode:HmacKey";
    public const int MinimumKeyBytes = 32;
    // Development and test hosts without a configured key share this value; production startup rejects it (ReleaseSecurity).
    private static readonly byte[] DevelopmentKey = SHA256.HashData(Encoding.UTF8.GetBytes("buildingflow-development-person-code-key"));
    private readonly byte[] key;

    public PersonCodeHasher(IConfiguration configuration) => key = MasterKey(configuration);

    // The same server secret also derives other purpose-bound keys (for example SMS payload protection).
    public static byte[] MasterKey(IConfiguration configuration) => ReadKey(configuration[KeySetting]) ?? DevelopmentKey;

    public string Hash(string code)
    {
        if (!PersonCodePolicy.IsWellFormed(code)) throw new ArgumentException("Invalid person code.", nameof(code));
        return Convert.ToHexStringLower(HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(code)));
    }

    public static bool IsValidKey(string? value) => ReadKey(value) is not null;

    private static byte[]? ReadKey(string? value)
    {
        var text = value?.Trim();
        if (text is null || text.Length < MinimumKeyBytes * 2 || text.Length % 2 != 0 || !text.All(char.IsAsciiHexDigit)) return null;
        return Convert.FromHexString(text);
    }
}
