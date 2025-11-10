using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using BettsTax.Data;
using Microsoft.EntityFrameworkCore;

namespace BettsTax.Core.Services;

public interface IIdempotencyService
{
    Task<bool> IsProcessedAsync(string payload, string source);
    Task MarkAsProcessedAsync(string payload, string source, string? result = null);
    Task<string> GenerateIdempotencyKeyAsync(string payload, string? transactionId = null);
    Task<bool> TryLockAsync(string key, TimeSpan lockDuration);
    Task ReleaseLockAsync(string key);
}

public class IdempotencyService : IIdempotencyService
{
    private readonly IDistributedCache _cache;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IdempotencyService> _logger;
    private const string LOCK_PREFIX = "lock:";
    private const string PROCESSED_PREFIX = "processed:";

    public IdempotencyService(
        IDistributedCache cache,
        ApplicationDbContext context,
        ILogger<IdempotencyService> logger)
    {
        _cache = cache;
        _context = context;
        _logger = logger;
    }

    public async Task<bool> IsProcessedAsync(string payload, string source)
    {
        var key = await GenerateIdempotencyKeyAsync(payload);
        var cacheKey = $"{PROCESSED_PREFIX}{source}:{key}";

        try
        {
            // Check cache first for fast lookup
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Idempotency key found in cache: {Key}", cacheKey);
                return true;
            }

            // Check database for persistent storage using RequestBody as idempotency check
            var exists = await _context.PaymentWebhookLogs
                .AnyAsync(w => w.RequestBody.Contains(key) && w.IsProcessed);

            if (exists)
            {
                // Cache the result for future fast lookups
                await _cache.SetStringAsync(cacheKey, "processed", new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                });
                
                _logger.LogDebug("Idempotency key found in database: {Key}", key);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking idempotency for key: {Key}", key);
            // In case of error, assume not processed to avoid blocking legitimate requests
            return false;
        }
    }

    public async Task MarkAsProcessedAsync(string payload, string source, string? result = null)
    {
        var key = await GenerateIdempotencyKeyAsync(payload);
        var cacheKey = $"{PROCESSED_PREFIX}{source}:{key}";

        try
        {
            // Mark in cache for fast future lookups
            await _cache.SetStringAsync(cacheKey, result ?? "processed", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });

            // Update database record if exists
            var webhookLog = await _context.PaymentWebhookLogs
                .FirstOrDefaultAsync(w => w.IdempotencyKey == key);

            if (webhookLog != null)
            {
                webhookLog.IsProcessed = true;
                webhookLog.ProcessedDate = DateTime.UtcNow;
                webhookLog.ProcessingResult = result;
                await _context.SaveChangesAsync();
            }

            _logger.LogDebug("Marked idempotency key as processed: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking idempotency key as processed: {Key}", key);
            throw;
        }
    }

    public Task<string> GenerateIdempotencyKeyAsync(string payload, string? transactionId = null)
    {
        try
        {
            using var sha256 = SHA256.Create();
            
            // Combine payload with transaction ID if provided for uniqueness
            var input = transactionId != null ? $"{payload}:{transactionId}" : payload;
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(inputBytes);
            
            // Convert to base64 for storage efficiency
            var key = Convert.ToBase64String(hashBytes);
            
            _logger.LogDebug("Generated idempotency key for payload length: {Length}", payload.Length);
            return Task.FromResult(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating idempotency key");
            throw;
        }
    }

    public async Task<bool> TryLockAsync(string key, TimeSpan lockDuration)
    {
        var lockKey = $"{LOCK_PREFIX}{key}";
        
        try
        {
            // Try to acquire lock using cache
            var lockValue = Guid.NewGuid().ToString();
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = lockDuration
            };

            // Check if lock already exists
            var existingLock = await _cache.GetStringAsync(lockKey);
            if (existingLock != null)
            {
                _logger.LogDebug("Lock already exists for key: {Key}", key);
                return false;
            }

            // Set lock
            await _cache.SetStringAsync(lockKey, lockValue, options);
            
            // Verify we got the lock (race condition check)
            var verifyLock = await _cache.GetStringAsync(lockKey);
            if (verifyLock == lockValue)
            {
                _logger.LogDebug("Successfully acquired lock for key: {Key}", key);
                return true;
            }

            _logger.LogDebug("Failed to acquire lock due to race condition for key: {Key}", key);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock for key: {Key}", key);
            return false;
        }
    }

    public async Task ReleaseLockAsync(string key)
    {
        var lockKey = $"{LOCK_PREFIX}{key}";
        
        try
        {
            await _cache.RemoveAsync(lockKey);
            _logger.LogDebug("Released lock for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock for key: {Key}", key);
            // Don't throw - lock will expire naturally
        }
    }
}
