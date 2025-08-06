using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// === Configuration bindings ===
services.Configure<AppSettings>(configuration.GetSection(AppSettings.Path));
services.Configure<TokenSettings>(configuration.GetSection(TokenSettings.Path));
services.Configure<CorsWhiteListSettings>(configuration.GetSection(CorsWhiteListSettings.Path));
services.Configure<ExternalLoginSettings>(configuration.GetSection(ExternalLoginSettings.Path));
services.Configure<ExternalResourceSettings>(configuration.GetSection(ExternalResourceSettings.Path));
services.Configure<QueueMessagingSettings>(configuration.GetSection(QueueMessagingSettings.Path));
services.Configure<LocalizationSettings>(configuration.GetSection(LocalizationSettings.Path));
services.Configure<AISettings>(configuration.GetSection(AISettings.Path));
services.Configure<PaymentOptions>(configuration.GetSection(PaymentOptions.Path));
services.Configure<EmailProviderSettings>(configuration.GetSection(EmailProviderSettings.Path));

// === Infrastructure ===
services.AddHttpContextAccessor();
services.AddHttpClient<IHttpClientService, HttpClientService>();
services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
services.AddSingleton<RetryPolicy>();

// === JWT & Auth ===
services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();

// === Localization && CORS ===
services.AddSingleton<AppRequestLocalization>();

var corsSettings = configuration.GetSection(CorsWhiteListSettings.Path).Get<CorsWhiteListSettings>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsWhiteListSettings.Policy,
        policy =>
        {
            policy.WithOrigins([.. corsSettings.Origins])
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .AllowAnyMethod()
                  .SetPreflightMaxAge(TimeSpan.FromHours(1));
        });
});

// === Messaging ===
services.AddSingleton<IQueueMessaging, RabbitMqService>();
services.AddSingleton<PainPublisher>();
services.AddSingleton<PainConsumer>();
services.AddHostedService<ConsumerInitializer>();

// === Business logic ===
services.AddSingleton<IEmailService, EmailService>(); 
services.AddSingleton<IOpenAiService, OpenAiService>();
services.AddSingleton<IGeminiAIService, GeminiAIService>();
services.AddSingleton<IPhongThuyNhanSinhService, PhongThuyNhanSinhService>();
services.AddScoped<IPainBusiness, PainBusiness>();
services.AddScoped<ILuckyNumberBusiness, LuckyNumberBusiness>();
services.AddScoped<IBocMenhBusiness, BocMenhBusiness>();
services.AddScoped<IAccountBusiness, AccountBusiness>();
services.AddScoped<IInitBusiness, InitBusiness>();
services.AddScoped<ITransactionBusiness, TransactionBusiness>();
services.AddScoped<IVietQRService, VietQRService>();
services.AddScoped<IPayPalService, PayPalService>();
services.AddScoped<ICurrencyService, CurrencyService>();
services.AddScoped<IAffiliateBusiness, AffiliateBusiness>();
services.AddScoped<IAffiliateInitBusiness, AffiliateInitBusiness>();

// === Controller & Swagger ===
services.AddControllers(options =>
{
    options.Filters.Add(typeof(HandleExceptionMiddleware));
}).AddJsonOptions(options =>
{
    // Configure options to ignore null properties
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    foreach (var item in SystemSerialization.JsonConverters)
        options.JsonSerializerOptions.Converters.Add(item);
});
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "InsightPlatform API", Version = "v1" });

    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer {token}' (without quotes)",
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            securityScheme,
            Array.Empty<string>()
        }
    });
});
services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");
app.MapGet("/", (HttpContext context) =>
{
    context.Response.Redirect("/health");
    return Task.CompletedTask;
});
app.UseHttpsRedirection();

var localization = app.Services.GetRequiredService<AppRequestLocalization>();
app.UseRequestLocalization(localization.GetRequestLocalizationOptions());

app.Use(async (context, next) =>
{
    var origin = context.Request.Headers.Origin.ToString();

    if (origin.IsPresent()
      && !corsSettings.Origins.Any(a => a.Trim().Trim('/').Equals(origin.Trim().Trim('/'), StringComparison.OrdinalIgnoreCase)))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new BaseResponse<object>(null, "Origin not allowed."));
        return;
    }

    var rqkey = context.Request.GetApiKey();
    var path = context.Request.Path.ToString().ToLowerInvariant();
    var excludedPaths = new List<string>
    {
        "/",
        "/health",
        "/api/transaction/paypal/webhook",
        "/api/transaction/vqr/ipn/api/token_generate",
        "/api/transaction/vqr/ipn/bank/api/transaction-sync",
    };

    if (!excludedPaths.Contains(path) && rqkey != corsSettings.Key)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new BaseResponse<object>(null, "Invalid API key."));
        return;
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseCors(CorsWhiteListSettings.Policy);

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    using var dbContext = dbContextFactory.CreateDbContext();
    dbContext.Database.Migrate();

    var initBusiness = scope.ServiceProvider.GetRequiredService<IInitBusiness>();
    await initBusiness.InitServicePrices();
    await initBusiness.InitTopUpPackages();
}

app.Run();
