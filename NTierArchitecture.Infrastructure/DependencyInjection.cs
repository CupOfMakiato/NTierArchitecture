using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NTierArchitecture.Application;
using NTierArchitecture.Application.IRepositories;
using NTierArchitecture.Application.IServices;
using NTierArchitecture.Application.Services;
using NTierArchitecture.Application.Settings.CloudinaryService;
using NTierArchitecture.Infrastructure.Database;
using NTierArchitecture.Infrastructure.Repositories;
using StackExchange.Redis;

namespace NTierArchitecture.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructuresService(this IServiceCollection services, IConfiguration configuration)
        {
            //UOW
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Service
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();

            services.AddScoped<IRedisService, RedisService>();
            services.AddScoped<ICloudinaryService, CloudinaryService>();

            services.AddMemoryCache();
            services.AddLogging();

            // Repo
            services.AddScoped<IUserRepository, UserRepository>();


            // Cloudinary
            services.Configure<CloudinarySetting>(configuration.GetSection("CloudinarySetting"));

            // Database Postgres
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
            );

            // Redis
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!)
            );

            // Hangfire DB
            //services.AddHangfire(options =>
            //{
            //    options.UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection"));
            //});


            return services;
        }
    }
}
