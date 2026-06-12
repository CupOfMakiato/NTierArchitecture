using NTierArchitecture.Application.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NTierArchitecture.Application.DTOs.User;
using NTierArchitecture.Application.Abstractions.Shared;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NTierArchitecture.Application.IRepositories;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace NTierArchitecture.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEncryptionService _encryptionService;

        public UserService(
            IUserRepository userRepository,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IEncryptionService encryptionService)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _encryptionService = encryptionService;
        }
        public async Task<Result<UserDTO>> GetCurrentUserById()
        {
            var userIdValue = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("id");

            if (!Guid.TryParse(userIdValue, out var userId))
            {
                return new Result<UserDTO>() { Error = 1, Message = "Invalid token", Data = null };
            }

            var user = await _userRepository.GetUserById(userId);

            if (user == null)
                return new Result<UserDTO>() { Error = 1, Message = "User not found", Data = null };

            // This should return success when user is found
            var userDto = _mapper.Map<UserDTO>(user);
            userDto.Email = _encryptionService.Decrypt(user.Email);
            return new Result<UserDTO>
            {
                Error = 0,
                Message = "Success",
                Data = userDto
            };
        }
    }
}
