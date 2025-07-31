using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

public class AccountBusiness(ILogger<AccountBusiness> logger
    , IDbContextFactory<ApplicationDbContext> contextFactory
    , IHttpContextAccessor contextAccessor
    , IHttpClientService httpClientService
    , IEmailService mailService
    , IOptions<TokenSettings> tokenSettings
    , IOptions<ExternalLoginSettings> externalLoginSettings) : BaseHttpBusiness<AccountBusiness, ApplicationDbContext>(logger, contextFactory, contextAccessor), IAccountBusiness
{
    private readonly TokenSettings _tokenSettings = tokenSettings.Value;
    private readonly ExternalLoginSettings _externalLoginSettings = externalLoginSettings.Value;
    private readonly IHttpClientService _httpClientService = httpClientService;
    private readonly IEmailService _mailService = mailService;
    private readonly int ResendEmailOtpInSeconds = 120;
    private readonly int EmailOtpForRegisterExpInSeconds = 300;
    private readonly int EmailOtpForPasswordRecoveryExpInSeconds = 600;
    public const string InsightSystemConst = "InsightSystem";

    public async Task<BaseResponse<dynamic>> InitGuest()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var guestUserId = Current.UserId;
            User user = null;

            if (guestUserId.HasValue)
            {
                user = await context.Users.FirstOrDefaultAsync(u => u.Id == guestUserId);
            }

            if (user == null)
            {
                user = new User
                {
                    UserAgent = Current.UA?.RawUserAgent,
                    ClientLocale = Current.CurrentCulture?.Name,
                    CreatedAt = DateTime.UtcNow,
                };

                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            var (accessToken, expiration) = GenerateAccessTokenFromUser(user, true);
            var isGuest = user.PasswordSalt.IsMissing() 
                       && user.GoogleId == null 
                       && user.FacebookId == null;

            return new(new
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Token = accessToken,
                Expiration = expiration,
                Username = user.Username,
                IsGuest = isGuest
            });
        }
        catch(Exception e)
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

    public async Task<BaseResponse<dynamic>> GoogleLoginAsync(string idToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _externalLoginSettings.Google.ClientId }
            });

            if (payload == null)
                throw new BusinessException("InvalidGoogleToken", "Invalid Google token");

            var guestUserId = Current.UserId;

            var guestUser = await context.Users.FirstOrDefaultAsync(u => u.Id == guestUserId);
            var ggUser = await context.Users.FirstOrDefaultAsync(u => u.GoogleId == payload.Subject);

            User user = null;

            if (guestUser == null && ggUser == null)
            {
                ggUser = new User
                {
                    UserAgent = Current.UA?.RawUserAgent,
                    ClientLocale = Current.CurrentCulture?.Name,
                    CreatedAt = DateTime.UtcNow,
                    GoogleId = payload.Subject,
                    DisplayName = payload.Name,
                };

                await context.Users.AddAsync(ggUser);
                await context.SaveChangesAsync();

                user = ggUser;
            }
            else if (guestUser != null && ggUser == null)
            {
                if(guestUser.Username == null
                    && guestUser.GoogleId == null
                    && guestUser.FacebookId == null)
                {
                    guestUser.GoogleId = payload.Subject;
                    guestUser.DisplayName = payload.Name;

                    context.Users.Update(guestUser);
                    await context.SaveChangesAsync();

                    user = guestUser;
                }
            }
            else
            {
                user = ggUser;
            }

            await transaction.CommitAsync();

            var (accessToken, expiration) = GenerateAccessTokenFromUser(user, true);
            var username = user.Username;

            var isGuest = user.PasswordSalt.IsMissing()
                       && user.GoogleId.IsMissing()
                       && user.FacebookId.IsMissing();

            return new(new
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Token = accessToken,
                Expiration = expiration,
                Username = username,
                IsGuest = isGuest
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

    public async Task<BaseResponse<dynamic>> FacebookLoginAsync(string token)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var payload = await ValidateFacebookTokenAsync(token);

            if (payload == null)
                throw new BusinessException("InvalidFacebookToken", "Invalid Facebook token");

            var guestUserId = Current.UserId;

            var guestUser = await context.Users.FirstOrDefaultAsync(u => u.Id == guestUserId);
            var fbUser = await context.Users.FirstOrDefaultAsync(u => u.FacebookId == payload.Id);

            User user = null;

            if (guestUser == null && fbUser == null)
            {
                fbUser = new User
                {
                    UserAgent = Current.UA?.RawUserAgent,
                    ClientLocale = Current.CurrentCulture?.Name,
                    CreatedAt = DateTime.UtcNow,
                    FacebookId = payload.Id,
                    DisplayName = payload.Name,
                };

                await context.Users.AddAsync(fbUser);
                await context.SaveChangesAsync();

                user = fbUser;
            }
            else if (guestUser != null && fbUser == null)
            {
                if(guestUser.Username == null
                    && guestUser.GoogleId == null
                    && guestUser.FacebookId == null)
                {
                    guestUser.FacebookId = payload.Id;
                    guestUser.DisplayName = payload.Name;

                    context.Users.Update(guestUser);
                    await context.SaveChangesAsync();

                    user = guestUser;
                }
            }
            else
            {
                user = fbUser;
            }

            await transaction.CommitAsync();

            var (accessToken, expiration) = GenerateAccessTokenFromUser(user, true);
            var username = user.Username;

            var isGuest = user.PasswordSalt.IsMissing()
                       && user.GoogleId.IsMissing()
                       && user.FacebookId.IsMissing();

            return new(new
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Token = accessToken,
                Expiration = expiration,
                Username = username,
                IsGuest = isGuest
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

    public async Task<BaseResponse<dynamic>> RegisterAsync(RegisterRequest payload)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            if (payload.Username.IsMissing()
                || payload.Password.IsMissing())
                throw new BusinessException("InvalidPayload", "Payload invalid");

            payload.Username = payload.Username.Trim();
            payload.Password = payload.Password.Trim();

            await VerifyEmailAsync(Modules.BocMenh
                , EntityOtpType.EmailOtpForRegister
                , EmailOtpForRegisterExpInSeconds
                , payload.Username);

            var user = await context.Users.FirstOrDefaultAsync(f => f.FacebookId == null
                                                                 && f.GoogleId == null
                                                                 && f.Username.ToLower() == payload.Username.ToLower());

            if (user != null)
            {
                throw new BusinessException("ExistedUsername", "Username existed");
            }

            var guestUserId = Current.UserId;

            var guestUser = await context.Users.FirstOrDefaultAsync(u => u.Id == guestUserId);

            PasswordHelper.CreatePasswordHash(payload.Password, out var passwordHash, out var passwordSalt);

            if (guestUser != null && guestUser.Username == null && guestUser.FacebookId == null && guestUser.GoogleId == null)
            {
                guestUser.Username = payload.Username;
                guestUser.Email = payload.Username;
                guestUser.DisplayName = payload.DisplayName?.Trim();
                guestUser.PasswordHash = passwordHash;
                guestUser.PasswordSalt = passwordSalt;

                context.Users.Update(guestUser);
                await context.SaveChangesAsync();

                user = guestUser;
            }
            else
            {
                user = new User
                {
                    UserAgent = Current.UA?.RawUserAgent,
                    ClientLocale = Current.CurrentCulture?.Name,
                    CreatedAt = DateTime.UtcNow,
                    Username = payload.Username,
                    Email = payload.Username,
                    DisplayName = payload.DisplayName,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt
                };

                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();
            }
           
            await transaction.CommitAsync();

            var (accessToken, expiration) = GenerateAccessTokenFromUser(user, false);
            var username = user.Username;

            var isGuest = user.PasswordSalt.IsMissing()
                       && user.GoogleId.IsMissing()
                       && user.FacebookId.IsMissing();

            return new(new
            {
                Id = user.Id,
                Email = user.Email ?? username,
                DisplayName = user.DisplayName,
                Token = accessToken,
                Expiration = expiration,
                Username = username,
                IsGuest = isGuest
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

    public async Task<BaseResponse<dynamic>> LoginAsync(LoginRequest payload)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (payload.Username.IsMissing() || payload.Password.IsMissing())
            throw new BusinessException("InvalidPayload", "Payload invalid");

        var user = await context.Users.FirstOrDefaultAsync(u => u.FacebookId == null
                                                             && u.GoogleId == null
                                                             && u.Username.ToLower() == payload.Username.ToLower());

        if (user == null || !PasswordHelper.VerifyPassword(payload.Password, user.PasswordHash, user.PasswordSalt))
            throw new BusinessException("InvalidCredentials", "Username or password incorrect");

        var (accessToken, expiration) = GenerateAccessTokenFromUser(user, payload.RememberMe);
        var username = user.Username;

        var isGuest = user.PasswordSalt.IsMissing()
                   && user.GoogleId.IsMissing()
                   && user.FacebookId.IsMissing();

        return new(new
        {
            Id = user.Id,
            Email = user.Email ?? username,
            DisplayName = user.DisplayName,
            Token = accessToken,
            Expiration = expiration,
            Username = username,
            IsGuest = isGuest
        });
    }
    
    public async Task<BaseResponse<dynamic>> GetMeAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users.AsNoTracking()
                                      .FirstOrDefaultAsync(u => u.Id == Current.UserId);

        if(user == null)
            throw new BusinessException("UserNotFound", "User not found");

        return new(new
        {
            Id = user.Id,
            Email = user.Email ?? user.Username,
            Name = user.DisplayName,
            Avatar = string.Empty,
            CreatedTs = user.CreatedAt,
            TotalDays = (DateTime.UtcNow - user.CreatedAt).Days,
            CurrentFates = user.Fates
        });
    }

    public async Task<BaseResponse<dynamic>> UpdateMeAsync(UpdateRequest request)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users.AsNoTracking()
                                      .FirstOrDefaultAsync(u => u.Id == Current.UserId);

        if (user == null)
            throw new BusinessException("UserNotFound", "User not found");

        bool needLogout = false;

        if (request.DisplayName.IsPresent())
        {
            user.DisplayName = request.DisplayName.Trim();
            context.Users.Update(user);
            await context.SaveChangesAsync();
        }

        if(request.Password.IsPresent() && request.NewPassword.IsPresent())
        {
            if (!PasswordHelper.VerifyPassword(request.Password.Trim(), user.PasswordHash, user.PasswordSalt))
                throw new BusinessException("InvalidCredentials", "Current password is incorrect");
            PasswordHelper.CreatePasswordHash(request.NewPassword.Trim(), out var passwordHash, out var passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            context.Users.Update(user);
            await context.SaveChangesAsync();

            needLogout = true;
        }

        return new(new
        {
            DisplayName = user.DisplayName,
            Avatar = "",
            NeedLogout = needLogout
        });
    }

    public async Task<BaseResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.Password.IsMissing())
            throw new BusinessException("MissingPassword", "Missing password");

        if (request.Email.IsMissing())
            throw new BusinessException("MissingEmail", "Missing email");

        request.Email = request.Email?.Trim();
        request.Password = request.Password?.Trim();

        await VerifyEmailAsync(Modules.BocMenh
                , EntityOtpType.EmailOtpForPasswordRecovery
                , EmailOtpForPasswordRecoveryExpInSeconds
                , request.Email);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users.AsNoTracking()
                                      .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower()
                                                             && u.DeletedTs == null);

        if (user == null)
            throw new BusinessException("UserNotFound", "User not found");

        PasswordHelper.CreatePasswordHash(request.Password, out var passwordHash, out var passwordSalt);

        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;
        context.Users.Update(user);
        await context.SaveChangesAsync();

        return new(true);
    }

    public async Task<BaseResponse<bool>> SendEmailOtpForRegisterAsync(SendEmailVerifyRequest input)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (input.Email.IsMissing())
            throw new BusinessException("InvalidPayload", "Email is required");

        input.Email= input.Email.Trim();

        if (await context.Users.AnyAsync(u => u.Username.ToLower() == input.Email.ToLower() 
                                           && u.DeletedTs == null))
            throw new BusinessException("EmailAlreadyExists", "Email already exists");

        var otpType = EntityOtpType.EmailOtpForRegister;
        var emailTemplate = EmailTemplateEnum.EmailOtpForRegister;

        return await SendOtpByEmailAsync(context
            , input.Email
            , input.Module
            , otpType
            , emailTemplate
            , EmailOtpForRegisterExpInSeconds);
    }

    public async Task<BaseResponse<bool>> ConfirmEmailOtpForRegisterAsync(ConfirmEmailVerifyRequest request)
    {
        return await ConfirmEmailOtpAsync(request.Module
            , EntityOtpType.EmailOtpForRegister
            , EmailOtpForRegisterExpInSeconds
            , request.Otp
            , request.Email);
    }

    public async Task<BaseResponse<bool>> SendEmailOtpForPasswordRecoveryAsync(SendEmailVerifyRequest input)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (input.Email.IsMissing())
            throw new BusinessException("InvalidPayload", "Email is required");

        input.Email = input.Email.Trim();

        if (!await context.Users.AnyAsync(u => u.Username.ToLower() == input.Email.ToLower()
                                            && u.DeletedTs == null))
            throw new BusinessException("UserNotFound", "User not found");

        var otpType = EntityOtpType.EmailOtpForPasswordRecovery;
        var emailTemplate = EmailTemplateEnum.EmailOtpForPasswordRecovery;

        return await SendOtpByEmailAsync(context
            , input.Email
            , input.Module
            , otpType
            , emailTemplate
            , EmailOtpForPasswordRecoveryExpInSeconds);
    }

    public async Task<BaseResponse<bool>> ConfirmEmailOtpForPasswordRecoveryAsync(ConfirmEmailVerifyRequest request)
    {
        return await ConfirmEmailOtpAsync(request.Module
            , EntityOtpType.EmailOtpForPasswordRecovery
            , EmailOtpForPasswordRecoveryExpInSeconds
            , request.Otp
            , request.Email);
    }

    public async Task<FacebookUserInfo> ValidateFacebookTokenAsync(string accessToken)
    {
        using var http = new HttpClient();

        var userInfoResponse = await _httpClientService.GetAsync<FacebookUserInfo>(
            $"https://graph.facebook.com/me?fields=id,name,email&access_token={accessToken}");

        return userInfoResponse;
    }

    private async Task<BaseResponse<bool>> SendOtpByEmailAsync(
        ApplicationDbContext context,
        string email,
        Modules module,
        EntityOtpType otpType,
        EmailTemplateEnum emailTemplate,
        int? expInSeconds)
    {
        string otp = GenerateOtp();

        var existed = await context.EntityOTPs.FirstOrDefaultAsync(f => f.Key.ToLower() == email.ToLower()
                                                                     && f.Module == (short)module
                                                                     && f.Type == (short)otpType);

        var modules = module.GetDescription().Split("|");

        var model = new EmailVerificationModel
        {
            ProductName = modules[0],
            ProductFullName = modules[1],
            VerificationCode = otp,
            ExpInSeconds = expInSeconds
        };

        if (existed == null)
            return await CreateAndSendOtpByEmailAsync(module
                , otpType
                , emailTemplate
                , email
                , otp
                , model);

        ValidateResendOtp(existed, ResendEmailOtpInSeconds);

        return await UpdateAndSendOtpByEmailAsync(existed, emailTemplate, otp, model);
    }

    private async Task<BaseResponse<bool>> ConfirmEmailOtpAsync(
        Modules module
        , EntityOtpType otpType
        , int expInSeconds
        , string otp
        , string email)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        otp = otp?.Replace(" ", string.Empty);
        email = email?.Replace(" ", string.Empty);

        try
        {
            var existed = await context.EntityOTPs.FirstOrDefaultAsync(f => f.Key.ToLower() == email.ToLower()
                                                                      && f.Module == (short)module
                                                                      && f.OTP == otp
                                                                      && f.Type == (short)otpType
                                                                      && f.ConfirmedTs == null);

            if (existed == null)
            {
                throw new BusinessException("InvalidOtp", $"Invalid OTP.");
            }

            if (DateTime.UtcNow >= existed.CreatedTs.AddSeconds(expInSeconds))
            {
                context.EntityOTPs.Remove(existed);
                await context.SaveChangesAsync();
                throw new BusinessException("ExpiredOtp", $"Expired OTP.");
            }

            existed.ConfirmedTs = DateTime.UtcNow;
            context.EntityOTPs.Update(existed);

            await context.SaveChangesAsync();
            return new(true, "Verified!");
        }
        catch
        {
            throw;
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    private void ValidateResendOtp(EntityOTP existed, int expInSeconds)
    {
        var nextValidResendTime = existed.ConfirmedTs?.AddSeconds(expInSeconds) ?? existed.CreatedTs.AddSeconds(expInSeconds);
        if (DateTime.UtcNow <= nextValidResendTime)
        {
            var waitTs = (nextValidResendTime - DateTime.UtcNow).PrettyFormatTimeSpan();
            throw new BusinessException(new BusinessErrorItem
            {
                Code = "SpamEmailSending",
                Description = waitTs
            }, $"Please wait more '{waitTs}' and try again.");
        }
    }

    private async Task VerifyEmailAsync(
        Modules module
        , EntityOtpType otpType
        , int expInSeconds
        , string email)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var entity = await context.EntityOTPs.FirstOrDefaultAsync(f => f.Key.ToLower() == email.ToLower()
                                                                    && f.Module == (short)module
                                                                    && f.Type == (short)otpType
                                                                    && f.ConfirmedTs != null);

        if (entity == null)
            throw new BusinessException("UnverifiedEmail", "Unverified Email.");

        var expiredTs = DateTime.UtcNow.AddSeconds(-expInSeconds);
        var confirmedTs = entity.ConfirmedTs;

        context.EntityOTPs.Remove(entity);
        await context.SaveChangesAsync();

        if (confirmedTs > DateTime.UtcNow || confirmedTs < expiredTs)
            throw new BusinessException("ExpiredEmailVerification", "Expired email verification");
    }

    private async Task<BaseResponse<bool>> CreateAndSendOtpByEmailAsync(
        Modules module
        , EntityOtpType otpType
        , EmailTemplateEnum emailTemplate
        , string email
        , string otp
        , EmailVerificationModel content)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var currentCulture = Current.CurrentCulture;

        try
        {
            await context.EntityOTPs.AddAsync(new EntityOTP
            {
                Module = (short)module,
                Type = (short)otpType,
                Key = email,
                OTP = otp,
                CreatedTs = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
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

        // No wait, fire and forget
        FuncTaskHelper.FireAndForget(() => _mailService.SendEmailAsync(email
            , module
            , emailTemplate
            , currentCulture
            , content
        ));

        return new(true, "OTP sent to your email.");
    }

    private async Task<BaseResponse<bool>> UpdateAndSendOtpByEmailAsync(EntityOTP existed
        , EmailTemplateEnum emailTemplate
        , string otp
        , EmailVerificationModel content)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var currentCulture = Current.CurrentCulture;

        try
        {
            existed.CreatedTs = DateTime.UtcNow;
            existed.ConfirmedTs = null;
            existed.OTP = otp;

            context.EntityOTPs.Update(existed);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
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

        // No wait, fire and forget
        FuncTaskHelper.FireAndForget(() => _mailService.SendEmailAsync(existed.Key
           , (Modules)existed.Module
           , emailTemplate
           , currentCulture
           , content));

        return new(true, "OTP was sent to your email.");
    }

    public (string token, DateTime? expiration) GenerateAccessTokenForPaymentGate(TimeSpan exp, GateConnectionOptions gateConnection)
    {
        List<Claim> claims = [
            new Claim(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Sub, gateConnection.Username)
        ];

        return TokenHelper.GetToken(gateConnection.Password, InsightSystemConst, gateConnection.Username, claims, exp);
    }

    private (string token, DateTime? expiration) GenerateAccessTokenFromUser(User user, bool isRememberMe)
    {
        List<Claim> claims = [
            new Claim(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Sub, user.Id.ToString())
        ];

        if (user.Username.IsPresent())
            claims.Add(new Claim(ClaimTypes.Name, user.Username));

        if (user.DisplayName.IsPresent())
            claims.Add(new Claim(SystemClaim.FullName, user.DisplayName));

        return TokenHelper.GetToken(_tokenSettings, claims, isRememberMe);
    }

    private static string GenerateOtp() => new Random().Next(0, 1000000).ToString("D6");
}

public class FacebookUserInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class RegisterRequest : UpdateRequest
{
    public string Username { get; set; }
}

public class UpdateRequest : ChangePasswordRequest
{
    public string DisplayName { get; set; }
}

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public bool RememberMe { get; set; }
}

public class ResetPasswordRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class ChangePasswordRequest
{
    public string Password { get; set; }
    public string NewPassword { get; set; }
}

public class SendEmailVerifyRequest
{
    public Modules Module { get; set; }

    public string Email { get; set; }
}

public class ConfirmEmailVerifyRequest : SendEmailVerifyRequest
{
    public string Otp { get; set; }
}