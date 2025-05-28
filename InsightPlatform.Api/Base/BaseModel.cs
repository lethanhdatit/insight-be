using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using UAParser;


public class BaseAppRequest
{
    public string BaseRequest { get; protected set; }
        
    public string ClientIP { get; protected set; }
        
    public int TimeZoneOffset { get; protected set; }
        
    public string ClientOrigin { get; protected set; }
        
    public Guid? UserId { get; protected set; }

    public string UserName { get; protected set; }
        
    public string Email { get; protected set; }
        
    public bool IsAuthenticated => UserId != null && UserId != Guid.Empty;
        
    public string SecurityStamp { get; protected set; }
        
    public string AccessToken { get; protected set; }
        
    public string AccessTokenId { get; protected set; }
        
    public DateTime? AccessTokenExpiration { get; protected set; }
    
    public bool IsMobileRequest { get; protected set; }

    public CultureInfo CurrentCulture { get; protected set; }

    public UserAgentInfo UA { get; protected set; }

    public BaseAppRequest(HttpRequest request)
    {
        GetMetadata(request);
    }

    private void GetMetadata(HttpRequest request)
    {
        if (request != null)
        {
            var current = request.HttpContext.User;

            BaseRequest = request.GetBaseUrl();
            ClientOrigin = request.GetClientOrigin();
            ClientIP = request.GetClientIP();
            TimeZoneOffset = request.GetClientTimeZoneOffset();
            CurrentCulture = CultureInfo.CurrentCulture;
            UserId = current.GetUserId();
            UserName = current.GetUserName();
            Email = current.GetEmail();
            SecurityStamp = current.GetSecurityStamp();
            AccessToken = current.GetAccessToken();
            AccessTokenId = current.GetTokenId();
            AccessTokenExpiration = current.GetTokenExpiration();
            IsMobileRequest = request.IsMobileRequest();
            UA = request.GetUserAgentInfo();
        }       
    }
}


public class BaseResponse<T>
{
    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("data")]
    public T Data { get; set; }

    [JsonPropertyName("ts")]
    public string Timestamp => DateTime.UtcNow.ToString();

    public BaseResponse() { }

    public BaseResponse(T data, string message = "OK")
    {
        Data = data;
        Message = message;
    }
}


public class BusinessErrorItem
{
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("metaData")]
    public object MetaData { get; set; }
}

public class BusinessError
{
    public List<BusinessErrorItem> Data { get; set; }

    public BusinessError(BusinessErrorItem error)
    {
        Data = [error];
    }

    public BusinessError(List<BusinessErrorItem> errors)
    {
        Data = errors;
    }

    public BusinessError(IEnumerable<IdentityError> errors)
    {
        Data = errors.Select(s => new BusinessErrorItem
        {
            Code = s.Code,
            Description = s.Description
        })
        .ToList();
    }

    public BusinessError(ModelStateDictionary modelState)
    {
        Data = modelState
               .Where(e => e.Value.ValidationState == ModelValidationState.Invalid)
               .ToDictionary(k => k.Key, v => v.Value.Errors.Select(s => s.ErrorMessage))
               .Where(s => s.Key != "$")
               .Select(s => new BusinessErrorItem
               {
                   Code = s.Key,
                   Description = string.Join(" ", s.Value)
               })
               .ToList();
    }
}

public class BusinessException : Exception
{
    public const string UnexpectedError = "UnexpectedBusinessError";

    public BusinessError Errors { get; set; }

    public string ErrorCode { get; private set; }

    public string SourceName { get; private set; }

    public BusinessException(string errorCode = UnexpectedError
        , string message = "Unexpected error"
        , Exception innerException = default
        , [CallerMemberName] string memberName = ""
        , [CallerFilePath] string filePath = ""
        , object metaData = null) : base(message, innerException)
    {
        ErrorCode = errorCode;

        Errors = new BusinessError(new BusinessErrorItem { Code = errorCode, Description = message, MetaData = metaData });

        SourceName = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}";
    }

    public BusinessException(BusinessError errors
        , string message = null
        , Exception innerException = default
        , [CallerMemberName] string memberName = ""
        , [CallerFilePath] string filePath = "") : base(message, innerException)
    {
        Errors = errors;
        SourceName = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}";
    }

    public BusinessException(BusinessErrorItem error
        , string message = null
        , Exception innerException = default
        , [CallerMemberName] string memberName = ""
        , [CallerFilePath] string filePath = "") : base(message, innerException)
    {
        Errors = new BusinessError(error);
        SourceName = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}";
    }

    public BusinessException(List<BusinessErrorItem> errors
        , string message = null
        , Exception innerException = default
        , [CallerMemberName] string memberName = ""
        , [CallerFilePath] string filePath = "") : base(message, innerException)
    {
        Errors = new BusinessError(errors);
        SourceName = $"{Path.GetFileNameWithoutExtension(filePath)}.{memberName}";
    }
}

public class UserAgentInfo
{
    public Device Device { get; set; }

    public OS OS { get; set; }

    public UserAgent UA { get; set; }

    public string RawUserAgent { get; set; }

    public string Referer { get; set; }

    public string Src { get; set; }

    public string XFrom { get; set; }
}
