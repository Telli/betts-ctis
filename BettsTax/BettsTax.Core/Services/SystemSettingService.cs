using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BettsTax.Data;
using System.Text.Json;

namespace BettsTax.Core.Services
{
    public class SystemSettingService : ISystemSettingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SystemSettingService> _logger;

        public SystemSettingService(ApplicationDbContext context, ILogger<SystemSettingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            try
            {
                var setting = await _context.SystemSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Key == key);

                return setting?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving setting {Key}", key);
                return null;
            }
        }

        public async Task<T?> GetSettingAsync<T>(string key)
        {
            try
            {
                var value = await GetSettingAsync(key);
                if (value == null)
                    return default;

                if (typeof(T) == typeof(string))
                    return (T)(object)value;

                if (typeof(T) == typeof(bool))
                    return (T)(object)bool.Parse(value);

                if (typeof(T) == typeof(int))
                    return (T)(object)int.Parse(value);

                // For complex types, try JSON deserialization
                return JsonSerializer.Deserialize<T>(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing setting {Key} to type {Type}", key, typeof(T).Name);
                return default;
            }
        }

        public async Task SetSettingAsync(string key, string value, string userId, string? description = null, string category = "General")
        {
            try
            {
                var setting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.Key == key);

                if (setting != null)
                {
                    setting.Value = value;
                    setting.Description = description ?? setting.Description;
                    setting.Category = category;
                    setting.UpdatedDate = DateTime.UtcNow;
                    setting.UpdatedByUserId = userId;
                }
                else
                {
                    setting = new SystemSetting
                    {
                        Key = key,
                        Value = value,
                        Description = description,
                        Category = category,
                        UpdatedByUserId = userId,
                        IsEncrypted = IsPasswordField(key)
                    };
                    _context.SystemSettings.Add(setting);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Setting {Key} updated by user {UserId}", key, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting {Key} by user {UserId}", key, userId);
                throw;
            }
        }

        public async Task<Dictionary<string, string>> GetSettingsByCategoryAsync(string category)
        {
            try
            {
                var settings = await _context.SystemSettings
                    .AsNoTracking()
                    .Where(s => s.Category == category)
                    .ToDictionaryAsync(s => s.Key, s => s.Value);

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings for category {Category}", category);
                return new Dictionary<string, string>();
            }
        }

        public async Task<Dictionary<string, string>> GetEmailSettingsAsync()
        {
            var settings = await GetSettingsByCategoryAsync("Email");
            
            // Return default values if settings don't exist
            var defaultSettings = new Dictionary<string, string>
            {
                ["Email.SmtpHost"] = "",
                ["Email.SmtpPort"] = "587",
                ["Email.Username"] = "",
                ["Email.Password"] = "",
                ["Email.FromEmail"] = "noreply@thebettsfirmsl.com",
                ["Email.FromName"] = "The Betts Firm",
                ["Email.UseSSL"] = "true",
                ["Email.UseTLS"] = "true"
            };

            // Override with actual settings
            foreach (var setting in settings)
            {
                defaultSettings[setting.Key] = setting.Value;
            }

            return defaultSettings;
        }

        public async Task UpdateEmailSettingsAsync(Dictionary<string, string> settings, string userId)
        {
            try
            {
                foreach (var setting in settings)
                {
                    await SetSettingAsync(setting.Key, setting.Value, userId, GetEmailSettingDescription(setting.Key), "Email");
                }

                _logger.LogInformation("Email settings updated by user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating email settings by user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> SettingExistsAsync(string key)
        {
            try
            {
                return await _context.SystemSettings
                    .AsNoTracking()
                    .AnyAsync(s => s.Key == key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if setting {Key} exists", key);
                return false;
            }
        }

        public async Task DeleteSettingAsync(string key, string userId)
        {
            try
            {
                var setting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.Key == key);

                if (setting != null)
                {
                    _context.SystemSettings.Remove(setting);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Setting {Key} deleted by user {UserId}", key, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting setting {Key} by user {UserId}", key, userId);
                throw;
            }
        }

        private static bool IsPasswordField(string key)
        {
            var passwordFields = new[] { "Email.Password", "Database.Password", "JWT.Key" };
            return passwordFields.Contains(key, StringComparer.OrdinalIgnoreCase);
        }

        private static string? GetEmailSettingDescription(string key)
        {
            return key switch
            {
                "Email.SmtpHost" => "SMTP server hostname or IP address",
                "Email.SmtpPort" => "SMTP server port number (typically 587 or 25)",
                "Email.Username" => "SMTP authentication username",
                "Email.Password" => "SMTP authentication password",
                "Email.FromEmail" => "Default sender email address",
                "Email.FromName" => "Default sender display name",
                "Email.UseSSL" => "Enable SSL encryption",
                "Email.UseTLS" => "Enable TLS encryption",
                _ => null
            };
        }
    }
}