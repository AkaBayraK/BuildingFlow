using BuildingFlow.Application;
namespace BuildingFlow.Api;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public string? Issuer => accessor.HttpContext?.User.FindFirst("iss")?.Value;
    public string? UserId => accessor.HttpContext?.User.FindFirst("sub")?.Value;
    public string? Username => accessor.HttpContext?.User.FindFirst("preferred_username")?.Value;
    public bool IsInRole(string role)
    {
        var context=accessor.HttpContext;if(context==null)return false;
        if(!context.Items.TryGetValue(typeof(WorkspacePropertyPermissions),out var value) || value is not WorkspacePropertyPermissions scope)return context.User.IsInRole(role);
        // Preserve realm capability inheritance, but narrow it to this request's current Property duty.
        // These are server facts, not new JWT/Keycloak roles or client-selected permissions.
        var super=context.User.IsInRole("SUPER_ADMIN");var admin=context.User.IsInRole("SITE_ADMIN");var manager=context.User.IsInRole("MANAGER");
        return role switch
        {
            "SUPER_ADMIN"=>super,
            "SITE_ADMIN"=>(super||admin)&&scope.CanManageAssignments,
            "MANAGER"=>(super||admin||manager)&&scope.CanManagePeople,
            "ACCOUNTANT"=>super||admin||manager||context.User.IsInRole("ACCOUNTANT"),
            _=>context.User.IsInRole(role)
        };
    }
    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
