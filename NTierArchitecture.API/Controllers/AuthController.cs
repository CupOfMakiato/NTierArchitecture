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
            var result = await _authService.RequestRegisterOtpAsync(request);
            if (result.Error != 0)
            {
                return BadRequest(result);
            }

            return Ok(new
            {
                Error = 0,
                Message = "OTP email verification sent."
            });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var result = await _authService.RequestLoginAsync(request);
            if (result.Error != 0)
            {
                return Unauthorized(result);
            }

            // return Ok(new
            // {
            //     Error = 0,
            //     Message = "OTP email verification sent."
            // });
            if (result.Data != null)
            {
                Response.AppendJwtTokenCookies(result.Data.Tokens, Request.IsHttps);
            }

            return Ok(new
            {
                Error = 0,
                Message = result.Message
            });
        }

        [AllowAnonymous]
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
        {
            var result = await _authService.VerifyOtpAsync(request);
            if (result.Error != 0)
            {
                return BadRequest(result);
            }

            if (result.Data != null)
            {
                Response.AppendJwtTokenCookies(result.Data.Tokens, Request.IsHttps);
            }

            return Ok(new
            {
                Error = 0,
                Message = result.Message
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
