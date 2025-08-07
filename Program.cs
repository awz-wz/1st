using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string piWebApiUrl = "https://piwebd2.tengizchevroil.com/piwebapi";
        string dataServerWebId = "F1DSWCdJQqyHMkueMiGiQ3KQpwVENPR0lUlRQSUQ";
        string tagName = "KTL-FWS-K-260.MV";
 
        var handler = new HttpClientHandler
        {
            UseDefaultCredentials = true
        };
 
        using var client = new HttpClient(handler);

        try
        {
            string searchUrl = $"{piWebApiUrl}/dataservers/{dataServerWebId}/points?nameFilter={tagName}";
            var searchResponse = await client.GetAsync(searchUrl);
            searchResponse.EnsureSuccessStatusCode();
            var searchContent = await searchResponse.Content.ReadAsStringAsync();
            using var searchDoc = JsonDocument.Parse(searchContent);

            var items = searchDoc.RootElement.GetProperty("Items");
            if (items.GetArrayLength() == 0)
            {
                Console.WriteLine($"Tag '{tagName}' not found.");
                return;
            }

            string tagWebId = items[0].GetProperty("WebId").GetString();

            string valueUrl = $"{piWebApiUrl}/streams/{tagWebId}/value";

            var payload = new
            {
                Value = 0 // 0 соответствует CLOSED
            };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            var valueResponse = await client.PostAsync(valueUrl, content);
            valueResponse.EnsureSuccessStatusCode();
            var valueContent = await valueResponse.Content.ReadAsStringAsync();
            using var valueDoc = JsonDocument.Parse(valueContent);

            // Если требуется распечатать детали результата, можно извлечь нужные свойства, например:
            // var value = valueDoc.RootElement.GetProperty("Value");
            // var timestamp = valueDoc.RootElement.GetProperty("Timestamp").GetString();

            if (valueResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Value успешно изменено на CLOSED.");
            }
            else
            {
                Console.WriteLine($"Ошибка при изменении значения: {valueResponse.StatusCode}");
            }
            Console.WriteLine($"HTTP Status: {valueResponse.StatusCode}");
            foreach (var header in valueResponse.Headers)
            Console.WriteLine($"{header.Key}: {string.Join(",", header.Value)}");
            foreach (var header in valueResponse.Content.Headers)
            Console.WriteLine($"{header.Key}: {string.Join(",", header.Value)}");
            Console.WriteLine($"Body: {valueResponse}");

        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}