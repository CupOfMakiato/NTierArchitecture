using Microsoft.Extensions.Configuration;
using NTierArchitecture.Application.IServices;
using NTierArchitecture.Application.Utils;

namespace NTierArchitecture.Application.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return password.Hash(12);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return password.VerifyHash(passwordHash);
        }
    }
}
