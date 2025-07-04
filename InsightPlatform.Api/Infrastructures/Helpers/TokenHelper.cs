using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public static class TokenHelper
{
    public static (string token, DateTime? expiration) GetToken(TokenSettings appSettings, List<Claim> userClaims, bool isRememberMe = false)
    {
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.SecretKey));
        var signInCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        var expInt = isRememberMe ? appSettings.AccessTokenRememberExpSeconds : appSettings.AccessTokenExpSeconds;
        var expiration = expInt >= 0 ? DateTime.UtcNow.Add(TimeSpan.FromSeconds(expInt)) : (DateTime?)null;

        var tokenOptions = new JwtSecurityToken(
            issuer: appSettings.Issuer,
            audience: appSettings.Audience,
            claims: userClaims,
            expires: expiration,
            signingCredentials: signInCredentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

        return (token, expiration);
    }

    public static (string token, DateTime? expiration) GetToken(TokenSettings appSettings, List<Claim> userClaims, TimeSpan exp)
    {
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.SecretKey));
        var signInCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        var expiration = DateTime.UtcNow.Add(exp);

        var tokenOptions = new JwtSecurityToken(
            issuer: appSettings.Issuer,
            audience: appSettings.Audience,
            claims: userClaims,
            expires: expiration,
            signingCredentials: signInCredentials
        );

        var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

        return (token, expiration);
    }

    public static ClaimsPrincipal GetPrincipalFromExpiredToken(TokenSettings appSettings, string token)
    {
        try
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.SecretKey))
            };

            var principal = new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
        catch (Exception)
        {
            return null;
        }
    }
}