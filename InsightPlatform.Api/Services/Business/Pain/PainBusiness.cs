using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class PainBusiness(ILogger<PainBusiness> logger
    , IDbContextFactory<ApplicationDbContext> contextFactory
    , IHttpContextAccessor contextAccessor
    , IAccountBusiness accountBusiness
    , IOpenAiService openAiService
    , IOptions<AppSettings> appOptions
    , PainPublisher publisher) : BaseHttpBusiness<PainBusiness, ApplicationDbContext>(logger, contextFactory, contextAccessor), IPainBusiness
{
    private readonly PainPublisher _publisher = publisher;
    private readonly AppSettings _appSettings = appOptions.Value;
    private readonly IAccountBusiness _accountBusiness = accountBusiness;
    private readonly IOpenAiService _openAiService = openAiService;

    public async Task<BaseResponse<dynamic>> InsertPain(PainDto dto)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var userId = Current.UserId;

            if (userId == null)
                userId = (await _accountBusiness.InitGuest()).Data;

            if (userId == null)
                throw new BusinessException("UnableInitGuest", "Unable to init guest user");

            var ua = Current.UA?.RawUserAgent;

            var entity = new Pain
            {
                PainDetail = dto.Pain,
                Desire = dto.Desire,
                UserId = userId,
                UserAgent = ua,
                ClientLocale = Current.CurrentCulture?.Name
            };

            await context.Pains.AddAsync(entity);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _publisher.PainLabelingAsync(entity.Id);

            return new(new
            {
                TrackLink = _appSettings.FeDomain.WithPath($"track/{entity.Id}")
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            await transaction.DisposeAsync();
            await context.DisposeAsync();
        }
    } 
    
    public async Task<BaseResponse<dynamic>> PainLabelingAsync(Guid painId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var entity = await context.Pains.FirstOrDefaultAsync(f => f.Id == painId);

            if (entity != null)
            {
                var res = await ClassifyPainAsync(entity);

                if (res != null)
                {
                    entity.Category = res.Category;
                    entity.UrgencyLevel = res.UrgencyLevel;
                    entity.Emotion = res.Emotion;
                    entity.Topic = res.Topic;

                    context.Pains.Update(entity);
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new(true);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            await transaction.DisposeAsync();
            await context.DisposeAsync();
        }
    }

    private async Task<PainClassificationResult> ClassifyPainAsync(Pain pain)
    {
        var detectedLanguage = await DetectLanguageAsync(pain);

        var (systemPrompt, userPrompt) = BuildPainClassifyPrompt(pain, detectedLanguage);
        
        var result = await _openAiService.SendChatAsync(systemPrompt, userPrompt);

        if (result.IsPresent())
        {
            result = result.Replace("```json", string.Empty);
        }

        return JsonSerializer.Deserialize<PainClassificationResult>(result);
    }

    private async Task<string> DetectLanguageAsync(Pain pain)
    {
        var combined = $"{pain.PainDetail}\n{pain.Desire}";
        var systemPrompt = "Detect the primary human language of the input text. Just return the ISO language code (e.g. en, vi, ko, zh). No explanation.";
        var detectedLang = await _openAiService.SendChatAsync(systemPrompt, combined);
        return Regex.IsMatch(detectedLang.Trim(), "^[a-z]{2}$") ? detectedLang.Trim().ToLower() : "en";
    }

    private static (string systemPrompt, string userPrompt) BuildPainClassifyPrompt(Pain pain, string detectedLanguage)
    {
        var systemPrompt = "You are a classification expert analyzing emotional and behavioral inputs.";

        var sb = new StringBuilder();

        sb.AppendLine("You are an assistant that analyzes user pain points and desires. Your task is to classify the pain by category, topic, emotion, and urgency level.");
        sb.AppendLine("Return results in English, regardless of the language used by the user.");
        sb.AppendLine();

        sb.AppendLine("User Context:");
        sb.AppendLine($"- ClientLocale: {pain.ClientLocale ?? "unknown"}");
        sb.AppendLine($"- UserAgent: {pain.UserAgent ?? "unknown"}");
        sb.AppendLine($"- DetectedLanguage: {detectedLanguage ?? "unknown"}");
        sb.AppendLine();

        sb.AppendLine("User Input:");
        sb.AppendLine($"PainDetail: {pain.PainDetail}");
        sb.AppendLine($"Desire: {pain.Desire}");
        sb.AppendLine();

        sb.AppendLine("Please analyze the input and return the result as a JSON string that can be easy to deserialize without any non-json characters. Following structure:");
        sb.AppendLine(@"
        {
          ""category"": ""string"",
          ""topic"": ""string"",
          ""emotion"": ""string"",
          ""urgencyLevel"": number (1 to 5)
        }");

        sb.AppendLine();
        sb.AppendLine("Use the context information to better understand regional, cultural, or linguistic nuances, but again, return the final result in English.");

        return (systemPrompt, sb.ToString());
    }

}

public record PainDto(string Pain, string Desire);