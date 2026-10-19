using System.Security.Cryptography;
namespace BuildingFlow.Application;

public sealed record IssuedPersonCode(string Code, string Hash, string Hint);
public sealed record RegeneratePersonCodeRequest(long Version, bool SendSms = false);

public interface IPersonCodeHasher
{
    string Hash(string code);
}

public static class PersonCodePolicy
{
    public const int Length = 6;
    public const int HintLength = 2;

    public static bool IsWellFormed(string? code) =>
        code is { Length: Length } && code.All(char.IsAsciiDigit) && code[0] != '0';

    // Rejects codes a person could guess or that read as a sequence: monotonic runs, repeated digits and short cycles.
    public static bool IsAcceptable(string code)
    {
        if (!IsWellFormed(code)) return false;
        if (code.GroupBy(c => c).Any(group => group.Count() >= 4)) return false;
        for (var i = 0; i + 2 < code.Length; i++)
            if (code[i] == code[i + 1] && code[i] == code[i + 2]) return false;
        if (code[..2] == code[2..4] && code[..2] == code[4..]) return false;
        if (code[..3] == code[3..]) return false;
        return LongestStepRun(code, 1) < 4 && LongestStepRun(code, -1) < 4;
    }

    public static string Generate(Func<int, int, int> next)
    {
        for (var attempt = 0; attempt < 1000; attempt++)
        {
            var digits = new char[Length];
            digits[0] = (char)('0' + next(1, 10));
            for (var i = 1; i < Length; i++) digits[i] = (char)('0' + next(0, 10));
            var code = new string(digits);
            if (IsAcceptable(code)) return code;
        }
        throw new InvalidOperationException("Person code generator could not produce an acceptable code.");
    }

    public static string Hint(string code) => code[^HintLength..];

    private static int LongestStepRun(string code, int step)
    {
        int longest = 1, current = 1;
        for (var i = 1; i < code.Length; i++)
        {
            current = code[i] - code[i - 1] == step ? current + 1 : 1;
            longest = Math.Max(longest, current);
        }
        return longest;
    }
}

public sealed class PersonCodeIssuer(IPersonCodeHasher hasher)
{
    public IssuedPersonCode Issue()
    {
        var code = PersonCodePolicy.Generate(RandomNumberGenerator.GetInt32);
        return new(code, hasher.Hash(code), PersonCodePolicy.Hint(code));
    }
}
