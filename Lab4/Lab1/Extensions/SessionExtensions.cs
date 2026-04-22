using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace Lab1.Extensions;

public static class SessionExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public static void SetObject<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value, SerializerOptions));
    }

    public static T? GetObject<T>(this ISession session, string key)
    {
        var payload = session.GetString(key);
        return payload == null ? default : JsonSerializer.Deserialize<T>(payload, SerializerOptions);
    }
}
