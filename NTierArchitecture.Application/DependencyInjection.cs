using Microsoft.Extensions.DependencyInjection;
using NTierArchitecture.Application.IServices;
using NTierArchitecture.Application.Mappers;
using NTierArchitecture.Application.Services;
using NTierArchitecture.Application.Utils;

namespace NTierArchitecture.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<ICurrentTime, CurrentTime>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();

            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtTokenService, TokenGenerators>();
            services.AddScoped<TokenGenerators>();

            services.AddAutoMapper(configuration =>
            {
                configuration.AddProfile<MapperConfigurationsProfile>();
            });
            return services;
        }
    }
}
