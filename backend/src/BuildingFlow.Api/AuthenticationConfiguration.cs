using Microsoft.AspNetCore.Authentication.JwtBearer;

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
            options.TokenValidationParameters.ValidateAudience = true;
            options.TokenValidationParameters.ValidateLifetime = true;

            // Public issuer stays localhost; discovery and keys use the internal Docker network.
            if (configuration["Authentication:MetadataAddress"] is { Length: > 0 } metadata)
            {
                options.MetadataAddress = metadata;
            }
        });
        return services;
    }
}
