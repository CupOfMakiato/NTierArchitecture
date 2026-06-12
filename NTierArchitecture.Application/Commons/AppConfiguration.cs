namespace NTierArchitecture.Application.Commons
{
    public class AppConfiguration
    {
        public string DatabaseConnection { get; set; } = string.Empty;
        public string JWTSecretKey { get; set; } = string.Empty;
    }
}
