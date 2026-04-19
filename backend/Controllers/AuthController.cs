using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraceAI.Api.Contracts.Auth;
using TraceAI.Api.Data;
using TraceAI.Api.Models;
using TraceAI.Api.Options;
using TraceAI.Api.Services.Auth;
using TraceAI.Api.Services.OAuth;
using TraceAI.Api.Services.Otp;
using Microsoft.Extensions.Options;

namespace TraceAI.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    TraceAiDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IOtpProvider otpProvider,
    IOptions<AuthOptions> authOptions,
    IOptions<OtpOptions> otpOptions,
    GoogleOAuthClient googleOAuthClient) : ControllerBase
{
    private readonly AuthOptions _authOptions = authOptions.Value;
    private readonly OtpOptions _otpOptions = otpOptions.Value;

    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponse>> Signup([FromBody] SignupRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Email and password are required.");
        }

        var existing = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (existing is not null)
        {
            return Conflict("A user with this email already exists.");
        }

        var (hash, salt) = passwordHasher.HashPassword(request.Password);
        var user = new User
        {
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            PhoneNumber = request.PhoneNumber,
            EmailVerified = false,
            PhoneVerified = false
        };

        db.Users.Add(user);
        var response = await IssueTokensAsync(user);
        await db.SaveChangesAsync();

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(x => x.RefreshTokens).FirstOrDefaultAsync(x => x.Email == email);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash) || string.IsNullOrWhiteSpace(user.PasswordSalt))
        {
            return Unauthorized("Invalid credentials.");
        }

        if (!passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Unauthorized("Invalid credentials.");
        }

        var response = await IssueTokensAsync(user);
        await db.SaveChangesAsync();

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var hash = tokenService.HashOpaqueToken(request.RefreshToken);
        var refresh = await db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == hash && !x.Revoked);

        if (refresh?.User is null || refresh.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return Unauthorized("Refresh token is invalid or expired.");
        }

        refresh.Revoked = true;
        var response = await IssueTokensAsync(refresh.User);
        await db.SaveChangesAsync();

        return Ok(response);
    }

    [HttpPost("google/callback")]
    public async Task<ActionResult<AuthResponse>> GoogleCallback([FromBody] GoogleCodeExchangeRequest request, CancellationToken cancellationToken)
    {
        var profile = await googleOAuthClient.ExchangeCodeAsync(request.Code, request.RedirectUri, cancellationToken);
        var email = profile.Email.Trim().ToLowerInvariant();

        var user = await db.Users.Include(x => x.RefreshTokens).FirstOrDefaultAsync(x => x.Email == email);
        if (user is null)
        {
            user = new User
            {
                Email = email,
                EmailVerified = profile.EmailVerified,
                OAuthProvider = "google",
                OAuthSubject = profile.Sub
            };
            db.Users.Add(user);
        }
        else
        {
            user.OAuthProvider = "google";
            user.OAuthSubject = profile.Sub;
            user.EmailVerified = user.EmailVerified || profile.EmailVerified;
        }

        var authResponse = await IssueTokensAsync(user);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(authResponse);
    }

    [HttpPost("phone/request-otp")]
    public async Task<ActionResult<OtpChallengeResponse>> RequestOtp([FromBody] PhoneOtpRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user is null)
        {
            return NotFound("User not found.");
        }

        var now = DateTime.UtcNow;
        var existing = await db.PhoneOtpCodes
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.PhoneNumber == request.PhoneNumber && !x.Verified && x.ExpiresAtUtc > now);

        if (existing is not null && existing.ResendAvailableAtUtc > now)
        {
            return BadRequest($"OTP cooldown active. Retry at {existing.ResendAvailableAtUtc:O}");
        }

        var code = BuildOtpCode(_otpOptions.CodeLength);
        var otp = new PhoneOtpCode
        {
            UserId = user.Id,
            PhoneNumber = request.PhoneNumber,
            CodeHash = tokenService.HashOpaqueToken(code),
            ExpiresAtUtc = now.AddMinutes(_otpOptions.ExpiryMinutes),
            ResendAvailableAtUtc = now.AddSeconds(_otpOptions.ResendCooldownSeconds),
            AttemptCount = 0,
            MaxAttempts = _otpOptions.MaxAttempts,
            Verified = false
        };

        db.PhoneOtpCodes.Add(otp);
        user.PhoneNumber = request.PhoneNumber;
        await otpProvider.SendOtpAsync(request.PhoneNumber, code);
        await db.SaveChangesAsync();

        return Ok(new OtpChallengeResponse(otp.ExpiresAtUtc, otp.ResendAvailableAtUtc, otp.MaxAttempts));
    }

    [HttpPost("phone/verify")]
    public async Task<ActionResult<UserProfileDto>> VerifyOtp([FromBody] VerifyPhoneOtpRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user is null)
        {
            return NotFound("User not found.");
        }

        var now = DateTime.UtcNow;
        var otp = await db.PhoneOtpCodes
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.PhoneNumber == request.PhoneNumber && !x.Verified);

        if (otp is null || otp.ExpiresAtUtc <= now)
        {
            return BadRequest("OTP is expired or unavailable.");
        }

        if (otp.AttemptCount >= otp.MaxAttempts)
        {
            return BadRequest("Maximum OTP attempts reached.");
        }

        otp.AttemptCount += 1;
        if (otp.CodeHash != tokenService.HashOpaqueToken(request.Code))
        {
            await db.SaveChangesAsync();
            return BadRequest("Invalid OTP code.");
        }

        otp.Verified = true;
        user.PhoneVerified = true;
        user.PhoneNumber = request.PhoneNumber;
        await db.SaveChangesAsync();

        return Ok(ToProfile(user));
    }

    private async Task<AuthResponse> IssueTokensAsync(User user)
    {
        var (accessToken, accessExpiry) = tokenService.CreateAccessToken(user);
        var refreshToken = tokenService.CreateSecureRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenService.HashOpaqueToken(refreshToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_authOptions.RefreshTokenDays)
        });

        await Task.CompletedTask;
        return new AuthResponse(accessToken, refreshToken, accessExpiry, ToProfile(user));
    }

    private static UserProfileDto ToProfile(User user)
        => new(user.Email, user.EmailVerified, user.PhoneVerified, user.PhoneNumber, user.OAuthProvider);

    private static string BuildOtpCode(int length)
    {
        var max = (int)Math.Pow(10, length);
        var value = Random.Shared.Next(0, max);
        return value.ToString(new string('0', length));
    }
}
