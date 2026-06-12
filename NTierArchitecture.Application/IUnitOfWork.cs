using NTierArchitecture.Application.IRepositories;

namespace NTierArchitecture.Application
{
    public interface IUnitOfWork
    {
        public IUserRepository UserRepository { get; }
        //public IAuthRepository AuthRepository { get; }
        public Task<int> SaveChangeAsync();
    }
}
