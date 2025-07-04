using System;
using System.Threading.Tasks;

public interface ITransactionBusiness
{
    Task<BaseResponse<dynamic>> GetTopupsAsync();

    Task<BaseResponse<dynamic>> BuyTopupAsync(BuyTopupRequest request);

    Task<BaseResponse<dynamic>> CheckStatusAsync(Guid id);

    Task<BaseResponse<int>> GetUserFates();

    Task<BaseResponse<dynamic>> CancelAsync(Guid id);

    Task<int> RecalculateUserFates(Guid userId);

    Task<BaseResponse<Guid>> VietQrCallbackAsync(TransactionCallback request);
}