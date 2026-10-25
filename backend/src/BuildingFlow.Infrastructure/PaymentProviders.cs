using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Application;
using Microsoft.Extensions.Configuration;
namespace BuildingFlow.Infrastructure;

// Merchant secrets live in the secret store (environment / vault), addressed by reference; the database keeps only the name.
public sealed class ConfigurationPaymentSecretResolver(IConfiguration configuration) : IPaymentSecretResolver
{
    public bool Has(string reference) => !string.IsNullOrWhiteSpace(configuration["Payments:Secrets:" + reference]);
}

public sealed class PaymentProviderRegistry(IEnumerable<IPaymentProviderAdapter> adapters)
{
    // Catalog entries without an adapter are listed for planning but cannot be enabled (no contract, sandbox account or adapter yet).
    public static readonly IReadOnlyDictionary<string, string> Planned = new Dictionary<string, string>
    {
        ["IYZICO"] = "iyzico", ["PAYTR"] = "PayTR", ["BANK_POS"] = "Banka sanal POS"
    };
    public IPaymentProviderAdapter? Find(string code) => adapters.FirstOrDefault(value => value.Code == code);
    public IReadOnlyList<IPaymentProviderAdapter> All => adapters.ToArray();
}

public sealed class SandboxPaymentOptions
{
    public bool Enabled { get; init; }
    public string? WebhookSecret { get; init; }
    public string PublicBaseUrl { get; init; } = "http://localhost:5080";
    public decimal FeeRate { get; init; } = 0.0199m;
    public static SandboxPaymentOptions From(IConfiguration configuration) => new()
    {
        Enabled = configuration.GetValue("Payments:Sandbox:Enabled", false),
        WebhookSecret = configuration["Payments:Sandbox:WebhookSecret"],
        PublicBaseUrl = configuration["Payments:Sandbox:PublicBaseUrl"] ?? "http://localhost:5080",
    };
}

// Local stand-in for a hosted checkout provider. It keeps state in memory (single instance, test only) and
// signs its notifications exactly like a real provider would, so the webhook path is exercised end to end.
public sealed class SandboxPaymentProvider(SandboxPaymentOptions options, IPaymentSecretResolver secrets, TimeProvider clock) : IPaymentProviderAdapter
{
    public const string ProviderCode = "SANDBOX";
    public const string SignatureHeader = "x-bf-sandbox-signature";
    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(5);
    private sealed class Session
    {
        public required string Reference { get; init; }
        public required string MerchantId { get; init; }
        public required decimal Amount { get; init; }
        public required string Description { get; init; }
        public required DateTimeOffset ExpiresAt { get; init; }
        public ProviderPaymentState State { get; set; } = ProviderPaymentState.PENDING;
        public string? TransactionId { get; set; }
        public decimal Fee { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public DateTimeOffset? ReversedAt { get; set; }
        public string? FailureCode { get; set; }
    }
    private readonly ConcurrentDictionary<string, Session> _sessions = new(StringComparer.Ordinal);
    public string Code => ProviderCode;
    public string DisplayName => "BuildingFlow Sandbox (test)";
    public bool SupportsRefund => true;
    public bool Enabled => options.Enabled && options.WebhookSecret is { Length: >= 32 };
    private static string Token(string prefix) => prefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    private void EnsureEnabled() { if (!Enabled) throw new PaymentProviderException("SANDBOX_DISABLED"); }

    public Task<ProviderCheckout> CreateCheckoutAsync(ProviderCheckoutRequest request, CancellationToken token)
    {
        EnsureEnabled();
        // A reserved merchant id lets tests exercise a provider outage without a network.
        if (request.Merchant.MerchantId == "SANDBOX-UNAVAILABLE") throw new PaymentProviderException("UNAVAILABLE");
        var reference = Token("sbx_");
        _sessions[reference] = new() { Reference = reference, MerchantId = request.Merchant.MerchantId, Amount = request.Amount, Description = request.Description, ExpiresAt = request.ExpiresAt };
        return Task.FromResult(new ProviderCheckout(reference, options.PublicBaseUrl.TrimEnd('/') + "/api/v1/payment-sandbox/checkout/" + reference));
    }
    public Task<ProviderPaymentStatus> GetStatusAsync(ProviderMerchant merchant, string reference, CancellationToken token)
    {
        EnsureEnabled();
        if (!_sessions.TryGetValue(reference, out var session) || session.MerchantId != merchant.MerchantId)
            return Task.FromResult(new ProviderPaymentStatus(ProviderPaymentState.UNKNOWN, null, null, null, null, null));
        lock (session)
        {
            if (session.State == ProviderPaymentState.PENDING && session.ExpiresAt <= clock.GetUtcNow()) session.State = ProviderPaymentState.EXPIRED;
            return Task.FromResult(new ProviderPaymentStatus(session.State, session.TransactionId, session.Amount, session.Fee, session.CompletedAt, session.FailureCode));
        }
    }
    public Task<string> RefundAsync(ProviderMerchant merchant, string reference, decimal amount, CancellationToken token)
    {
        EnsureEnabled();
        if (!_sessions.TryGetValue(reference, out var session) || session.MerchantId != merchant.MerchantId) throw new PaymentProviderException("UNKNOWN_PAYMENT");
        lock (session)
        {
            if (session.State != ProviderPaymentState.SUCCEEDED || amount != session.Amount) throw new PaymentProviderException("NOT_REFUNDABLE");
            session.State = ProviderPaymentState.REFUNDED; session.ReversedAt = clock.GetUtcNow();
        }
        return Task.FromResult(Token("sbx_rf_"));
    }
    public Task<bool> TestConnectionAsync(ProviderMerchant merchant, CancellationToken token) => Task.FromResult(Enabled && secrets.Has(merchant.SecretReference));
    public Task<IReadOnlyList<ProviderSettlementLine>> SettlementAsync(ProviderMerchant merchant, DateOnly date, CancellationToken token)
    {
        EnsureEnabled();
        static DateOnly Day(DateTimeOffset value) => BusinessDate.Of(value);
        List<ProviderSettlementLine> lines = [];
        foreach (var session in _sessions.Values.Where(value => value.MerchantId == merchant.MerchantId))
            lock (session)
            {
                if (session.CompletedAt is { } done && session.TransactionId is not null && Day(done) == date)
                    lines.Add(new(session.Reference, "PAYMENT", session.Amount, session.Fee, date));
                if (session.ReversedAt is { } reversed && Day(reversed) == date)
                    lines.Add(new(session.Reference, session.State == ProviderPaymentState.CHARGED_BACK ? "CHARGEBACK" : "REFUND", -session.Amount, 0m, date));
            }
        return Task.FromResult<IReadOnlyList<ProviderSettlementLine>>(lines);
    }

