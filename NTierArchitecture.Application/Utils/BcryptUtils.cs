namespace NTierArchitecture.Application.Utils
{
    public static class BcryptUtils
    {
        public static string Hash(this string input, int workFactor = 12)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("Password cannot be empty.", nameof(input));
            }

            return BCrypt.Net.BCrypt.HashPassword(input, workFactor);
        }

        public static bool VerifyHash(this string input, string hash)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(hash))
            {
                return false;
            }

            return BCrypt.Net.BCrypt.Verify(input, hash);
        }
    }
}
