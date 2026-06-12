using NTierArchitecture.Application;
using NTierArchitecture.Application.IRepositories;
using NTierArchitecture.Infrastructure.Database;
namespace NTierArchitecture.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _dbContext;
        private readonly IUserRepository _userRepository;

        public UnitOfWork(AppDbContext dbContext,
            IUserRepository userRepository)

        {
            _dbContext = dbContext;
            _userRepository = userRepository;
        }

        public IUserRepository UserRepository => _userRepository;
        public async Task<int> SaveChangeAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
