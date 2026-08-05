using System.Security.Claims;
using System.Text.Json;

namespace EntertainmentDocs.Admin.Infrastructure.Authentication;

public static class JwtClaimsParser
{
    public static ClaimsPrincipal CreatePrincipal(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length != 3)
            return new ClaimsPrincipal(new ClaimsIdentity());

        try
        {
            var payload = Decode(parts[1]);
            using var document = JsonDocument.Parse(payload);
            var claims = new List<Claim>();

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.Value.EnumerateArray())
                        claims.Add(new Claim(property.Name, ReadClaimValue(item)));
                }
                else
                {
                    claims.Add(new Claim(property.Name, ReadClaimValue(property.Value)));
                }
            }

            var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);
            return new ClaimsPrincipal(identity);
        }
        catch (FormatException)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
        catch (JsonException)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    public static bool IsExpired(ClaimsPrincipal principal, DateTimeOffset now)
    {
        var expiration = principal.FindFirst("exp")?.Value;
        return !long.TryParse(expiration, out var seconds) || DateTimeOffset.FromUnixTimeSeconds(seconds) <= now;
    }

    private static byte[] Decode(string payload)
    {
        var normalized = payload.Replace('-', '+').Replace('_', '/');
        normalized = normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=');
        return Convert.FromBase64String(normalized);
    }

    private static string ReadClaimValue(JsonElement value) =>
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : value.GetRawText();
}