    // Simulates the payer finishing the hosted page; returns the signed notification the provider would send.
    public (Dictionary<string, string> Headers, byte[] Body)? Complete(string reference, bool success)
    {
        if (!Enabled || !_sessions.TryGetValue(reference, out var session)) return null;
        lock (session)
        {
            var now = clock.GetUtcNow();
            if (session.State != ProviderPaymentState.PENDING || session.ExpiresAt <= now) return null;
            if (success) { session.State = ProviderPaymentState.SUCCEEDED; session.TransactionId = Token("sbx_txn_"); session.Fee = decimal.Round(session.Amount * options.FeeRate, 2, MidpointRounding.AwayFromZero); session.CompletedAt = now; }
            else { session.State = ProviderPaymentState.FAILED; session.FailureCode = "CARD_DECLINED"; }
            return Sign(success ? "payment.succeeded" : "payment.failed", reference, now);
        }
    }
    public (Dictionary<string, string> Headers, byte[] Body)? ChargeBack(string reference)
    {
        if (!Enabled || !_sessions.TryGetValue(reference, out var session)) return null;
        lock (session)
        {
            if (session.State != ProviderPaymentState.SUCCEEDED) return null;
            session.State = ProviderPaymentState.CHARGED_BACK; session.ReversedAt = clock.GetUtcNow();
            return Sign("payment.charged_back", reference, session.ReversedAt.Value);
        }
    }
    public (string Description, decimal Amount, bool Open)? Describe(string reference) =>
        Enabled && _sessions.TryGetValue(reference, out var session) ? (session.Description, session.Amount, session.State == ProviderPaymentState.PENDING && session.ExpiresAt > clock.GetUtcNow()) : null;

    public (Dictionary<string, string> Headers, byte[] Body) Sign(string type, string reference, DateTimeOffset at, string? eventId = null, DateTimeOffset? signedAt = null)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(new { id = eventId ?? Token("evt_"), type, reference, occurredAt = at });
        var timestamp = (signedAt ?? clock.GetUtcNow()).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        return (new() { [SignatureHeader] = $"t={timestamp},v1={Signature(timestamp, body)}" }, body);
    }
    private string Signature(string timestamp, ReadOnlySpan<byte> body)
    {
        var payload = Encoding.UTF8.GetBytes(timestamp + ".").Concat(body.ToArray()).ToArray();
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.WebhookSecret!), payload)).ToLowerInvariant();
    }
    public ProviderWebhookEvent? VerifyWebhook(IReadOnlyDictionary<string, string> headers, ReadOnlySpan<byte> body, DateTimeOffset now)
    {
        if (!Enabled || body.Length is 0 or > 4096 || !headers.TryGetValue(SignatureHeader, out var header)) return null;
        var parts = header.Split(',').Select(value => value.Split('=', 2)).Where(value => value.Length == 2).ToDictionary(value => value[0], value => value[1]);
        if (!parts.TryGetValue("t", out var timestamp) || !parts.TryGetValue("v1", out var signature) ||
            !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)) return null;
        if ((now - DateTimeOffset.FromUnixTimeSeconds(seconds)).Duration() > Tolerance) return null;
        var expected = Encoding.ASCII.GetBytes(Signature(timestamp, body));
        if (!CryptographicOperations.FixedTimeEquals(expected, Encoding.ASCII.GetBytes(signature))) return null;
        try
        {
            using var document = JsonDocument.Parse(body.ToArray());
            var root = document.RootElement;
            var id = root.GetProperty("id").GetString(); var type = root.GetProperty("type").GetString(); var reference = root.GetProperty("reference").GetString();
            if (string.IsNullOrWhiteSpace(id) || id.Length > 100 || string.IsNullOrWhiteSpace(type) || type.Length > 40 ||
                string.IsNullOrWhiteSpace(reference) || reference.Length > 100) return null;
            return new(id, type, reference, root.GetProperty("occurredAt").GetDateTimeOffset());
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or FormatException) { return null; }
    }
}
