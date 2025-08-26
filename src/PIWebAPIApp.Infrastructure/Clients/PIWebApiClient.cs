using System.Text;
using System.Text.Json;
using PIWebAPIApp.Models;
using PIWebAPIApp.Utilities;

namespace PIWebAPIApp.Services
{
    public class PIWebApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private bool _disposed = false;

        public PIWebApiClient(PIConfiguration config)
        {
            if (config == null || !config.IsValid())
                throw new ArgumentException("Invalid configuration");

            _baseUrl = config.BaseUrl.TrimEnd('/');
            
            var handler = new HttpClientHandler
            {
                UseDefaultCredentials = config.UseDefaultCredentials
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds)
            };

            Logger.Info($"PIWebApiClient initialized with base URL: {_baseUrl}");
        }

        public async Task<PIPoint> FindPointAsync(string dataServerWebId, string tagName)
        {
            if (string.IsNullOrWhiteSpace(dataServerWebId))
                throw new ArgumentException("Data server WebId cannot be null or empty", nameof(dataServerWebId));
            
            if (string.IsNullOrWhiteSpace(tagName))
                throw new ArgumentException("Tag name cannot be null or empty", nameof(tagName));

            try
            {
                var url = $"{_baseUrl}/dataservers/{dataServerWebId}/points?nameFilter={Uri.EscapeDataString(tagName)}";
                Logger.Debug($"Searching for point: {url}");

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Error($"Failed to find point. Status: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var items = doc.RootElement.GetProperty("Items");

                if (items.GetArrayLength() == 0)
                {
                    Logger.Info($"Tag '{tagName}' not found");
                    return null;
                }

                var item = items[0];
                var point = new PIPoint
                {
                    WebId = item.GetProperty("WebId").GetString(),
                    Name = item.GetProperty("Name").GetString(),
                    Path = item.TryGetProperty("Path", out var path) ? path.GetString() : null,
                    Descriptor = item.TryGetProperty("Descriptor", out var desc) ? desc.GetString() : null,
                    PointClass = item.TryGetProperty("PointClass", out var pointClass) ? pointClass.GetString() : null,
                    PointType = item.TryGetProperty("PointType", out var pointType) ? pointType.GetString() : null
                };

                Logger.Info($"Found point: {point.Name} (WebId: {point.WebId})");
                return point;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error finding point '{tagName}': {ex.Message}");
                throw;
            }
        }

        public async Task<PIValue> ReadValueAsync(string pointWebId)
        {
            if (string.IsNullOrWhiteSpace(pointWebId))
                throw new ArgumentException("Point WebId cannot be null or empty", nameof(pointWebId));

            try
            {
                var url = $"{_baseUrl}/streams/{pointWebId}/value";
                Logger.Debug($"Reading value from: {url}");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                var piValue = new PIValue
                {
                    Value = doc.RootElement.GetProperty("Value").Clone(),
                    Timestamp = doc.RootElement.GetProperty("Timestamp").GetString(),
                    UnitsAbbreviation = doc.RootElement.TryGetProperty("UnitsAbbreviation", out var units) ? units.GetString() : null,
                    Good = doc.RootElement.TryGetProperty("Good", out var good) ? good.GetBoolean() : true
                };

                Logger.Debug($"Read value: {piValue.GetDisplayValue()}");
                return piValue;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reading value for WebId '{pointWebId}': {ex.Message}");
                throw;
            }
        }

        public async Task<bool> WriteValueAsync(string pointWebId, object value)
        {
            if (string.IsNullOrWhiteSpace(pointWebId))
                throw new ArgumentException("Point WebId cannot be null or empty", nameof(pointWebId));

            try
            {
                var url = $"{_baseUrl}/streams/{pointWebId}/value";
                Logger.Debug($"Writing to URL: {url}");

                var payload = new
                {
                    Value = value,
                    Timestamp = DateTime.UtcNow.ToString("o")
                };







                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions 
                { 
                    WriteIndented = false 
                });
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                Logger.Debug($"Payload: {json}");

                var response = await _httpClient.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    Logger.Info($"Successfully wrote value '{value}' to point WebId: {pointWebId}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Logger.Error($"Failed to write value. Status: {response.StatusCode}, Content: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error writing value to WebId '{pointWebId}': {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _httpClient?.Dispose();
                _disposed = true;
                Logger.Info("PIWebApiClient disposed");
            }
        }
    }
}