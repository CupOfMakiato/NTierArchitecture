
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
        private readonly IEncryptionService _encryptionService;

        public UserRepository(
            AppDbContext dbContext,
            ICurrentTime timeService,
            IClaimService claimsService,
            IEncryptionService encryptionService)
            : base(dbContext, timeService, claimsService)
        {
            _dbContext = dbContext;
            _encryptionService = encryptionService;
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

        public async Task<User?> GetByEmailAsync(string email)
        {
            var normalizedValue = NormalizeEmail(email);

            var users = await _dbContext.User
                .Include(user => user.Role)
                .ToListAsync();

            return users.FirstOrDefault(user =>
                NormalizeEmail(_encryptionService.Decrypt(user.Email)) == normalizedValue);
        }

        public async Task<bool> IsEmailTakenAsync(string email)
        {
            var normalizedEmail = NormalizeEmail(email);

            var users = await _dbContext.User.ToListAsync();

            return users.Any(user =>
                NormalizeEmail(_encryptionService.Decrypt(user.Email)) == normalizedEmail);
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

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }
}
