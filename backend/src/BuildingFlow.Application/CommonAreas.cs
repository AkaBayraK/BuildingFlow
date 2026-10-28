using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record CommonAreaRequest(Guid RequestId, string? Name, string? Type, Guid? BuildingId,
    string? Visibility, string? Usage, int Capacity, string? Description = null, string? Rules = null,
    string? OpensAt = null, string? ClosesAt = null, string? FeeAmount = "0.00", string? Currency = "TRY", bool IsEnabled = true, long Version = 0);
public sealed record CommonAreaQuery(int Page = 1, int PageSize = 20, string? Search = null, string? Type = null, Guid? BuildingId = null, bool? IsEnabled = null);
public sealed record CommonAreaCommand(Guid RequestId, string Fingerprint, string Name, CommonAreaType Type,
    Guid? BuildingId, CommonAreaVisibility Visibility, CommonAreaUsage Usage, int Capacity, string? Description,
    string? Rules, TimeOnly? OpensAt, TimeOnly? ClosesAt, decimal FeeAmount, bool IsEnabled, long Version);
public sealed record CommonAreaResponse(Guid Id, Guid PropertyId, Guid RequestId, string Name, string Type, Guid? BuildingId,
    string Visibility, string Usage, int Capacity, string? Description, string? Rules, string? OpensAt, string? ClosesAt,
    string FeeAmount, string Currency, bool IsEnabled, long Version, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt, bool Replayed = false);
public interface ICommonAreaStore
{
    Task<PagedRows<CommonAreaResponse>?> List(Guid property, CommonAreaQuery query, CancellationToken token);
    Task<CommonAreaResponse?> Get(Guid property, Guid id, CancellationToken token);
    Task<CommonAreaResponse?> Save(Guid property, Guid? id, CommonAreaCommand command, CancellationToken token);
    Task<bool> Delete(Guid property, Guid id, long version, CancellationToken token);
    Task<PagedRows<CommonAreaResponse>?> ForUnit(ActiveContextQuery context, CommonAreaQuery query, Guid? id, CancellationToken token);
}
public sealed class CommonAreaService(ICommonAreaStore store, ICurrentUserService user)
{
    private void Manager()
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || string.IsNullOrWhiteSpace(user.Issuer) ||
            !new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER" }.Any(user.IsInRole)) throw new UnauthorizedAccessException();
    }
    public static void Invalid(string field) => throw new RequestValidationException(new() { [field] = [ErrorCatalog.CommonAreaInvalid.Create()] });
    private static T EnumValue<T>(string? value, string field) where T : struct, Enum
    { if (value is null || !Enum.GetNames<T>().Contains(value)) Invalid(field); return Enum.Parse<T>(value!); }
    public static void Validate(CommonAreaQuery q)
    {
        if (q.Page is < 1 or > 1000000 || q.PageSize is < 1 or > 100) Invalid("page");
        if (q.Search?.Length > 200) Invalid("search");
        if (q.Type is not null) EnumValue<CommonAreaType>(q.Type, "type");
        if (q.BuildingId == Guid.Empty) Invalid("buildingId");
    }
    public Task<PagedRows<CommonAreaResponse>?> List(Guid property, CommonAreaQuery q, CancellationToken token)
    { Manager(); Validate(q); return store.List(property, q, token); }
    public Task<CommonAreaResponse?> Get(Guid property, Guid id, CancellationToken token)
    { Manager(); return store.Get(property, id, token); }
    public Task<bool> Delete(Guid property, Guid id, long version, CancellationToken token)
    { Manager(); if (version < 1) Invalid("version"); return store.Delete(property, id, version, token); }
    public Task<PagedRows<CommonAreaResponse>?> ForUnit(ActiveContextQuery context, CommonAreaQuery q, Guid? id, CancellationToken token)
    {
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || string.IsNullOrWhiteSpace(user.Issuer)) throw new UnauthorizedAccessException();
        Validate(q);
        if (context.Capacity is not ("OWNER" or "TENANT" or "RESIDENT") || context.PropertyId == Guid.Empty || context.UnitId is null || context.UnitId == Guid.Empty || context.RelationId == Guid.Empty) Invalid("context");
        return store.ForUnit(context, q, id, token);
    }
    public Task<CommonAreaResponse?> Save(Guid property, Guid? id, CommonAreaRequest r, CancellationToken token)
    {
        Manager();
        if (r.RequestId == Guid.Empty) Invalid("requestId");
        if (id.HasValue ? r.Version < 1 : r.Version != 0) Invalid("version");
        if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Trim().Length > 150) Invalid("name");
        if (r.Description?.Trim().Length > 2000) Invalid("description");
        if (r.Rules?.Trim().Length > 8000) Invalid("rules");
        var type = EnumValue<CommonAreaType>(r.Type, "type");
        var visibility = EnumValue<CommonAreaVisibility>(r.Visibility, "visibility");
        var usage = EnumValue<CommonAreaUsage>(r.Usage, "usage");
        if (r.BuildingId == Guid.Empty || visibility == CommonAreaVisibility.BUILDING && r.BuildingId is null) Invalid("buildingId");
        if (r.Capacity is < 1 or > 10000) Invalid("capacity");
        TimeOnly? Time(string? value, string field) { if (value is null) return null; if (!TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)) Invalid(field); return time; }
        var opens = Time(r.OpensAt, "opensAt"); var closes = Time(r.ClosesAt, "closesAt");
        if (opens.HasValue != closes.HasValue || opens.HasValue && opens >= closes) Invalid("closesAt");
        if (r.Currency != "TRY" || r.FeeAmount is null || !decimal.TryParse(r.FeeAmount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount) || amount is < 0 or > 99999999.99m || decimal.Round(amount, 2) != amount) Invalid("feeAmount");
        // JSON has deterministic property order and handles delimiters in user text without ambiguity.
        var canonical = new { actor = user.UserId, issuer = user.Issuer, property, name = r.Name!.Trim(), type, r.BuildingId, visibility, usage, r.Capacity, description = Clean(r.Description), rules = Clean(r.Rules), opens, closes, amount = decimal.Parse(r.FeeAmount!, CultureInfo.InvariantCulture).ToString("0.00", CultureInfo.InvariantCulture), r.IsEnabled };
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical))));
        return store.Save(property, id, new(r.RequestId, fingerprint, canonical.name, type, r.BuildingId, visibility, usage, r.Capacity, canonical.description, canonical.rules, opens, closes, decimal.Parse(r.FeeAmount!, CultureInfo.InvariantCulture), r.IsEnabled, r.Version), token);
    }
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
