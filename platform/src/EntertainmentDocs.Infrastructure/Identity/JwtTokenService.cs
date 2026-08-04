using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace EntertainmentDocs.Infrastructure.Identity;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public string CreateAccessToken(ApplicationUser user, IReadOnlyCollection<string> roles)
    {
        var settings = options.Value;
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(settings.AccessTokenMinutes);

        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };

        var payload = new Dictionary<string, object?>
        {
            ["iss"] = settings.Issuer,
            ["aud"] = settings.Audience,
            ["sub"] = user.Id.ToString(),
            ["jti"] = Guid.NewGuid().ToString("N"),
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = expires.ToUnixTimeSeconds(),
            [ClaimTypes.NameIdentifier] = user.Id.ToString(),
            [ClaimTypes.Name] = user.DisplayName,
            [ClaimTypes.Email] = user.Email,
            [ClaimTypes.Role] = roles.ToArray()
        };

        var encodedHeader = EncodeJson(header);
        var encodedPayload = EncodeJson(payload);
        var unsignedToken = $"{encodedHeader}.{encodedPayload}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(settings.SigningKey));
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken)));
        return $"{unsignedToken}.{signature}";
    }

    private static string EncodeJson<T>(T value) =>
        Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(value));

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
