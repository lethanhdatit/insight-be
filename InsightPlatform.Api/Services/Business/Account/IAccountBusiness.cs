using System;
using System.Threading.Tasks;

public interface IAccountBusiness
{
    Task<BaseResponse<dynamic>> InitGuest();

    Task<BaseResponse<dynamic>> GoogleLoginAsync(string idToken);

    Task<BaseResponse<dynamic>> FacebookLoginAsync(string accessToken);

    Task<BaseResponse<dynamic>> RegisterAsync(RegisterRequest payload);

    Task<BaseResponse<dynamic>> LoginAsync(LoginRequest payload);
}