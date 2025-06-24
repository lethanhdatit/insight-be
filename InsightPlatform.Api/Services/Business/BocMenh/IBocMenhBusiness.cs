using System;
using System.Threading.Tasks;

public interface IBocMenhBusiness
{
    Task<BaseResponse<dynamic>> TheologyAndNumbersAsync(TheologyRequest request);

    Task<BaseResponse<dynamic>> GetTheologyAndNumbersAsync(Guid id);

    BaseResponse<dynamic> GetVietnameseCalendar(DateTime solarDate, bool includeMonthDetail);
}