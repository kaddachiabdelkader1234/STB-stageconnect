using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace SubjectMatching.Service.Authentication;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var secret = ResolveJwtSecret(configuration);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    IssuerSigningKey = signingKey,
                    NameClaimType = "sub",
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddAuthorization();
        services.AddSingleton<IClaimsTransformation, JwtRoleClaimsTransformation>();

        return services;
    }

    private static string ResolveJwtSecret(IConfiguration configuration)
    {
        var secret = configuration["JWT_SECRET"]
            ?? configuration["Jwt:Secret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("JWT secret is missing. Configure JWT_SECRET or Jwt:Secret.");
        }

        return secret;
    }
}

public sealed class JwtRoleClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        var roleClaims = identity.FindAll("role").ToList();
        foreach (var claim in roleClaims)
        {
            if (!identity.HasClaim(ClaimTypes.Role, claim.Value))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, claim.Value));
            }
        }

        return Task.FromResult(principal);
    }
}
