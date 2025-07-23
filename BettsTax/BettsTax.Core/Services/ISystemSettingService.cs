namespace BettsTax.Core.Services
{
    public interface ISystemSettingService
    {
        Task<string?> GetSettingAsync(string key);
        Task<T?> GetSettingAsync<T>(string key);
        Task SetSettingAsync(string key, string value, string userId, string? description = null, string category = "General");
        Task<Dictionary<string, string>> GetSettingsByCategoryAsync(string category);
        Task<Dictionary<string, string>> GetEmailSettingsAsync();
        Task UpdateEmailSettingsAsync(Dictionary<string, string> settings, string userId);
        Task<bool> SettingExistsAsync(string key);
        Task DeleteSettingAsync(string key, string userId);
    }
}