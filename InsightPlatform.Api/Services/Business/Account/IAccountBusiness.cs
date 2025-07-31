using System;
using System.Threading.Tasks;

public interface IAccountBusiness
{
    Task<BaseResponse<dynamic>> InitGuest();

    Task<BaseResponse<dynamic>> GoogleLoginAsync(string idToken);

    Task<BaseResponse<dynamic>> FacebookLoginAsync(string accessToken);

    Task<BaseResponse<dynamic>> RegisterAsync(RegisterRequest payload);

    Task<BaseResponse<dynamic>> LoginAsync(LoginRequest payload);

    Task<BaseResponse<dynamic>> GetMeAsync();

    Task<BaseResponse<dynamic>> UpdateMeAsync(UpdateRequest request);

    Task<BaseResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request);

    Task<BaseResponse<bool>> SendEmailOtpForRegisterAsync(SendEmailVerifyRequest input);

    Task<BaseResponse<bool>> ConfirmEmailOtpForRegisterAsync(ConfirmEmailVerifyRequest input);

    Task<BaseResponse<bool>> SendEmailOtpForPasswordRecoveryAsync(SendEmailVerifyRequest input);

    Task<BaseResponse<bool>> ConfirmEmailOtpForPasswordRecoveryAsync(ConfirmEmailVerifyRequest request);

    (string token, DateTime? expiration) GenerateAccessTokenForPaymentGate(TimeSpan exp, GateConnectionOptions gateConnection);
}