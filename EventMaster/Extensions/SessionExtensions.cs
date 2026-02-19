using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace EventMaster.Extensions;

public static class SessionExtensions
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static void SetJson<T>(this ISession session, string key, T value)
        => session.SetString(key, JsonSerializer.Serialize(value, Options));

    public static T? GetJson<T>(this ISession session, string key)
    {
        var json = session.GetString(key);
        return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, Options);
    }
}