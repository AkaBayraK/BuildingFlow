using System.Globalization;
using System.Text.RegularExpressions;
using BuildingFlow.Domain;
namespace BuildingFlow.Application;

public sealed record CreateMeterRequest(Guid RequestId, string? Name, string? Kind, Guid? UnitId);
public sealed record ReadingRequest(Guid RequestId, DateOnly? ReadingDate, string? Value, string? Reason, long Version = 0);
public sealed record ReadingCommand(Guid RequestId, DateOnly Date, decimal Value, string Reason, long Version);
public sealed record MeterResponse(Guid Id, Guid PropertyId, Guid? UnitId, string Name, string Kind, string MeasurementUnit, bool IsEnabled, long Version, bool Replayed = false);
public sealed record MeterPage(IReadOnlyList<MeterResponse> Items, int Page, int PageSize, int TotalCount);
public sealed record ReadingRevisionResponse(Guid Id, string Value, int Sequence, string Reason, DateTimeOffset CreatedAt);
public sealed record ReadingResponse(Guid Id, Guid PropertyId, Guid MeterId, DateOnly ReadingDate, string Value, string? Consumption, long Version, IReadOnlyList<ReadingRevisionResponse> Revisions, bool Replayed = false);
public sealed record ReadingPage(IReadOnlyList<ReadingResponse> Items, int Page, int PageSize, int TotalCount);
public interface IMeterStore
{
    Task<MeterResponse?> Create(Guid property, CreateMeterRequest request, AccessScope access, CancellationToken token);
    Task<MeterPage?> List(Guid property, int page, int size, AccessScope access, CancellationToken token);
    Task<ReadingResponse?> Save(Guid property, Guid meter, Guid? reading, ReadingCommand request, AccessScope access, CancellationToken token);
    Task<ReadingPage?> Readings(Guid property, Guid meter, int page, int size, AccessScope access, CancellationToken token);
}
public sealed class MeterService(IMeterStore store, ICurrentUserService user)
{
    private AccessScope Access(bool write = false)
    {
        var roles = write ? new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER" } : new[] { "SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT" };
        if (!user.IsAuthenticated || string.IsNullOrWhiteSpace(user.UserId) || !roles.Any(user.IsInRole)) throw new UnauthorizedAccessException();
        return new(user.UserId, user.IsInRole("SUPER_ADMIN"));
    }
    public static string Number(decimal value) => value.ToString("0.000", CultureInfo.InvariantCulture);
    public static void Page(int page, int size) { if (page is < 1 or > 1000000 || size is < 1 or > 100) throw new RequestValidationException(new() { ["page"] = [ErrorCatalog.InvalidPage.Create()] }); }
    public Task<MeterResponse?> Create(Guid property, CreateMeterRequest request, CancellationToken token)
    {
        var access = Access(true);
        if (request.RequestId == Guid.Empty || request.UnitId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100 || !Enum.GetNames<MeterKind>().Contains(request.Kind))
            throw new RequestValidationException(new() { ["name"] = [ErrorCatalog.MeterInvalid.Create()] });
        return store.Create(property, request with { Name = request.Name.Trim() }, access, token);
    }
    public Task<MeterPage?> List(Guid property, int page, int size, CancellationToken token) { var access = Access(); Page(page, size); return store.List(property, page, size, access, token); }
    public Task<ReadingPage?> Readings(Guid property, Guid meter, int page, int size, CancellationToken token) { var access = Access(); Page(page, size); return store.Readings(property, meter, page, size, access, token); }
    public Task<ReadingResponse?> Save(Guid property, Guid meter, Guid? reading, ReadingRequest request, CancellationToken token)
    {
        var access = Access(true);
        if (request.RequestId == Guid.Empty || request.ReadingDate is null || request.ReadingDate.Value.Year < 2000 || request.Value is not { Length: <= 20 } || !Regex.IsMatch(request.Value, @"^[0-9]+(?:\.[0-9]{1,3})?$") || !decimal.TryParse(request.Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) || value > 999999999.999m || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500 || (reading.HasValue ? request.Version < 1 : request.Version != 0))
            throw new RequestValidationException(new() { ["value"] = [ErrorCatalog.ReadingInvalid.Create()] });
        return store.Save(property, meter, reading, new(request.RequestId, request.ReadingDate.Value, value, request.Reason.Trim(), request.Version), access, token);
    }
}
