using System;
using System.Threading.Tasks;

public interface IPainBusiness
{
    Task<BaseResponse<dynamic>> InsertPain(PainDto dto);

    Task<BaseResponse<dynamic>> PainLabelingAsync(Guid painId);
}