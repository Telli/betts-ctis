using System.Security.Cryptography;
using System.Text;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Data;
using BettsTax.Data.Models.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RefreshTokenService> _logger;
    private readonly TimeSpan _lifetime;

    public RefreshTokenService(ApplicationDbContext dbContext, IConfiguration configuration, ILogger<RefreshTokenService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        var days = configuration.GetValue<int?>("Authentication:RefreshTokenDays") ?? 7;
        _lifetime = TimeSpan.FromDays(days);
    }

    public async Task<(RefreshToken Entity, string RawToken)> CreateAsync(ApplicationUser user, string ipAddress, string? userAgent)
    {
        var rawToken = GenerateToken();
        var entity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(rawToken),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            CreatedByUserAgent = userAgent,
            ExpiresAt = DateTime.UtcNow.Add(_lifetime)
        };

        _dbContext.RefreshTokens.Add(entity);
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Issued refresh token {TokenId} for user {UserId}", entity.Id, user.Id);

        return (entity, rawToken);
    }

    public async Task<RefreshToken?> GetValidTokenAsync(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var hash = Hash(rawToken);
        var token = await _dbContext.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == hash);

        if (token == null)
        {
            _logger.LogWarning("Refresh token lookup failed: hash {Hash}", hash);
            return null;
        }

        if (token.RevokedAt.HasValue)
        {
            _logger.LogWarning("Refresh token {TokenId} has been revoked", token.Id);
            return null;
        }

        if (token.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token {TokenId} expired at {Expires}", token.Id, token.ExpiresAt);
            return null;
        }

        return token;
    }

    public async Task RevokeAsync(RefreshToken token, string reason, string? ipAddress = null, string? userAgent = null, bool compromised = false)
    {
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.RevokedByUserAgent = userAgent;
        token.RevocationReason = reason;
        token.IsCompromised = compromised;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Revoked refresh token {TokenId} for reason {Reason}", token.Id, reason);
    }

    public async Task<(RefreshToken Entity, string RawToken)> RotateAsync(RefreshToken existingToken, string ipAddress, string? userAgent)
    {
        await RevokeAsync(existingToken, "Rotated", ipAddress, userAgent);
        var (entity, rawToken) = await CreateAsync(existingToken.User, ipAddress, userAgent);
        existingToken.ReplacedByTokenHash = entity.TokenHash;
        await _dbContext.SaveChangesAsync();
        return (entity, rawToken);
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
