using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

public interface ILuckyNumberBusiness
{
    Task<BaseResponse<dynamic>> ImportCrawledDataAsync(IFormFile file
       , LuckyNumberProviderDto provider
        , bool isOverride
        , string crossCheckProviderName = null);

    Task<BaseResponse<dynamic>> GetHistoricalSequencesAsync(string prizeType, int yearsBack = 5);

    Task<BaseResponse<dynamic>> GetHistoricalPrizetypeFlatAsync(string fromDate);

    Task<BaseResponse<dynamic>> BuildCrawledDataAsync(int? yearsBack = null, bool isOverride = false);

    Task<BaseResponse<dynamic>> TheologyAndNumbersAsync(TheologyRequest request);
}