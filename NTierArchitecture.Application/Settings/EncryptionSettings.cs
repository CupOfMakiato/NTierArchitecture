namespace NTierArchitecture.Application.Settings
{
    public class EncryptionSettings
    {
        public const string SectionName = "EncryptionSettings";

        public string AesKey { get; set; } = string.Empty;
    }
}
