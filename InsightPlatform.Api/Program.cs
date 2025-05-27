using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.Path));
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection(TokenSettings.Path));
builder.Services.Configure<ExternalLoginSettings>(builder.Configuration.GetSection(ExternalLoginSettings.Path));
builder.Services.Configure<QueueMessagingSettings>(builder.Configuration.GetSection(QueueMessagingSettings.Path));
builder.Services.Configure<LocalizationOptions>(builder.Configuration.GetSection(LocalizationOptions.Path));
builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection(OpenAISettings.Path));

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<IHttpClientService, HttpClientService>();

builder.Services.AddSingleton<AppRequestLocalization>();

builder.Services.AddControllers(options =>
{
    options.Filters.Add(typeof(HandleExceptionMiddleware));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<RetryPolicy>();
builder.Services.AddSingleton<IOpenAiService, OpenAiService>();
builder.Services.AddSingleton<IQueueMessaging, RabbitMqService>();
builder.Services.AddSingleton<PainPublisher>();
builder.Services.AddSingleton<PainConsumer>();
builder.Services.AddHostedService<ConsumerInitializer>();

builder.Services.AddScoped<IPainBusiness, PainBusiness>();
builder.Services.AddScoped<IAccountBusiness, AccountBusiness>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

var localization = app.Services.GetRequiredService<AppRequestLocalization>();
app.UseRequestLocalization(localization.GetRequestLocalizationOptions());

app.UseAuthorization();

app.MapControllers();

app.Run();