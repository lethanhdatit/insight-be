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
    , IOptions<TokenSettings> tokenSettings
    , IOptions<ExternalLoginSettings> externalLoginSettings) : BaseHttpBusiness<AccountBusiness, ApplicationDbContext>(logger, contextFactory, contextAccessor), IAccountBusiness
{
    private readonly TokenSettings _tokenSettings = tokenSettings.Value;
    private readonly ExternalLoginSettings _externalLoginSettings = externalLoginSettings.Value;
    private readonly IHttpClientService _httpClientService = httpClientService;
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

            var user = await context.Users.FirstOrDefaultAsync(f => f.FacebookId == null
                                                                 && f.GoogleId == null
                                                                 && f.Username.ToLower() == payload.Username.ToLower());

            if (user != null)
            {
                throw new BusinessException("ExistedUsername", "Username existed");
            }

            var guestUserId = Current.UserId;

            var guestUser = await context.Users.FirstOrDefaultAsync(u => u.Id == guestUserId);

            PasswordHelper.CreatePasswordHash(payload.Password.Trim(), out var passwordHash, out var passwordSalt);

            if (guestUser != null && guestUser.Username == null && guestUser.FacebookId == null && guestUser.GoogleId == null)
            {
                guestUser.Username = payload.Username.Trim();
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
                Email = user.Email,
                DisplayName = user.DisplayName,
                Token = accessToken,
                Expiration = expiration,
                Username = username,
                IsGuest = isGuest
            });
    }

    public async Task<FacebookUserInfo> ValidateFacebookTokenAsync(string accessToken)
    {
        using var http = new HttpClient();

        var userInfoResponse = await _httpClientService.GetAsync<FacebookUserInfo>(
            $"https://graph.facebook.com/me?fields=id,name,email&access_token={accessToken}");

        return userInfoResponse;
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
}

public class FacebookUserInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class RegisterRequest
{
    public string DisplayName { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public bool RememberMe { get; set; }
}