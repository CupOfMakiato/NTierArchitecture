using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("get-current-user")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUserById()
        {
            var result = await _userService.GetCurrentUserById();
            if (result.Error != 0)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }
    }
}
