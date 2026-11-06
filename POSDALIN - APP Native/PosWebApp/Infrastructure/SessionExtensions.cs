using System.Text.Json;

namespace PosWebApp.Infrastructure;

/// <summary>
/// Typed helpers for storing and retrieving objects in ASP.NET Core session state
/// using <see cref="JsonSerializer"/> for serialization.
/// </summary>
public static class SessionExtensions
{
    /// <summary>Serializes <paramref name="value"/> as JSON and stores it under <paramref name="key"/>.</summary>
    public static void SetObject<T>(this ISession session, string key, T value)
        => session.SetString(key, JsonSerializer.Serialize(value));

    /// <summary>
    /// Retrieves the JSON string stored under <paramref name="key"/> and deserializes it to <typeparamref name="T"/>.
    /// Returns <see langword="default"/> when the key is absent.
    /// </summary>
    public static T? GetObject<T>(this ISession session, string key)
    {
        var json = session.GetString(key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }
}
