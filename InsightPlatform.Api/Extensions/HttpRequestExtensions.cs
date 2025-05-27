using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using UAParser;

public static class HttpRequestExtensions
{
    public const string XCountrySiteHeaderKey = "X-Country-site";
    public const string XTimeZoneOffsetHeaderKey = "X-TimeZone-Offset";
    public const string XLocaleCodeHeaderKey = "X-Locale-Code";

    public readonly static Parser UaParser = Parser.GetDefault();


    public static string GetClientOrigin(this HttpRequest request)
    {
        var origin = request.Headers.Origin.FirstOrDefault();

        if (origin.IsMissing())
        {
            var refererHeader = request.Headers["Referer"].FirstOrDefault();
            if (Uri.TryCreate(refererHeader, UriKind.Absolute, out var refererUri))
            {
                origin = $"{refererUri.Scheme}://{refererUri.Host}:{refererUri.Port}";
            }
        }

        return origin;
    }

    public static bool IsMobileRequest(this HttpRequest request)
    {
        var userAgent = request.Headers.UserAgent.ToString().ToLower();

        if (userAgent.Contains("iphone") || userAgent.Contains("android"))
        {
            return true;
        }

        var origin = request.Headers.Origin.ToString();

        if (origin.IsMissing() 
            || (!origin.StartsWith("http://", StringComparison.OrdinalIgnoreCase) 
               && !origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    public static UserAgentInfo GetUserAgentInfo(this HttpRequest request)
    {
        var userAgent = request.Headers.UserAgent.ToString();
        var referer = request.Headers.Referer.ToString();
        var src = request.Headers["Src"].ToString();
        var xFrom = request.Headers["X-From"].ToString();

        ClientInfo ua = UaParser.Parse(userAgent);

        return new UserAgentInfo
        {
            Device = ua.Device,
            OS = ua.OS,
            UA = ua.UA,
            RawUserAgent = userAgent,
            Referer = referer,
            Src = src,
            XFrom = xFrom
        };
    }


    public static int GetClientTimeZoneOffset(this HttpRequest request)
    {
        var str = request.Headers[XTimeZoneOffsetHeaderKey].FirstOrDefault();

        _ = int.TryParse(str, out var offset);

        return offset;
    }

    public static string GetBaseUrl(this HttpRequest request)
    {
        return $"{request.Scheme}://{request.Host}";
    }
    
    public static string GetClientIP(this HttpRequest request)
    {
        // Try to get the client IP address from the "X-Forwarded-For" header
        if (request.Headers.ContainsKey("X-Forwarded-For"))
        {
            var ip = request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (ip.IsPresent())
            {
                // In case of multiple IP addresses, take the first one
                var firstIp = ip.Split(',').FirstOrDefault();
                if (firstIp.IsPresent())
                {
                    return firstIp.Trim();
                }
            }
        }

        // Try to get the client IP address from the "X-Real-IP" header
        if (request.Headers.ContainsKey("X-Real-IP"))
        {
            var ip = request.Headers["X-Real-IP"].FirstOrDefault();
            if (ip.IsPresent())
            {
                return ip;
            }
        }

        // Fallback to the remote IP address
        return request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
