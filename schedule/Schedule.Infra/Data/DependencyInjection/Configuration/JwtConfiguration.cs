using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Schedule.Infra.Data.DependencyInjection.Configuration
{
    public static class JwtConfiguration
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt");

            string secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException(
                    "JWT SecretKey is not configured properly in appsettings.json. Please ensure that a valid key is provided."
                );

            string issuer = jwtSettings["Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT Issuer is not configured properly in appsettings.json. Please ensure that a valid issuer is provided."
                );

            string audience = jwtSettings["Audience"]
                ?? throw new InvalidOperationException(
                    "JWT Audience is not configured properly in appsettings.json. Please ensure that a valid audience is provided."
                );

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }

            ).AddJwtBearer(opt =>
            {
                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var auth = context.Request.Headers.Authorization.ToString();
                        if (!string.IsNullOrWhiteSpace(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            context.Token = auth["Bearer ".Length..].Trim();

                        return Task.CompletedTask;
                    }
                };
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],

                    NameClaimType = ClaimTypes.Email,
                    RoleClaimType = ClaimTypes.Role
                };
            });

            return services;

        }
    }
}
