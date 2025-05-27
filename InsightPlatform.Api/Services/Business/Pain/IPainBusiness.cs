using System.Threading.Tasks;

public interface IPainBusiness
{
    Task<BaseResponse<dynamic>> InsertPain(PainDto dto);
}