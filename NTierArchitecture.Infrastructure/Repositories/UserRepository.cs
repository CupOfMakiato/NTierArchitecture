
using Microsoft.EntityFrameworkCore;
using NTierArchitecture.Application.IRepositories;
using NTierArchitecture.Application.IServices;
using NTierArchitecture.Domain.Entities;
using NTierArchitecture.Infrastructure.Database;

namespace NTierArchitecture.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private readonly AppDbContext _dbContext;

        public UserRepository(
            AppDbContext dbContext,
            ICurrentTime timeService,
            IClaimService claimsService)
            : base(dbContext, timeService, claimsService)
        {
            _dbContext = dbContext;
        }

        public Task<List<User>> GetAllUser()
        {
            return _dbContext.User
                .Include(user => user.Role)
                .ToListAsync();
        }

        public Task<User?> GetUserById(Guid id)
        {
            return _dbContext.User
                .Include(user => user.Role)
                .FirstOrDefaultAsync(user => user.Id == id);
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            var normalizedValue = email.Trim().ToLower();

            return _dbContext.User
                .Include(user => user.Role)
                .FirstOrDefaultAsync(user =>
                    user.Email.ToLower() == normalizedValue);
        }

        public Task<bool> IsEmailTakenAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();

            return _dbContext.User.AnyAsync(user =>
                user.Email.ToLower() == normalizedEmail);
        }

        public Task<Role?> GetRoleByIdAsync(int roleId)
        {
            return _dbContext.Role.FirstOrDefaultAsync(role => role.Id == roleId);
        }

        public Task<Role?> GetRoleByNameAsync(string roleName)
        {
            var normalizedRoleName = roleName.Trim().ToLower();
            return _dbContext.Role.FirstOrDefaultAsync(role => role.RoleName.ToLower() == normalizedRoleName);
        }
    }
}
