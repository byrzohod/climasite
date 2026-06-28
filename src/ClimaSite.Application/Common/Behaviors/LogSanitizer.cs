using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace ClimaSite.Application.Common.Behaviors;

/// <summary>
/// Produces a log-safe projection of a MediatR request: any property whose NAME contains a sensitive
/// marker (password, token, secret, card, cvc, …) is replaced with <see cref="Redacted"/>, so the
/// request-logging behavior never writes credentials/tokens to the logs in cleartext.
/// <para>
/// Denylist-by-name is deliberate: the original leak was a *missing* explicit redaction, so this fails
/// CLOSED — any current or future command that adds a sensitively-named field is auto-redacted without
/// touching this class. Over-redacting a non-secret field that merely contains a marker is harmless (it
/// only hides a value from a log line); under-redacting a real secret is not.
/// </para>
/// <para>
/// Redaction RECURSES into nested objects and collections (bounded by <see cref="MaxDepth"/>): Serilog's
/// <c>{@Request}</c> destructures nested objects too, so a secret nested under a non-sensitive-named
/// field would otherwise still leak. Scalars pass through unchanged so request logging keeps its
/// debugging value (emails/ids stay visible — PII-in-logs is tracked separately from credential leaks).
/// </para>
/// </summary>
public static class LogSanitizer
{
    public const string Redacted = "***REDACTED***";

    private const int MaxDepth = 4;

    // Case-insensitive substrings. "token" covers IdToken/RefreshToken/AccessToken/ShareToken;
    // "secret" covers ClientSecret/WebhookSecret; "card" covers CardNumber/CreditCard.
    private static readonly string[] SensitiveMarkers =
    {
        "password", "token", "secret", "card", "cvc", "cvv", "pin", "apikey",
        "credential", "authorization", "cookie"
    };

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    /// <summary>Returns the request's public readable properties with sensitive values (incl. nested) redacted.</summary>
    public static IReadOnlyDictionary<string, object?> Redact(object request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return RedactObject(request, 0);
    }

    private static IReadOnlyDictionary<string, object?> RedactObject(object obj, int depth)
    {
        var props = PropertyCache.GetOrAdd(obj.GetType(), static type => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray());

        var result = new Dictionary<string, object?>(props.Length, StringComparer.Ordinal);
        foreach (var prop in props)
        {
            result[prop.Name] = IsSensitive(prop.Name)
                ? Redacted
                : RedactValue(SafeRead(obj, prop), depth + 1);
        }

        return result;
    }

    private static object? RedactValue(object? value, int depth)
    {
        if (value is null)
        {
            return null;
        }

        // Scalars (incl. string) pass through — they carry no nested properties to leak.
        if (value is string || value is decimal || value is DateTime || value is DateTimeOffset
            || value is Guid || value is TimeSpan)
        {
            return value;
        }

        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum)
        {
            return value;
        }

        // Bound recursion: a deep/cyclic graph terminates here rather than looping forever.
        if (depth >= MaxDepth)
        {
            return value.ToString();
        }

        if (value is IEnumerable enumerable)
        {
            return enumerable.Cast<object?>().Select(item => RedactValue(item, depth + 1)).ToList();
        }

        // Nested complex object — recurse so its sensitive properties are redacted too.
        return RedactObject(value, depth);
    }

    private static bool IsSensitive(string propertyName)
    {
        foreach (var marker in SensitiveMarkers)
        {
            if (propertyName.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static object? SafeRead(object obj, PropertyInfo prop)
    {
        try
        {
            return prop.GetValue(obj);
        }
        catch
        {
            // A throwing getter must never break request logging.
            return null;
        }
    }
}
