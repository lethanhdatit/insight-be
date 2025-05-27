using Microsoft.IdentityModel.JsonWebTokens;
using System;
using System.Linq;
using System.Security.Claims;


public static class ClaimsPrincipalExtensions
{
    public static DateTime? GetTokenExpiration(this ClaimsPrincipal user)
    {
        // Attempt to find the "exp" claim in the user's claims
        var expClaim = user.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp);

        if (expClaim != null && long.TryParse(expClaim.Value, out long expValue))
        {
            // The exp claim is in seconds since epoch (Unix time),
            // so convert it to a DateTime
            var expiration = DateTimeOffset.FromUnixTimeSeconds(expValue).DateTime;
            return expiration;
        }

        // If there's no "exp" claim, return null
        return null;
    }

    public static string GetAccessToken(this ClaimsPrincipal user)
    {
        return user.FindFirst(SystemClaim.AccessTokenClaim)?.Value ?? string.Empty;
    }

    public static string GetTokenId(this ClaimsPrincipal user)
    {
        return user.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;
    }

    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var str = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        return str.IsPresent() ? Guid.TryParse(str, out var val) ? val : null : null;
    }

    public static string GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
    }

    public static string GetUserName(this ClaimsPrincipal user)
    {
        return user.FindFirst(SystemClaim.FullName)?.Value ?? string.Empty;
    }

    public static string GetSecurityStamp(this ClaimsPrincipal user)
    {
        return user.FindFirst(SystemClaim.SecurityStampClaim)?.Value ?? string.Empty;
    }
}
