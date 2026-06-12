using NTierArchitecture.Application.IServices;

namespace NTierArchitecture.Application.Services
{
    public class CurrentTime : ICurrentTime
    {
        public DateTime GetCurrentTime() => DateTime.UtcNow;
    }
}
