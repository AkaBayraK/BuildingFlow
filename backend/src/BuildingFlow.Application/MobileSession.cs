namespace BuildingFlow.Application;

public sealed record MobileContext(Guid PropertyId, string PropertyName, Guid? UnitId, string? UnitNumber, string Role);
public sealed record MobileSession(string UserId, string? Username, IReadOnlyList<MobileContext> Contexts, bool HasMore);
public interface IMobileSessionStore
{
    Task<MobileSession> ReadAsync(string userId, string? username, IReadOnlyList<string> roles, CancellationToken token);
}
