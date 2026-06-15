namespace NTierArchitecture.Application.Settings.MailjetService
{
    public class SentEmailSettings
    {
        public const string SectionName = "SentEmailSettings";
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }
}
