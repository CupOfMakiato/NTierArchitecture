using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NTierArchitecture.Application.IServices;

namespace NTierArchitecture.API.Services
{
    public class ClaimService : IClaimService
    {
        public ClaimService(IHttpContextAccessor httpContextAccessor)
        {
            var id = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("id");

            GetCurrentUserId = Guid.TryParse(id, out var userId) ? userId : Guid.Empty;
        }

        public Guid GetCurrentUserId { get; }
    }
}
