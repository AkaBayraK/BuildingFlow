using BuildingFlow.Application;
namespace BuildingFlow.Api;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public string? UserId => accessor.HttpContext?.User.FindFirst("sub")?.Value;
    public string? Username => accessor.HttpContext?.User.FindFirst("preferred_username")?.Value;
    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) == true;
    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
