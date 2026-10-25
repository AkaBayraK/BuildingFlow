namespace BuildingFlow.Domain;

// Evidence belongs to an account and an exact phone value; never to a Property or Person.
public sealed class AccountPhoneVerification : BaseEntity
{
    private AccountPhoneVerification() { }
    public AccountPhoneVerification(string owner) => OwnerUserId = Required(owner, nameof(owner), 200);
    public string OwnerUserId { get; private set; } = null!;
    public string? VerifiedPhone { get; private set; }
    public string? VerifiedKey { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public string? PendingPhone { get; private set; }
    public string? PendingKey { get; private set; }

    public bool Stage(string? value)
    {
        var phone = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (phone is { Length: > 32 }) throw new ArgumentException("Phone exceeds profile bounds.", nameof(value));
        var key = PhoneContactKey.FromLegacy(phone);
        if (key is null)
        {
            if (PendingKey is null && VerifiedKey is null) return false;
            PendingPhone = PendingKey = VerifiedPhone = VerifiedKey = null;
            VerifiedAt = null;
            return true;
        }
        if (key == (PendingKey ?? VerifiedKey)) return false;
        // Returning to the already verified value cancels the pending change without re-verification.
        PendingPhone = key == VerifiedKey ? null : phone;
        PendingKey = key == VerifiedKey ? null : key;
        return true;
    }

    // Only a trusted server-side proof handler may call this.
    public void Confirm(string expectedKey, DateTimeOffset at)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedKey);
        if (PendingKey != expectedKey || !expectedKey.StartsWith('+') || at == default)
            throw new InvalidOperationException("Phone proof does not match the pending value.");
        VerifiedPhone = PendingPhone; VerifiedKey = PendingKey; VerifiedAt = at;
        PendingPhone = PendingKey = null;
    }
}

public static class PhoneContactKey
{
    // Keep every previously accepted profile value readable. Ambiguous legacy values cannot be proofs.
    public static string? FromLegacy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        var digits = string.Concat(value.Where(char.IsAsciiDigit));
        if (value.Count(c => c == '+') > 1 || (value.Contains('+') && !value.StartsWith('+')) ||
            value.Any(c => !char.IsAsciiDigit(c) && !"+ -()".Contains(c)) || digits.Length is < 7 or > 15)
            return "RAW:" + value;
        if (value.StartsWith('+')) return "+" + digits;
        if (digits.Length == 11 && digits.StartsWith('0')) return "+90" + digits[1..];
        if (digits.Length == 10) return "+90" + digits;
        return "LOCAL:" + digits;
    }

    public static bool ValidInput(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 32 &&
        value.Count(char.IsAsciiDigit) is >= 7 and <= 15 && value.All(c => char.IsAsciiDigit(c) || "+ -()".Contains(c));
}
