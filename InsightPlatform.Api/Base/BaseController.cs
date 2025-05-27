using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

public class BaseController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger _logger;

    public BaseController(IWebHostEnvironment env, ILogger logger)
    {
        _env = env;
        _logger = logger;
    }

    [NonAction]
    public ObjectResult HandleOk<T>(BaseResponse<T> response) => new ObjectResult(response) { StatusCode = (int)HttpStatusCode.OK };

    [NonAction]
    public ObjectResult HandleException(Exception exception) => exception.ProcessResult(_logger, _env, out _);
}