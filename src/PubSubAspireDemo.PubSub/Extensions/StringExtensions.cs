using System.Text.Json;

namespace PubSubAspireDemo.PubSub.Extensions;

public static class StringExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, 
        WriteIndented = true 
    };

    public static string ToJsonStr(this object obj)
    {
        if (obj == null)
        {
            return default!;
        }
        
        return JsonSerializer.Serialize(obj, _jsonSerializerOptions);
    }
}
