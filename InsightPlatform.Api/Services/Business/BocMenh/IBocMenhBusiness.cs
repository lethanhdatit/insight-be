using System;
using System.Threading.Tasks;

public interface IBocMenhBusiness
{  
    Task<BaseResponse<dynamic>> InitTuTruBatTuAsync(TuTruBatTuRequest request);

    Task<BaseResponse<TheologyBaseResult<string, string>>> ExplainTuTruBatTuAsync(Guid id, int retry);

    Task<BaseResponse<int>> PaidTheologyRecordAsync(Guid id);

    Task<BaseResponse<dynamic>> GetTuTruBatTuAsync(Guid id);
}