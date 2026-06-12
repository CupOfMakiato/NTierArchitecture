namespace NTierArchitecture.Application.IServices
{
    public interface IClaimService
    {
        public Guid GetCurrentUserId { get; }
    }
}
