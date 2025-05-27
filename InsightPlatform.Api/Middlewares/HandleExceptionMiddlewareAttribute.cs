using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

public class HandleExceptionMiddleware(ILogger<HandleExceptionMiddleware> logger, IWebHostEnvironment env) : ExceptionFilterAttribute
{
    private readonly ILogger<HandleExceptionMiddleware> _logger = logger;
    private readonly IWebHostEnvironment _env = env;

    public override void OnException(ExceptionContext context)
    {
        context.Result = context.Exception.ProcessResult(_logger, _env, out bool isHandled);
        context.ExceptionHandled = isHandled;
    }
}

