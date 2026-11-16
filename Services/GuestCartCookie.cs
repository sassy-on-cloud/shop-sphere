using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace ShopSphere.Services;

public sealed class GuestCartCookie(IWebHostEnvironment environment)
{
    public const string CookieName = "ShopSphere.GuestCart";

    public string? GetToken(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var token))
        {
            return null;
        }

        try
        {
            return WebEncoders.Base64UrlDecode(token).Length == 32 ? token : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public string EnsureToken(HttpContext context)
    {
        var token = GetToken(context);
        if (token is not null)
        {
            return token;
        }

        token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        context.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(30),
            Path = "/",
            SameSite = SameSiteMode.Lax,
            Secure = !environment.IsDevelopment() || context.Request.IsHttps
        });

        return token;
    }

    public void Delete(HttpContext context) =>
        context.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
