using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// === Configuration bindings ===
services.Configure<AppOptions>(configuration.GetSection(AppOptions.Path));
services.Configure<TokenSettings>(configuration.GetSection(TokenSettings.Path));
services.Configure<CorsWhiteListOptions>(configuration.GetSection(CorsWhiteListOptions.Path));
services.Configure<ExternalLoginSettings>(configuration.GetSection(ExternalLoginSettings.Path));
services.Configure<QueueMessagingSettings>(configuration.GetSection(QueueMessagingSettings.Path));
services.Configure<LocalizationOptions>(configuration.GetSection(LocalizationOptions.Path));
services.Configure<OpenAISettings>(configuration.GetSection(OpenAISettings.Path));

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
var cors = builder.Configuration.GetSection(CorsWhiteListOptions.Path).Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsWhiteListOptions.Policy,
        policy =>
        {
            policy.WithOrigins(cors)
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
services.AddSingleton<IOpenAiService, OpenAiService>();
services.AddScoped<IPainBusiness, PainBusiness>();
services.AddScoped<IAccountBusiness, AccountBusiness>();

// === Controller & Swagger ===
services.AddControllers(options =>
{
    options.Filters.Add(typeof(HandleExceptionMiddleware));
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

// === Build & Pipeline ===
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

var localization = app.Services.GetRequiredService<AppRequestLocalization>();
app.UseRequestLocalization(localization.GetRequestLocalizationOptions());

app.UseAuthentication();
app.UseAuthorization();

app.UseCors(CorsWhiteListOptions.Policy);

app.MapControllers();

app.Run();
