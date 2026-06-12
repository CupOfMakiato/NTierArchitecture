using NTierArchitecture.Domain.Entities;

namespace NTierArchitecture.Application.IRepositories
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<List<User>> GetAllUser();
        Task<User?> GetUserById(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<bool> IsEmailTakenAsync(string email);
        Task<Role?> GetRoleByIdAsync(int roleId);
        Task<Role?> GetRoleByNameAsync(string roleName);
    }
}
