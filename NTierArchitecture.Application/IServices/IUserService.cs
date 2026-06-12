using NTierArchitecture.Application.Abstractions.Shared;
using NTierArchitecture.Application.DTOs.User;

namespace NTierArchitecture.Application.IServices
{
    public interface IUserService
    {
        Task<Result<UserDTO>> GetCurrentUserById();
    }
}
