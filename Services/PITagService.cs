using PIWebAPIApp.Models;
using PIWebAPIApp.Services;
using PIWebAPIApp.Utilities;

namespace PIWebAPIApp.Services
{
    public class PITagService
    {
        private readonly PIWebApiClient _client;

        public PITagService(PIWebApiClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<bool> ChangeDigitalStateAsync(string dataServerWebId, string tagName, string newState)
        {
            if (string.IsNullOrWhiteSpace(dataServerWebId))
                throw new ArgumentException("Data server WebId cannot be null or empty", nameof(dataServerWebId));
            
            if (string.IsNullOrWhiteSpace(tagName))
                throw new ArgumentException("Tag name cannot be null or empty", nameof(tagName));
            
            if (string.IsNullOrWhiteSpace(newState))
                throw new ArgumentException("New state cannot be null or empty", nameof(newState));

            try
            {
                Logger.Info($"Changing digital state for tag '{tagName}' to '{newState}'");

                // Найти тег
                var point = await _client.FindPointAsync(dataServerWebId, tagName);
                if (point == null)
                {
                    Logger.Error($"Tag '{tagName}' not found");
                    return false;
                }

                Logger.Info($"Found tag: {point}");

                // Прочитать текущее значение
                var currentValue = await _client.ReadValueAsync(point.WebId);
                Logger.Info($"Current value: {currentValue}");

                // Записать новое значение
                var writeSuccess = await _client.WriteValueAsync(point.WebId, newState);
                
                if (writeSuccess)
                {
                    Logger.Info($"Successfully changed {tagName} to {newState}");
                    
                    // Добавим небольшую задержку перед проверкой
                    await Task.Delay(1000);
                    
                    // Проверить обновленное значение
                    var updatedValue = await _client.ReadValueAsync(point.WebId);
                    Logger.Info($"Updated value: {updatedValue}");
                    
                    return true;
                }
                else
                {
                    Logger.Error($"Failed to change {tagName} to {newState}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error changing digital state for '{tagName}': {ex.Message}");
                return false;
            }
        }

        public async Task<PIValue> GetTagValueAsync(string dataServerWebId, string tagName)
        {
            try
            {
                var point = await _client.FindPointAsync(dataServerWebId, tagName);
                if (point == null)
                {
                    Logger.Error($"Tag '{tagName}' not found");
                    return null;
                }

                return await _client.ReadValueAsync(point.WebId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting tag value for '{tagName}': {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SetTagValueAsync(string dataServerWebId, string tagName, object value)
        {
            try
            {
                var point = await _client.FindPointAsync(dataServerWebId, tagName);
                if (point == null)
                {
                    Logger.Error($"Tag '{tagName}' not found");
                    return false;
                }

                return await _client.WriteValueAsync(point.WebId, value);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting tag value for '{tagName}': {ex.Message}");
                return false;
            }
        }
    }
}