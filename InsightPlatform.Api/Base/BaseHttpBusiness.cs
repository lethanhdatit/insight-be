using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class BaseHttpBusiness<TBusiness, TContext>(ILogger<TBusiness> logger
        , IDbContextFactory<TContext> contextFactory
        , IHttpContextAccessor contextAccessor) where TContext : DbContext
{
    protected readonly ILogger<TBusiness> _logger = logger;
    protected readonly IDbContextFactory<TContext> _contextFactory = contextFactory;
    protected readonly IHttpContextAccessor _contextAccessor = contextAccessor;

    protected readonly BaseAuthorizedRequest Current = new(contextAccessor?.HttpContext?.Request);

    protected HttpContext CurrentContext()
    {
        return _contextAccessor?.HttpContext;
    }

    protected ClaimsPrincipal CurrentUser()
    {
        return CurrentContext()?.User;
    }

    protected HttpRequest CurrentRequest()
    {
        return CurrentContext()?.Request;
    }
}
