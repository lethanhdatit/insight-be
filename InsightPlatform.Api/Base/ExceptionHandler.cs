using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

public static class ExceptionHandler
{
    public static ObjectResult ProcessResult(this Exception exception, ILogger logger, IWebHostEnvironment env, out bool isHandled)
    {
        var statusCode = HttpStatusCode.BadRequest;
        string message = exception.Message;
        object data = null;
        isHandled = true;

        switch (exception)
        {
            case BusinessException ex:
                {
                    //if (!env.IsProduction())
                    //{
                    //    message = AddInnerExceptionMessage(ex, message);
                    //    message += AddSourceMessage(ex.Source, message);
                    //}

                    message = AddInnerExceptionMessage(ex, message);
                    message += AddSourceMessage(ex.Source, message);

                    data = ex.Errors?.Data;
                }
                break;
            default:
                {
                    statusCode = HttpStatusCode.InternalServerError;
                    //message = env.IsProduction() ? "Unexpected error in backend side" : message;

                    //if (!env.IsProduction())
                    //{
                    //    message = AddInnerExceptionMessage(exception, message);
                    //    message += AddSourceMessage(exception.Source, message);
                    //}

                    message = AddInnerExceptionMessage(exception, message);
                    message += AddSourceMessage(exception.Source, message);

                    isHandled = false;
                }
                break;
        }

        logger.LogError(exception, message);

        return new ObjectResult(new BaseResponse<object>(data, message))
        {
            StatusCode = (int)statusCode
        };
    }

    private static string AddSourceMessage(string source, string message)
    {
        return $"{(message.IsPresent() ? ", " : string.Empty)}Non-Production trace: {source}";
    }

    private static string AddInnerExceptionMessage(Exception ex, string initMessage = null)
    {
        string message = initMessage ?? ex.Message;
        if (ex.InnerException != null)
        {
            message += $", {AddInnerExceptionMessage(ex.InnerException)}";
        }
        return message;
    }
}
