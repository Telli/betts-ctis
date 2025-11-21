using BettsTax.Data;
using BettsTax.Data.Models.Security;

namespace BettsTax.Core.Services.Interfaces;

public interface IRefreshTokenService
{
    Task<(RefreshToken Entity, string RawToken)> CreateAsync(ApplicationUser user, string ipAddress, string? userAgent);
    Task<RefreshToken?> GetValidTokenAsync(string rawToken);
    Task RevokeAsync(RefreshToken token, string reason, string? ipAddress = null, string? userAgent = null, bool compromised = false);
    Task<(RefreshToken Entity, string RawToken)> RotateAsync(RefreshToken existingToken, string ipAddress, string? userAgent);
}
