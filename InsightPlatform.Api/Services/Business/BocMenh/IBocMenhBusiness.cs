using System;
using System.Threading.Tasks;

public interface IBocMenhBusiness
{  
    Task<BaseResponse<dynamic>> InitTuTruBatTuAsync(TuTruBatTuRequest request);

    Task<BaseResponse<bool>> ExplainTuTruBatTuAsync(Guid id);

    Task<BaseResponse<dynamic>> ExplainTuTruBatTuTestAsync(TuTruBatTuRequest request);    

    Task<BaseResponse<dynamic>> GetTuTruBatTuAsync(Guid id);
}