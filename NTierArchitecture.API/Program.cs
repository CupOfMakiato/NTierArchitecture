using NTierArchitecture.Application;
using NTierArchitecture.Application.IServices;
using NTierArchitecture.Application.Settings.Jwt;
using NTierArchitecture.Infrastructure.Hubs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NTierArchitecture.Infrastructure;
using NTierArchitecture.API;
using NTierArchitecture.API.Middlewares;
using NTierArchitecture.Application.Settings.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// parse the configuration in appsettings
{
    builder.Services
            .AddWebAPIService()
            .AddApplication()
            .AddInfrastructuresService(builder.Configuration);
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configure JWT authentication
var jwtSecretKey = builder.Configuration[$"{JwtSettings.SectionName}:SecretKey"]
    ?? throw new InvalidOperationException($"{JwtSettings.SectionName}:SecretKey is missing.");
var key = Encoding.UTF8.GetBytes(jwtSecretKey);
if (key.Length < 32)
{
    throw new InvalidOperationException($"{JwtSettings.SectionName}:SecretKey must be at least 32 bytes for HMAC SHA256.");
}

var accessTokenCookieName =
    builder.Configuration[$"{JwtSettings.SectionName}:AccessTokenCookieName"]
    ?? new JwtSettings().AccessTokenCookieName;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    //options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrWhiteSpace(context.Token)
                    && context.Request.Cookies.TryGetValue(accessTokenCookieName, out var accessToken))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var accessTokenId = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                var sessionId = context.Principal?.FindFirstValue("sid")
                    ?? context.Principal?.FindFirstValue(ClaimTypes.Sid);
                var tokenType = context.Principal?.FindFirstValue("token_type");

                if (string.IsNullOrWhiteSpace(accessTokenId)
                    || string.IsNullOrWhiteSpace(sessionId)
                    || !string.Equals(tokenType, "access", StringComparison.Ordinal))
                {
                    context.Fail("Invalid JWT session claims.");
                    return;
                }

                var tokenService = context.HttpContext.RequestServices.GetRequiredService<IJwtTokenService>();
                var isCachedToken = await tokenService.ValidateAccessTokenCacheAsync(accessTokenId, sessionId);
                if (!isCachedToken)
                {
                    context.Fail("JWT session is not active in Redis.");
                }
            }
        };
    });
//.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
//{
//    options.ClientId = builder.Configuration["GoogleAPI:ClientId"];
//    options.ClientSecret = builder.Configuration["GoogleAPI:SecretCode"];
//});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    });
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.WithOrigins()
                   .AllowAnyHeader()
                   .AllowCredentials()
                   .AllowAnyMethod();
        });
});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

//builder.Services.AddHangfireServer();


var app = builder.Build();

app.UseSwagger(options => options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("v1/swagger.json", "Clean Architecture v1");
    });
}

app.UseExceptionHandler("/Error");

app.UseCors("AllowAllOrigins");


// Middleware for performance tracking
app.UseMiddleware<PerformanceMiddleware>();

// use authen
app.UseAuthentication();
app.UseAuthorization();

// Use Global Exception Middleware 
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseMiddleware<RateLimitMiddleware>();

app.UseHttpsRedirection();

//app.UseHangfireDashboard("/hangfire");

//app.UseMiddleware<SystemConfigurationMiddleware>();

app.MapHealthChecks("/healthchecks");

app.MapControllers();

app.MapHub<NotificationHub>("hub/notificationHub");

app.Run();

