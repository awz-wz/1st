using System.Text.Json;
using PIWebAPIApp.Utilities;

namespace PIWebAPIApp.Models
{
    public class PIValue
    {
        public JsonElement Value { get; set; }
        public string Timestamp { get; set; }
        public bool Good { get; set; }
        public string UnitsAbbreviation { get; set; }

        public T GetValue<T>()
        {
            try
            {
                return Value.Deserialize<T>();
            }
            catch
            {
                return default(T);
            }
        }

        public string GetDisplayValue()
        {
            switch (Value.ValueKind)
            {
                case JsonValueKind.String:
                    return Value.GetString();
                case JsonValueKind.Number:
                    return Value.GetDouble().ToString();
                case JsonValueKind.True:
                    return "True";
                case JsonValueKind.False:
                    return "False";
                case JsonValueKind.Object:
                    // Извлекаем поле "Name" из объекта
                    if (Value.TryGetProperty("Name", out var nameProperty) && 
                        nameProperty.ValueKind == JsonValueKind.String)
                    {
                        return nameProperty.GetString();
                    }
                    // Если нет поля "Name", возвращаем сырой текст
                    return Value.GetRawText();
                case JsonValueKind.Array:
                    return Value.GetRawText();
                case JsonValueKind.Null:
                    return "null";
                default:
                    return Value.ToString();
            }
        }

        public override string ToString()
        {
            return $"Value: {GetDisplayValue()}, Timestamp: {Timestamp}, Good: {Good}";
        }
    }
}