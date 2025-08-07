using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq; // Added for EnumerateObject()

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
            // Step 1: Search for the tag
            string searchUrl = $"{piWebApiUrl}/dataservers/{dataServerWebId}/points?nameFilter={tagName}";
            Console.WriteLine($"Searching for tag: {searchUrl}");
            
            var searchResponse = await client.GetAsync(searchUrl);
            Console.WriteLine($"Search Response Status: {searchResponse.StatusCode}");
            Console.WriteLine($"Search Response Headers:");
            foreach (var header in searchResponse.Headers)
                Console.WriteLine($"  {header.Key}: {string.Join(",", header.Value)}");
            foreach (var header in searchResponse.Content.Headers)
                Console.WriteLine($"  {header.Key}: {string.Join(",", header.Value)}");

            var searchContent = await searchResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Search Response Body Length: {searchContent.Length}");
            Console.WriteLine($"Search Response Body: {searchContent}");

            // Check if response is successful before parsing
            if (!searchResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Search request failed with status: {searchResponse.StatusCode}");
                Console.WriteLine($"Response content: {searchContent}");
                return;
            }

            // Check if content is empty before parsing JSON
            if (string.IsNullOrWhiteSpace(searchContent))
            {
                Console.WriteLine("Search response body is empty or whitespace");
                return;
            }

            // Try to parse JSON with better error handling
            JsonDocument searchDoc;
            try
            {
                searchDoc = JsonDocument.Parse(searchContent);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse search response as JSON: {ex.Message}");
                Console.WriteLine($"Response content was: '{searchContent}'");
                return;
            }

            using (searchDoc)
            {
                // Check if Items property exists
                if (!searchDoc.RootElement.TryGetProperty("Items", out var items))
                {
                    Console.WriteLine("Response does not contain 'Items' property");
                    Console.WriteLine($"Available properties: {string.Join(", ", searchDoc.RootElement.EnumerateObject().Select(p => p.Name))}");
                    return;
                }

                if (items.GetArrayLength() == 0)
                {
                    Console.WriteLine($"Tag '{tagName}' not found.");
                    return;
                }

                if (!items[0].TryGetProperty("WebId", out var webIdProperty))
                {
                    Console.WriteLine("First item does not contain 'WebId' property");
                    return;
                }

                string tagWebId = webIdProperty.GetString();
                Console.WriteLine($"Found tag WebId: {tagWebId}");

                // Step 2: Set the tag value
                string valueUrl = $"{piWebApiUrl}/streams/{tagWebId}/value";
                Console.WriteLine($"Setting value at: {valueUrl}");

                var payload = new
                {
                    Value = 0 // 0 соответствует CLOSED
                };
                var jsonPayload = JsonSerializer.Serialize(payload);
                Console.WriteLine($"Payload: {jsonPayload}");
                
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var valueResponse = await client.PostAsync(valueUrl, content);
                
                Console.WriteLine($"Value Response Status: {valueResponse.StatusCode}");
                Console.WriteLine($"Value Response Headers:");
                foreach (var header in valueResponse.Headers)
                    Console.WriteLine($"  {header.Key}: {string.Join(",", header.Value)}");
                foreach (var header in valueResponse.Content.Headers)
                    Console.WriteLine($"  {header.Key}: {string.Join(",", header.Value)}");

                var valueContent = await valueResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Value Response Body Length: {valueContent.Length}");
                Console.WriteLine($"Value Response Body: {valueContent}");

                if (!valueResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Value update failed with status: {valueResponse.StatusCode}");
                    Console.WriteLine($"Response content: {valueContent}");
                    return;
                }

                // Only try to parse JSON if there's content
                if (!string.IsNullOrWhiteSpace(valueContent))
                {
                    try
                    {
                        using var valueDoc = JsonDocument.Parse(valueContent);
                        Console.WriteLine("Value response parsed successfully");
                        
                        // Extract value details if available
                        if (valueDoc.RootElement.TryGetProperty("Value", out var valueProperty))
                        {
                            Console.WriteLine($"New Value: {valueProperty}");
                        }
                        if (valueDoc.RootElement.TryGetProperty("Timestamp", out var timestampProperty))
                        {
                            Console.WriteLine($"Timestamp: {timestampProperty.GetString()}");
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Failed to parse value response as JSON: {ex.Message}");
                        Console.WriteLine($"Response content was: '{valueContent}'");
                        // Continue execution as the operation might still have succeeded
                    }
                }

                if (valueResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine("Value успешно изменено на CLOSED.");
                }
                else
                {
                    Console.WriteLine($"Ошибка при изменении значения: {valueResponse.StatusCode}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"Request timeout: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}