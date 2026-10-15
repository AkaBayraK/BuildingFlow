using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using System.Text.Json;

namespace BuildingFlow.Api;

internal static class AuthenticationConfiguration
{
    public static IServiceCollection AddBuildingFlowAuthentication(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var authority = configuration["Authentication:Authority"]
            ?? throw new InvalidOperationException("Authentication:Authority is required.");
        var audience = configuration["Authentication:Audience"]
            ?? throw new InvalidOperationException("Authentication:Audience is required.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.MapInboundClaims = false;
            options.RequireHttpsMetadata = !environment.IsDevelopment();
            options.TokenValidationParameters.ValidateIssuer = true;
            if (environment.IsDevelopment())
            {
                var extra = configuration.GetSection("Authentication:DevelopmentIssuers").Get<string[]>() ?? [];
                if (extra.Any(v => !Uri.TryCreate(v, UriKind.Absolute, out var uri) || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.AbsolutePath != "/realms/buildingflow" || (uri.Scheme != "http" && uri.Scheme != "https")))
                    throw new InvalidOperationException("Invalid explicit development issuer.");
                options.TokenValidationParameters.ValidIssuers = extra.Append(authority).ToArray();
            }
            options.TokenValidationParameters.ValidateAudience = true;
            options.TokenValidationParameters.ValidateLifetime = true;
            options.TokenValidationParameters.RoleClaimType = "buildingflow_role";
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    if (context.Principal?.Identity is ClaimsIdentity identity)
                    {
                        // Only signed realm_access roles count; client roles and arbitrary role claims do not.
                        foreach (var existing in identity.FindAll("buildingflow_role").ToArray()) identity.RemoveClaim(existing);
                        var raw = identity.FindFirst("realm_access")?.Value;
                        if (raw is not null)
                        {
                            try
                            {
                                using var document = JsonDocument.Parse(raw);
                                if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
                                    foreach (var role in roles.EnumerateArray())
                                        if (role.ValueKind == JsonValueKind.String && role.GetString() is { Length: > 0 } name)
                                            identity.AddClaim(new Claim("buildingflow_role", name));
                            }
                            catch (JsonException) { context.Fail("Invalid realm role claim."); }
                        }
                    }
                    return Task.CompletedTask;
                }
            };

            // Public issuer stays localhost; discovery and keys use the internal Docker network.
            if (configuration["Authentication:MetadataAddress"] is { Length: > 0 } metadata)
            {
                options.MetadataAddress = metadata;
            }
        });
        return services;
    }
}
