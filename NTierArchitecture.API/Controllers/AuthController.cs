using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using NTierArchitecture.API.Extensions;
using NTierArchitecture.Application.Abstractions.Shared;
using NTierArchitecture.Application.DTOs.Auth;
using NTierArchitecture.Application.IServices;
using NTierArchitecture.Application.Settings.Jwt;
using System.Security.Claims;

namespace NTierArchitecture.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            if (result.Error != 0 || result.Data == null)
            {
                return BadRequest(result);
            }

            //Response.AppendJwtTokenCookies(result.Data.Tokens, Request.IsHttps);
            return Ok(new
            {
                Error = 0,
                Message = "Register Successfully!"
            });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (result.Error != 0 || result.Data == null)
            {
                return Unauthorized(result);
            }

            // uncomment to debug token
            // Response.AppendJwtAuthorizationHeader(result.Data.Tokens);
            Response.AppendJwtTokenCookies(result.Data.Tokens, Request.IsHttps);
            return Ok(new
            {
                Error = 0,
                Message = "Login Successfully!"
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var sessionId = User.FindFirstValue("sid") ?? User.FindFirstValue(ClaimTypes.Sid);
            var result = await _authService.LogoutAsync(sessionId ?? string.Empty);
            if (result.Error != 0)
            {
                return Unauthorized(result);
            }

            Response.DeleteJwtTokenCookies(GetAccessTokenCookieName(), GetRefreshTokenCookieName());
            return Ok(new
            {
                Error = 0,
                Message = "Logout-ed!"
            });
        }

        private static Result<AuthResponse> ToResponseResult(Result<AuthResult> result)
        {
            return new Result<AuthResponse>
            {
                Error = result.Error,
                Message = result.Message,
                Data = result.Data?.Response
            };
        }

        private string GetAccessTokenCookieName()
        {
            return _configuration[$"{JwtSettings.SectionName}:AccessTokenCookieName"]
                ?? new JwtSettings().AccessTokenCookieName;
        }

        private string GetRefreshTokenCookieName()
        {
            return _configuration[$"{JwtSettings.SectionName}:RefreshTokenCookieName"]
                ?? new JwtSettings().RefreshTokenCookieName;
        }
    }
}
