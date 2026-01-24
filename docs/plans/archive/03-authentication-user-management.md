# Authentication & User Management Plan

## 1. Overview

This document outlines the implementation plan for secure user authentication and authorization in the ClimaSite HVAC e-commerce platform. The system uses JWT tokens with refresh token rotation, Argon2 password hashing, and role-based access control.

### Key Features
- JWT-based authentication with short-lived access tokens (15 min)
- Refresh token rotation with 7-day expiry
- Role-based access control (Admin, Customer)
- Argon2id password hashing (OWASP recommended)
- Account lockout after failed attempts
- Password reset via email
- Profile management

### Security Considerations
- Tokens stored in httpOnly cookies (refresh) and memory (access)
- CSRF protection enabled
- Rate limiting on auth endpoints
- Audit logging for security events

---

## 2. Database Schema

### 2.1 Users Table

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL,
    normalized_email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    phone VARCHAR(20),
    email_confirmed BOOLEAN DEFAULT FALSE,
    email_confirmation_token VARCHAR(255),
    role VARCHAR(50) DEFAULT 'Customer' CHECK (role IN ('Admin', 'Customer')),
    failed_login_attempts INTEGER DEFAULT 0,
    lockout_end TIMESTAMP WITH TIME ZONE,
    last_login_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX idx_users_normalized_email ON users(normalized_email);
CREATE INDEX idx_users_role ON users(role);
```

### 2.2 Refresh Tokens Table

```sql
CREATE TABLE refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL UNIQUE,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    revoked_at TIMESTAMP WITH TIME ZONE,
    replaced_by_token_id UUID REFERENCES refresh_tokens(id),
    created_by_ip VARCHAR(45),
    revoked_by_ip VARCHAR(45)
);

CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_token_hash ON refresh_tokens(token_hash);
CREATE INDEX idx_refresh_tokens_expires_at ON refresh_tokens(expires_at);
```

### 2.3 Password Reset Tokens Table

```sql
CREATE TABLE password_reset_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL UNIQUE,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    used_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX idx_password_reset_tokens_user_id ON password_reset_tokens(user_id);
CREATE INDEX idx_password_reset_tokens_token_hash ON password_reset_tokens(token_hash);
```

### 2.4 Audit Log Table

```sql
CREATE TABLE auth_audit_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    event_type VARCHAR(50) NOT NULL,
    ip_address VARCHAR(45),
    user_agent TEXT,
    details JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX idx_auth_audit_log_user_id ON auth_audit_log(user_id);
CREATE INDEX idx_auth_audit_log_event_type ON auth_audit_log(event_type);
CREATE INDEX idx_auth_audit_log_created_at ON auth_audit_log(created_at);
```

---

## 3. API Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | /api/v1/auth/register | Register new user | No |
| POST | /api/v1/auth/login | Login and get tokens | No |
| POST | /api/v1/auth/refresh | Refresh access token | Refresh Token |
| POST | /api/v1/auth/logout | Revoke refresh token | Yes |
| POST | /api/v1/auth/forgot-password | Request password reset | No |
| POST | /api/v1/auth/reset-password | Reset password with token | No |
| POST | /api/v1/auth/confirm-email | Confirm email address | No |
| GET | /api/v1/auth/me | Get current user | Yes |
| PUT | /api/v1/auth/me | Update profile | Yes |
| PUT | /api/v1/auth/change-password | Change password | Yes |
| GET | /api/v1/admin/users | List all users | Admin |
| GET | /api/v1/admin/users/{id} | Get user by ID | Admin |
| PUT | /api/v1/admin/users/{id}/role | Update user role | Admin |

---

## 4. Implementation Tasks

### Task AUTH-001: Database Migration Setup

**Priority:** Critical
**Estimate:** 2 hours

**Description:**
Create EF Core migration for authentication tables.

**Acceptance Criteria:**
- [ ] Users table created with all fields
- [ ] Refresh tokens table with cascade delete
- [ ] Password reset tokens table
- [ ] Audit log table
- [ ] All indexes created
- [ ] Migration can be rolled back

**Implementation:**

```csharp
// src/ClimaSite.Core/Entities/User.cs
namespace ClimaSite.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? EmailConfirmationToken { get; set; }
    public string Role { get; set; } = "Customer";
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

// src/ClimaSite.Core/Entities/RefreshToken.cs
namespace ClimaSite.Core.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }

    public User User { get; set; } = null!;
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
```

---

### Task AUTH-002: Argon2 Password Hasher

**Priority:** Critical
**Estimate:** 2 hours

**Description:**
Implement Argon2id password hashing per OWASP recommendations.

**Acceptance Criteria:**
- [ ] Argon2id algorithm implemented
- [ ] OWASP recommended parameters (memory: 19MB, iterations: 2, parallelism: 1)
- [ ] Hash verification working
- [ ] Unit tests passing

**Implementation:**

```csharp
// src/ClimaSite.Infrastructure/Security/Argon2PasswordHasher.cs
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using ClimaSite.Core.Interfaces;

namespace ClimaSite.Infrastructure.Security;

public class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int MemorySize = 19456; // 19 MB (OWASP minimum)
    private const int Iterations = 2;
    private const int DegreeOfParallelism = 1;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = HashPasswordWithSalt(password, salt);

        // Format: $argon2id$v=19$m=19456,t=2,p=1$<salt>$<hash>
        var saltBase64 = Convert.ToBase64String(salt);
        var hashBase64 = Convert.ToBase64String(hash);

        return $"$argon2id$v=19$m={MemorySize},t={Iterations},p={DegreeOfParallelism}${saltBase64}${hashBase64}";
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            var parts = hashedPassword.Split('$');
            if (parts.Length != 6 || parts[1] != "argon2id")
                return false;

            var salt = Convert.FromBase64String(parts[4]);
            var expectedHash = Convert.FromBase64String(parts[5]);
            var actualHash = HashPasswordWithSalt(password, salt);

            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch
        {
            return false;
        }
    }

    private byte[] HashPasswordWithSalt(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = MemorySize,
            Iterations = Iterations,
            DegreeOfParallelism = DegreeOfParallelism
        };

        return argon2.GetBytes(HashSize);
    }
}

// src/ClimaSite.Core/Interfaces/IPasswordHasher.cs
namespace ClimaSite.Core.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}
```

**Unit Tests:**

```csharp
// tests/ClimaSite.Core.Tests/Security/Argon2PasswordHasherTests.cs
using ClimaSite.Infrastructure.Security;
using Xunit;

namespace ClimaSite.Core.Tests.Security;

public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ReturnsArgon2idFormat()
    {
        var hash = _hasher.HashPassword("TestPassword123!");

        Assert.StartsWith("$argon2id$v=19$", hash);
        Assert.Contains("m=19456", hash);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var password = "SecurePass123!";
        var hash = _hasher.HashPassword(password);

        Assert.True(_hasher.VerifyPassword(password, hash));
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        var hash = _hasher.HashPassword("CorrectPassword");

        Assert.False(_hasher.VerifyPassword("WrongPassword", hash));
    }

    [Fact]
    public void HashPassword_GeneratesUniqueHashes()
    {
        var password = "SamePassword123!";
        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);

        Assert.NotEqual(hash1, hash2);
    }
}
```

---

### Task AUTH-003: JWT Token Service

**Priority:** Critical
**Estimate:** 4 hours

**Description:**
Implement JWT access token and refresh token generation/validation.

**Acceptance Criteria:**
- [ ] Access token generation with 15-minute expiry
- [ ] Refresh token generation with 7-day expiry
- [ ] Token validation middleware configured
- [ ] Claims include user ID, email, and role
- [ ] Refresh token rotation implemented

**Implementation:**

```csharp
// src/ClimaSite.Core/Interfaces/ITokenService.cs
namespace ClimaSite.Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId, string ipAddress);
    ClaimsPrincipal? ValidateAccessToken(string token);
}

// src/ClimaSite.Infrastructure/Security/JwtTokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ClimaSite.Core.Entities;
using ClimaSite.Core.Interfaces;

namespace ClimaSite.Infrastructure.Security;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}

public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly IPasswordHasher _passwordHasher;

    public JwtTokenService(IOptions<JwtSettings> settings, IPasswordHasher passwordHasher)
    {
        _settings = settings.Value;
        _passwordHasher = passwordHasher;
    }

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("firstName", user.FirstName ?? string.Empty),
            new Claim("lastName", user.LastName ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(Guid userId, string ipAddress)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes);

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(token),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_settings.RefreshTokenExpirationDays),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByIp = ipAddress
        };
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
```

**JWT Configuration:**

```csharp
// src/ClimaSite.Api/Program.cs (partial)
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CustomerOrAdmin", policy => policy.RequireRole("Customer", "Admin"));
});
```

---

### Task AUTH-004: Registration Endpoint

**Priority:** Critical
**Estimate:** 4 hours

**Description:**
Implement user registration with validation and automatic login.

**Acceptance Criteria:**
- [ ] Email validation (format and uniqueness)
- [ ] Password strength validation (min 8 chars, upper, lower, number, special)
- [ ] Returns access and refresh tokens on success
- [ ] Sends email confirmation (async)
- [ ] Rate limited (5 requests per minute per IP)

**Implementation:**

```csharp
// src/ClimaSite.Core/DTOs/Auth/RegisterRequest.cs
namespace ClimaSite.Core.DTOs.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string ConfirmPassword,
    string? FirstName,
    string? LastName,
    string? Phone
);

// src/ClimaSite.Core/DTOs/Auth/AuthResponse.cs
namespace ClimaSite.Core.DTOs.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserDto User
);

public record UserDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? Phone,
    string Role,
    bool EmailConfirmed
);

// src/ClimaSite.Core/Validators/RegisterRequestValidator.cs
using FluentValidation;
using ClimaSite.Core.DTOs.Auth;

namespace ClimaSite.Core.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters")
            .Matches(@"^[\d\s\-\+\(\)]*$").When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Invalid phone number format");
    }
}

// src/ClimaSite.Api/Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ClimaSite.Core.DTOs.Auth;
using ClimaSite.Core.Interfaces;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [EnableRateLimiting("registration")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var ipAddress = GetIpAddress();
        var result = await _authService.RegisterAsync(request, ipAddress);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                "EMAIL_EXISTS" => Conflict(new ProblemDetails
                {
                    Title = "Email already registered",
                    Detail = "An account with this email already exists",
                    Status = StatusCodes.Status409Conflict
                }),
                _ => BadRequest(new ProblemDetails
                {
                    Title = "Registration failed",
                    Detail = result.Error,
                    Status = StatusCodes.Status400BadRequest
                })
            };
        }

        SetRefreshTokenCookie(result.Value!.RefreshToken);

        _logger.LogInformation("User registered: {Email}", request.Email);

        return CreatedAtAction(nameof(GetCurrentUser), result.Value);
    }

    private string GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}

// src/ClimaSite.Infrastructure/Services/AuthService.cs
using ClimaSite.Core.DTOs.Auth;
using ClimaSite.Core.Entities;
using ClimaSite.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClimaSite.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;

    public AuthService(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEmailService emailService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, string ipAddress)
    {
        var normalizedEmail = request.Email.ToUpperInvariant();

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        if (existingUser != null)
        {
            return Result<AuthResponse>.Failure("EMAIL_EXISTS");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Role = "Customer",
            EmailConfirmationToken = Guid.NewGuid().ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.Users.Add(user);

        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, ipAddress);
        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync();

        // Send confirmation email (fire and forget)
        _ = _emailService.SendEmailConfirmationAsync(user.Email, user.EmailConfirmationToken!);

        var accessToken = _tokenService.GenerateAccessToken(user);

        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.TokenHash,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(15),
            User: MapToUserDto(user)
        ));
    }

    private static UserDto MapToUserDto(User user) => new(
        Id: user.Id,
        Email: user.Email,
        FirstName: user.FirstName,
        LastName: user.LastName,
        Phone: user.Phone,
        Role: user.Role,
        EmailConfirmed: user.EmailConfirmed
    );
}
```

---

### Task AUTH-005: Login Endpoint

**Priority:** Critical
**Estimate:** 3 hours

**Description:**
Implement user login with account lockout protection.

**Acceptance Criteria:**
- [ ] Validates credentials
- [ ] Account lockout after 5 failed attempts (15 min lockout)
- [ ] Returns tokens on success
- [ ] Updates last login timestamp
- [ ] Audit logging for login attempts

**Implementation:**

```csharp
// src/ClimaSite.Core/DTOs/Auth/LoginRequest.cs
namespace ClimaSite.Core.DTOs.Auth;

public record LoginRequest(string Email, string Password);

// AuthController.cs (addition)
[HttpPost("login")]
[EnableRateLimiting("login")]
[ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked)]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var ipAddress = GetIpAddress();
    var result = await _authService.LoginAsync(request, ipAddress);

    if (!result.IsSuccess)
    {
        return result.Error switch
        {
            "ACCOUNT_LOCKED" => StatusCode(StatusCodes.Status423Locked, new ProblemDetails
            {
                Title = "Account locked",
                Detail = "Too many failed login attempts. Please try again later.",
                Status = StatusCodes.Status423Locked
            }),
            _ => Unauthorized(new ProblemDetails
            {
                Title = "Authentication failed",
                Detail = "Invalid email or password",
                Status = StatusCodes.Status401Unauthorized
            })
        };
    }

    SetRefreshTokenCookie(result.Value!.RefreshToken);

    _logger.LogInformation("User logged in: {Email}", request.Email);

    return Ok(result.Value);
}

// AuthService.cs (addition)
public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string ipAddress)
{
    var normalizedEmail = request.Email.ToUpperInvariant();

    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

    if (user == null)
    {
        await LogAuditEventAsync(null, "LOGIN_FAILED", ipAddress, "User not found");
        return Result<AuthResponse>.Failure("INVALID_CREDENTIALS");
    }

    // Check if account is locked
    if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
    {
        await LogAuditEventAsync(user.Id, "LOGIN_BLOCKED", ipAddress, "Account locked");
        return Result<AuthResponse>.Failure("ACCOUNT_LOCKED");
    }

    // Verify password
    if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
    {
        user.FailedLoginAttempts++;

        if (user.FailedLoginAttempts >= 5)
        {
            user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
            await LogAuditEventAsync(user.Id, "ACCOUNT_LOCKED", ipAddress, "Max attempts exceeded");
        }

        await _context.SaveChangesAsync();
        await LogAuditEventAsync(user.Id, "LOGIN_FAILED", ipAddress, "Invalid password");
        return Result<AuthResponse>.Failure("INVALID_CREDENTIALS");
    }

    // Reset failed attempts on successful login
    user.FailedLoginAttempts = 0;
    user.LockoutEnd = null;
    user.LastLoginAt = DateTimeOffset.UtcNow;
    user.UpdatedAt = DateTimeOffset.UtcNow;

    // Generate tokens
    var refreshToken = _tokenService.GenerateRefreshToken(user.Id, ipAddress);
    _context.RefreshTokens.Add(refreshToken);

    await _context.SaveChangesAsync();
    await LogAuditEventAsync(user.Id, "LOGIN_SUCCESS", ipAddress);

    var accessToken = _tokenService.GenerateAccessToken(user);

    return Result<AuthResponse>.Success(new AuthResponse(
        AccessToken: accessToken,
        RefreshToken: refreshToken.TokenHash,
        ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(15),
        User: MapToUserDto(user)
    ));
}
```

---

### Task AUTH-006: Refresh Token Endpoint

**Priority:** Critical
**Estimate:** 3 hours

**Description:**
Implement token refresh with rotation for security.

**Acceptance Criteria:**
- [ ] Validates refresh token from httpOnly cookie
- [ ] Implements token rotation (old token revoked)
- [ ] Detects token reuse (potential theft)
- [ ] Returns new access and refresh tokens

**Implementation:**

```csharp
// AuthController.cs (addition)
[HttpPost("refresh")]
[ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> RefreshToken()
{
    var refreshToken = Request.Cookies["refreshToken"];

    if (string.IsNullOrEmpty(refreshToken))
    {
        return Unauthorized(new ProblemDetails
        {
            Title = "No refresh token",
            Detail = "Refresh token is required",
            Status = StatusCodes.Status401Unauthorized
        });
    }

    var ipAddress = GetIpAddress();
    var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

    if (!result.IsSuccess)
    {
        Response.Cookies.Delete("refreshToken");
        return Unauthorized(new ProblemDetails
        {
            Title = "Invalid refresh token",
            Detail = result.Error,
            Status = StatusCodes.Status401Unauthorized
        });
    }

    SetRefreshTokenCookie(result.Value!.RefreshToken);
    return Ok(result.Value);
}

// AuthService.cs (addition)
public async Task<Result<AuthResponse>> RefreshTokenAsync(string tokenHash, string ipAddress)
{
    var token = await _context.RefreshTokens
        .Include(t => t.User)
        .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    if (token == null)
    {
        return Result<AuthResponse>.Failure("TOKEN_NOT_FOUND");
    }

    // Detect token reuse (stolen token)
    if (token.IsRevoked)
    {
        // Revoke all tokens for this user (potential security breach)
        await RevokeAllUserTokensAsync(token.UserId, ipAddress, "Token reuse detected");
        await LogAuditEventAsync(token.UserId, "TOKEN_REUSE_DETECTED", ipAddress);
        return Result<AuthResponse>.Failure("TOKEN_REUSED");
    }

    if (!token.IsActive)
    {
        return Result<AuthResponse>.Failure("TOKEN_EXPIRED");
    }

    // Rotate token
    var newRefreshToken = _tokenService.GenerateRefreshToken(token.UserId, ipAddress);

    token.RevokedAt = DateTimeOffset.UtcNow;
    token.RevokedByIp = ipAddress;
    token.ReplacedByTokenId = newRefreshToken.Id;

    _context.RefreshTokens.Add(newRefreshToken);
    await _context.SaveChangesAsync();

    var accessToken = _tokenService.GenerateAccessToken(token.User);

    return Result<AuthResponse>.Success(new AuthResponse(
        AccessToken: accessToken,
        RefreshToken: newRefreshToken.TokenHash,
        ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(15),
        User: MapToUserDto(token.User)
    ));
}

private async Task RevokeAllUserTokensAsync(Guid userId, string ipAddress, string reason)
{
    var activeTokens = await _context.RefreshTokens
        .Where(t => t.UserId == userId && t.RevokedAt == null)
        .ToListAsync();

    foreach (var token in activeTokens)
    {
        token.RevokedAt = DateTimeOffset.UtcNow;
        token.RevokedByIp = ipAddress;
    }

    await _context.SaveChangesAsync();
}
```

---

### Task AUTH-007: Logout Endpoint

**Priority:** High
**Estimate:** 1 hour

**Description:**
Implement logout that revokes refresh token.

**Acceptance Criteria:**
- [ ] Revokes current refresh token
- [ ] Clears refresh token cookie
- [ ] Returns success response

**Implementation:**

```csharp
// AuthController.cs (addition)
[HttpPost("logout")]
[Authorize]
[ProducesResponseType(StatusCodes.Status204NoContent)]
public async Task<IActionResult> Logout()
{
    var refreshToken = Request.Cookies["refreshToken"];

    if (!string.IsNullOrEmpty(refreshToken))
    {
        var ipAddress = GetIpAddress();
        await _authService.RevokeTokenAsync(refreshToken, ipAddress);
    }

    Response.Cookies.Delete("refreshToken");

    _logger.LogInformation("User logged out: {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));

    return NoContent();
}

// AuthService.cs (addition)
public async Task RevokeTokenAsync(string tokenHash, string ipAddress)
{
    var token = await _context.RefreshTokens
        .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    if (token != null && token.IsActive)
    {
        token.RevokedAt = DateTimeOffset.UtcNow;
        token.RevokedByIp = ipAddress;
        await _context.SaveChangesAsync();
    }
}
```

---

### Task AUTH-008: Password Reset Flow

**Priority:** High
**Estimate:** 4 hours

**Description:**
Implement forgot password and reset password endpoints.

**Acceptance Criteria:**
- [ ] Forgot password sends email with reset link
- [ ] Reset token expires in 1 hour
- [ ] Reset password validates token and updates password
- [ ] Old refresh tokens revoked after password change

**Implementation:**

```csharp
// DTOs
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword, string ConfirmPassword);

// AuthController.cs (additions)
[HttpPost("forgot-password")]
[EnableRateLimiting("passwordReset")]
[ProducesResponseType(StatusCodes.Status200OK)]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
{
    // Always return success to prevent email enumeration
    await _authService.InitiatePasswordResetAsync(request.Email);

    return Ok(new { message = "If the email exists, a password reset link has been sent." });
}

[HttpPost("reset-password")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
{
    var result = await _authService.ResetPasswordAsync(request);

    if (!result.IsSuccess)
    {
        return BadRequest(new ProblemDetails
        {
            Title = "Password reset failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    return Ok(new { message = "Password has been reset successfully." });
}

// AuthService.cs (additions)
public async Task InitiatePasswordResetAsync(string email)
{
    var normalizedEmail = email.ToUpperInvariant();
    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

    if (user == null)
    {
        return; // Silent fail to prevent enumeration
    }

    // Invalidate existing reset tokens
    var existingTokens = await _context.PasswordResetTokens
        .Where(t => t.UserId == user.Id && t.UsedAt == null)
        .ToListAsync();

    foreach (var t in existingTokens)
    {
        t.UsedAt = DateTimeOffset.UtcNow;
    }

    // Create new reset token
    var token = Guid.NewGuid().ToString("N");
    var resetToken = new PasswordResetToken
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        TokenHash = HashToken(token),
        ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        CreatedAt = DateTimeOffset.UtcNow
    };

    _context.PasswordResetTokens.Add(resetToken);
    await _context.SaveChangesAsync();

    await _emailService.SendPasswordResetEmailAsync(user.Email, token);
}

public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequest request)
{
    var tokenHash = HashToken(request.Token);
    var resetToken = await _context.PasswordResetTokens
        .Include(t => t.User)
        .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    if (resetToken == null || resetToken.UsedAt != null || resetToken.ExpiresAt < DateTimeOffset.UtcNow)
    {
        return Result<bool>.Failure("Invalid or expired reset token");
    }

    if (request.NewPassword != request.ConfirmPassword)
    {
        return Result<bool>.Failure("Passwords do not match");
    }

    // Update password
    resetToken.User.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
    resetToken.User.UpdatedAt = DateTimeOffset.UtcNow;
    resetToken.UsedAt = DateTimeOffset.UtcNow;

    // Revoke all refresh tokens
    await RevokeAllUserTokensAsync(resetToken.UserId, "unknown", "Password reset");

    await _context.SaveChangesAsync();
    await LogAuditEventAsync(resetToken.UserId, "PASSWORD_RESET", "unknown");

    return Result<bool>.Success(true);
}
```

---

### Task AUTH-009: Get Current User Endpoint

**Priority:** High
**Estimate:** 1 hour

**Description:**
Implement endpoint to get authenticated user's profile.

**Acceptance Criteria:**
- [ ] Returns user profile for authenticated requests
- [ ] Excludes sensitive data (password hash, tokens)

**Implementation:**

```csharp
// AuthController.cs (addition)
[HttpGet("me")]
[Authorize]
[ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
public async Task<IActionResult> GetCurrentUser()
{
    var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var user = await _authService.GetUserByIdAsync(userId);

    if (user == null)
    {
        return NotFound();
    }

    return Ok(user);
}

// AuthService.cs (addition)
public async Task<UserDto?> GetUserByIdAsync(Guid userId)
{
    var user = await _context.Users.FindAsync(userId);
    return user == null ? null : MapToUserDto(user);
}
```

---

### Task AUTH-010: Update Profile Endpoint

**Priority:** High
**Estimate:** 2 hours

**Description:**
Implement endpoint to update user profile.

**Acceptance Criteria:**
- [ ] Updates first name, last name, phone
- [ ] Email change requires re-confirmation
- [ ] Validates input

**Implementation:**

```csharp
// DTOs
public record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? Phone
);

// AuthController.cs (addition)
[HttpPut("me")]
[Authorize]
[ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
{
    var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await _authService.UpdateProfileAsync(userId, request);

    if (!result.IsSuccess)
    {
        return BadRequest(new ProblemDetails
        {
            Title = "Update failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    return Ok(result.Value);
}

// AuthService.cs (addition)
public async Task<Result<UserDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
{
    var user = await _context.Users.FindAsync(userId);

    if (user == null)
    {
        return Result<UserDto>.Failure("User not found");
    }

    user.FirstName = request.FirstName ?? user.FirstName;
    user.LastName = request.LastName ?? user.LastName;
    user.Phone = request.Phone ?? user.Phone;
    user.UpdatedAt = DateTimeOffset.UtcNow;

    await _context.SaveChangesAsync();

    return Result<UserDto>.Success(MapToUserDto(user));
}
```

---

### Task AUTH-011: Change Password Endpoint

**Priority:** High
**Estimate:** 2 hours

**Description:**
Implement endpoint to change password for authenticated users.

**Acceptance Criteria:**
- [ ] Requires current password verification
- [ ] Validates new password strength
- [ ] Revokes all other refresh tokens

**Implementation:**

```csharp
// DTOs
public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
);

// AuthController.cs (addition)
[HttpPut("change-password")]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
{
    var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var ipAddress = GetIpAddress();
    var result = await _authService.ChangePasswordAsync(userId, request, ipAddress);

    if (!result.IsSuccess)
    {
        return BadRequest(new ProblemDetails
        {
            Title = "Password change failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    return Ok(new { message = "Password changed successfully." });
}

// AuthService.cs (addition)
public async Task<Result<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string ipAddress)
{
    var user = await _context.Users.FindAsync(userId);

    if (user == null)
    {
        return Result<bool>.Failure("User not found");
    }

    if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
    {
        return Result<bool>.Failure("Current password is incorrect");
    }

    if (request.NewPassword != request.ConfirmPassword)
    {
        return Result<bool>.Failure("New passwords do not match");
    }

    user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
    user.UpdatedAt = DateTimeOffset.UtcNow;

    // Revoke all refresh tokens except current session
    var currentToken = Request.Cookies["refreshToken"];
    var tokensToRevoke = await _context.RefreshTokens
        .Where(t => t.UserId == userId && t.RevokedAt == null && t.TokenHash != currentToken)
        .ToListAsync();

    foreach (var token in tokensToRevoke)
    {
        token.RevokedAt = DateTimeOffset.UtcNow;
        token.RevokedByIp = ipAddress;
    }

    await _context.SaveChangesAsync();
    await LogAuditEventAsync(userId, "PASSWORD_CHANGED", ipAddress);

    return Result<bool>.Success(true);
}
```

---

### Task AUTH-012: Admin User Management Endpoints

**Priority:** Medium
**Estimate:** 4 hours

**Description:**
Implement admin endpoints for user management.

**Acceptance Criteria:**
- [ ] List users with pagination and filtering
- [ ] Get user by ID
- [ ] Update user role
- [ ] Admin-only authorization

**Implementation:**

```csharp
// src/ClimaSite.Api/Controllers/AdminUsersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClimaSite.Core.DTOs.Auth;
using ClimaSite.Core.Interfaces;

namespace ClimaSite.Api.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = "AdminOnly")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserService _userService;

    public AdminUsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null)
    {
        var result = await _userService.GetUsersAsync(page, pageSize, search, role);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPut("{id:guid}/role")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var result = await _userService.UpdateUserRoleAsync(id, request.Role);

        if (!result.IsSuccess)
        {
            return result.Error == "USER_NOT_FOUND"
                ? NotFound()
                : BadRequest(new ProblemDetails
                {
                    Title = "Update failed",
                    Detail = result.Error,
                    Status = StatusCodes.Status400BadRequest
                });
        }

        return Ok(result.Value);
    }
}

public record UpdateRoleRequest(string Role);
```

---

### Task AUTH-013: Email Confirmation Endpoint

**Priority:** Medium
**Estimate:** 2 hours

**Description:**
Implement email confirmation endpoint.

**Acceptance Criteria:**
- [ ] Validates confirmation token
- [ ] Updates email_confirmed flag
- [ ] Returns appropriate response

**Implementation:**

```csharp
// AuthController.cs (addition)
[HttpPost("confirm-email")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
{
    var result = await _authService.ConfirmEmailAsync(request.Token);

    if (!result.IsSuccess)
    {
        return BadRequest(new ProblemDetails
        {
            Title = "Email confirmation failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        });
    }

    return Ok(new { message = "Email confirmed successfully." });
}

public record ConfirmEmailRequest(string Token);

// AuthService.cs (addition)
public async Task<Result<bool>> ConfirmEmailAsync(string token)
{
    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);

    if (user == null)
    {
        return Result<bool>.Failure("Invalid confirmation token");
    }

    user.EmailConfirmed = true;
    user.EmailConfirmationToken = null;
    user.UpdatedAt = DateTimeOffset.UtcNow;

    await _context.SaveChangesAsync();

    return Result<bool>.Success(true);
}
```

---

## 5. Frontend Components

### Task AUTH-014: Auth Service

**Priority:** Critical
**Estimate:** 4 hours

**Description:**
Implement Angular authentication service.

**Implementation:**

```typescript
// src/ClimaSite.Web/src/app/core/services/auth.service.ts
import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface User {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  phone: string | null;
  role: 'Admin' | 'Customer';
  emailConfirmed: boolean;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName?: string;
  lastName?: string;
  phone?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/auth`;

  // Signals for reactive state
  private currentUserSignal = signal<User | null>(null);
  private accessTokenSignal = signal<string | null>(null);
  private tokenExpirySignal = signal<Date | null>(null);

  // Computed signals
  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.currentUserSignal());
  readonly isAdmin = computed(() => this.currentUserSignal()?.role === 'Admin');

  // Token refresh timer
  private refreshTimer: ReturnType<typeof setTimeout> | null = null;

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    this.initializeAuth();
  }

  private initializeAuth(): void {
    // Check for existing session on app startup
    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      this.currentUserSignal.set(JSON.parse(storedUser));
      this.refreshToken().subscribe({
        error: () => this.logout()
      });
    }
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, request)
      .pipe(
        tap(response => this.handleAuthResponse(response)),
        catchError(this.handleError)
      );
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request)
      .pipe(
        tap(response => this.handleAuthResponse(response)),
        catchError(this.handleError)
      );
  }

  refreshToken(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, {}, { withCredentials: true })
      .pipe(
        tap(response => this.handleAuthResponse(response)),
        catchError(error => {
          this.clearAuth();
          return throwError(() => error);
        })
      );
  }

  logout(): void {
    this.http.post(`${this.apiUrl}/logout`, {}, { withCredentials: true })
      .subscribe({
        complete: () => {
          this.clearAuth();
          this.router.navigate(['/login']);
        },
        error: () => {
          this.clearAuth();
          this.router.navigate(['/login']);
        }
      });
  }

  getAccessToken(): string | null {
    return this.accessTokenSignal();
  }

  private handleAuthResponse(response: AuthResponse): void {
    this.accessTokenSignal.set(response.accessToken);
    this.currentUserSignal.set(response.user);
    this.tokenExpirySignal.set(new Date(response.expiresAt));

    localStorage.setItem('user', JSON.stringify(response.user));

    this.scheduleTokenRefresh(response.expiresAt);
  }

  private scheduleTokenRefresh(expiresAt: string): void {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
    }

    const expiry = new Date(expiresAt).getTime();
    const now = Date.now();
    const refreshTime = expiry - now - 60000; // Refresh 1 minute before expiry

    if (refreshTime > 0) {
      this.refreshTimer = setTimeout(() => {
        this.refreshToken().subscribe();
      }, refreshTime);
    }
  }

  private clearAuth(): void {
    this.accessTokenSignal.set(null);
    this.currentUserSignal.set(null);
    this.tokenExpirySignal.set(null);
    localStorage.removeItem('user');

    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
      this.refreshTimer = null;
    }
  }

  private handleError(error: any): Observable<never> {
    let errorMessage = 'An error occurred';

    if (error.error?.detail) {
      errorMessage = error.error.detail;
    } else if (error.error?.title) {
      errorMessage = error.error.title;
    }

    return throwError(() => new Error(errorMessage));
  }
}
```

---

### Task AUTH-015: Auth Interceptor

**Priority:** Critical
**Estimate:** 2 hours

**Description:**
Implement HTTP interceptor for adding JWT to requests.

**Implementation:**

```typescript
// src/ClimaSite.Web/src/app/core/interceptors/auth.interceptor.ts
import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const authService = inject(AuthService);

  // Skip auth header for auth endpoints (except /me)
  if (req.url.includes('/auth/') && !req.url.includes('/auth/me')) {
    return next(req);
  }

  const token = authService.getAccessToken();

  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !req.url.includes('/auth/refresh')) {
        // Token expired, try to refresh
        return authService.refreshToken().pipe(
          switchMap(() => {
            const newToken = authService.getAccessToken();
            const newReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${newToken}`
              }
            });
            return next(newReq);
          }),
          catchError(refreshError => {
            authService.logout();
            return throwError(() => refreshError);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
```

---

### Task AUTH-016: Auth Guard

**Priority:** Critical
**Estimate:** 1 hour

**Description:**
Implement route guards for protected routes.

**Implementation:**

```typescript
// src/ClimaSite.Web/src/app/core/guards/auth.guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
  return false;
};

export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAdmin()) {
    return true;
  }

  if (authService.isAuthenticated()) {
    router.navigate(['/unauthorized']);
  } else {
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
  }

  return false;
};

export const guestGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  router.navigate(['/dashboard']);
  return false;
};
```

---

### Task AUTH-017: Login Component

**Priority:** Critical
**Estimate:** 3 hours

**Description:**
Implement login page component.

**Implementation:**

```typescript
// src/ClimaSite.Web/src/app/features/auth/login/login.component.ts
import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="auth-card">
        <h1>Sign In</h1>
        <p class="subtitle">Welcome back to ClimaSite</p>

        @if (errorMessage()) {
          <div class="error-alert" role="alert">
            {{ errorMessage() }}
          </div>
        }

        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label for="email">Email</label>
            <input
              id="email"
              type="email"
              formControlName="email"
              data-testid="email-input"
              [class.invalid]="isFieldInvalid('email')"
              placeholder="Enter your email"
            />
            @if (isFieldInvalid('email')) {
              <span class="error-message">Please enter a valid email</span>
            }
          </div>

          <div class="form-group">
            <label for="password">Password</label>
            <input
              id="password"
              type="password"
              formControlName="password"
              data-testid="password-input"
              [class.invalid]="isFieldInvalid('password')"
              placeholder="Enter your password"
            />
            @if (isFieldInvalid('password')) {
              <span class="error-message">Password is required</span>
            }
          </div>

          <div class="form-actions">
            <a routerLink="/forgot-password" class="forgot-link">Forgot password?</a>
          </div>

          <button
            type="submit"
            class="btn-primary"
            data-testid="login-button"
            [disabled]="isLoading()"
          >
            @if (isLoading()) {
              <span class="spinner"></span>
              Signing in...
            } @else {
              Sign In
            }
          </button>
        </form>

        <p class="auth-footer">
          Don't have an account? <a routerLink="/register">Sign up</a>
        </p>
      </div>
    </div>
  `,
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  loginForm: FormGroup;
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  private returnUrl: string = '/dashboard';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });

    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
  }

  isFieldInvalid(field: string): boolean {
    const control = this.loginForm.get(field);
    return !!(control && control.invalid && control.touched);
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.loginForm.value).subscribe({
      next: () => {
        this.router.navigateByUrl(this.returnUrl);
      },
      error: (error) => {
        this.isLoading.set(false);
        this.errorMessage.set(error.message);
      }
    });
  }
}
```

---

### Task AUTH-018: Register Component

**Priority:** Critical
**Estimate:** 4 hours

**Description:**
Implement registration page component.

**Implementation:**

```typescript
// src/ClimaSite.Web/src/app/features/auth/register/register.component.ts
import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="auth-card">
        <h1>Create Account</h1>
        <p class="subtitle">Join ClimaSite today</p>

        @if (errorMessage()) {
          <div class="error-alert" role="alert">
            {{ errorMessage() }}
          </div>
        }

        <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">
          <div class="form-row">
            <div class="form-group">
              <label for="firstName">First Name</label>
              <input
                id="firstName"
                type="text"
                formControlName="firstName"
                data-testid="first-name-input"
                placeholder="John"
              />
            </div>

            <div class="form-group">
              <label for="lastName">Last Name</label>
              <input
                id="lastName"
                type="text"
                formControlName="lastName"
                data-testid="last-name-input"
                placeholder="Doe"
              />
            </div>
          </div>

          <div class="form-group">
            <label for="email">Email *</label>
            <input
              id="email"
              type="email"
              formControlName="email"
              data-testid="email-input"
              [class.invalid]="isFieldInvalid('email')"
              placeholder="john.doe@example.com"
            />
            @if (isFieldInvalid('email')) {
              <span class="error-message">Please enter a valid email</span>
            }
          </div>

          <div class="form-group">
            <label for="phone">Phone</label>
            <input
              id="phone"
              type="tel"
              formControlName="phone"
              data-testid="phone-input"
              placeholder="+1 (555) 123-4567"
            />
          </div>

          <div class="form-group">
            <label for="password">Password *</label>
            <input
              id="password"
              type="password"
              formControlName="password"
              data-testid="password-input"
              [class.invalid]="isFieldInvalid('password')"
              placeholder="Create a strong password"
            />
            @if (isFieldInvalid('password')) {
              <span class="error-message">
                Password must be at least 8 characters with uppercase, lowercase, number, and special character
              </span>
            }
          </div>

          <div class="form-group">
            <label for="confirmPassword">Confirm Password *</label>
            <input
              id="confirmPassword"
              type="password"
              formControlName="confirmPassword"
              data-testid="confirm-password-input"
              [class.invalid]="isFieldInvalid('confirmPassword')"
              placeholder="Confirm your password"
            />
            @if (isFieldInvalid('confirmPassword')) {
              <span class="error-message">Passwords do not match</span>
            }
          </div>

          <button
            type="submit"
            class="btn-primary"
            data-testid="register-button"
            [disabled]="isLoading()"
          >
            @if (isLoading()) {
              <span class="spinner"></span>
              Creating account...
            } @else {
              Create Account
            }
          </button>
        </form>

        <p class="auth-footer">
          Already have an account? <a routerLink="/login">Sign in</a>
        </p>
      </div>
    </div>
  `,
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  registerForm: FormGroup;
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$/)
      ]],
      confirmPassword: ['', Validators.required],
      firstName: [''],
      lastName: [''],
      phone: ['']
    }, { validators: this.passwordMatchValidator });
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');

    if (password && confirmPassword && password.value !== confirmPassword.value) {
      confirmPassword.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }

    return null;
  }

  isFieldInvalid(field: string): boolean {
    const control = this.registerForm.get(field);
    return !!(control && control.invalid && control.touched);
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.register(this.registerForm.value).subscribe({
      next: () => {
        this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        this.isLoading.set(false);
        this.errorMessage.set(error.message);
      }
    });
  }
}
```

---

### Task AUTH-019: Forgot Password Component

**Priority:** High
**Estimate:** 2 hours

**Implementation:**

```typescript
// src/ClimaSite.Web/src/app/features/auth/forgot-password/forgot-password.component.ts
import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="auth-card">
        <h1>Reset Password</h1>
        <p class="subtitle">Enter your email to receive a reset link</p>

        @if (isSubmitted()) {
          <div class="success-alert" role="alert">
            If an account with that email exists, we've sent a password reset link.
          </div>
          <a routerLink="/login" class="btn-primary">Back to Login</a>
        } @else {
          <form [formGroup]="forgotForm" (ngSubmit)="onSubmit()">
            <div class="form-group">
              <label for="email">Email</label>
              <input
                id="email"
                type="email"
                formControlName="email"
                data-testid="email-input"
                [class.invalid]="isFieldInvalid('email')"
                placeholder="Enter your email"
              />
              @if (isFieldInvalid('email')) {
                <span class="error-message">Please enter a valid email</span>
              }
            </div>

            <button
              type="submit"
              class="btn-primary"
              data-testid="submit-button"
              [disabled]="isLoading()"
            >
              @if (isLoading()) {
                <span class="spinner"></span>
                Sending...
              } @else {
                Send Reset Link
              }
            </button>
          </form>

          <p class="auth-footer">
            Remember your password? <a routerLink="/login">Sign in</a>
          </p>
        }
      </div>
    </div>
  `
})
export class ForgotPasswordComponent {
  forgotForm: FormGroup;
  isLoading = signal(false);
  isSubmitted = signal(false);

  constructor(
    private fb: FormBuilder,
    private http: HttpClient
  ) {
    this.forgotForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  isFieldInvalid(field: string): boolean {
    const control = this.forgotForm.get(field);
    return !!(control && control.invalid && control.touched);
  }

  onSubmit(): void {
    if (this.forgotForm.invalid) {
      this.forgotForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);

    this.http.post(`${environment.apiUrl}/auth/forgot-password`, this.forgotForm.value)
      .subscribe({
        next: () => {
          this.isLoading.set(false);
          this.isSubmitted.set(true);
        },
        error: () => {
          // Still show success to prevent email enumeration
          this.isLoading.set(false);
          this.isSubmitted.set(true);
        }
      });
  }
}
```

---

### Task AUTH-020: Profile Component

**Priority:** High
**Estimate:** 3 hours

**Implementation:**

```typescript
// src/ClimaSite.Web/src/app/features/profile/profile.component.ts
import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService, User } from '../../core/services/auth.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="profile-container">
      <h1>My Profile</h1>

      @if (successMessage()) {
        <div class="success-alert">{{ successMessage() }}</div>
      }

      @if (errorMessage()) {
        <div class="error-alert">{{ errorMessage() }}</div>
      }

      <div class="profile-card">
        <h2>Personal Information</h2>

        <form [formGroup]="profileForm" (ngSubmit)="onSubmitProfile()">
          <div class="form-row">
            <div class="form-group">
              <label for="firstName">First Name</label>
              <input
                id="firstName"
                type="text"
                formControlName="firstName"
                data-testid="first-name-input"
              />
            </div>

            <div class="form-group">
              <label for="lastName">Last Name</label>
              <input
                id="lastName"
                type="text"
                formControlName="lastName"
                data-testid="last-name-input"
              />
            </div>
          </div>

          <div class="form-group">
            <label for="email">Email</label>
            <input
              id="email"
              type="email"
              formControlName="email"
              readonly
              class="readonly"
            />
            <span class="help-text">Email cannot be changed</span>
          </div>

          <div class="form-group">
            <label for="phone">Phone</label>
            <input
              id="phone"
              type="tel"
              formControlName="phone"
              data-testid="phone-input"
            />
          </div>

          <button
            type="submit"
            class="btn-primary"
            data-testid="save-profile-button"
            [disabled]="isLoadingProfile()"
          >
            @if (isLoadingProfile()) {
              Saving...
            } @else {
              Save Changes
            }
          </button>
        </form>
      </div>

      <div class="profile-card">
        <h2>Change Password</h2>

        <form [formGroup]="passwordForm" (ngSubmit)="onSubmitPassword()">
          <div class="form-group">
            <label for="currentPassword">Current Password</label>
            <input
              id="currentPassword"
              type="password"
              formControlName="currentPassword"
              data-testid="current-password-input"
            />
          </div>

          <div class="form-group">
            <label for="newPassword">New Password</label>
            <input
              id="newPassword"
              type="password"
              formControlName="newPassword"
              data-testid="new-password-input"
            />
          </div>

          <div class="form-group">
            <label for="confirmPassword">Confirm New Password</label>
            <input
              id="confirmPassword"
              type="password"
              formControlName="confirmPassword"
              data-testid="confirm-new-password-input"
            />
          </div>

          <button
            type="submit"
            class="btn-primary"
            data-testid="change-password-button"
            [disabled]="isLoadingPassword()"
          >
            @if (isLoadingPassword()) {
              Changing...
            } @else {
              Change Password
            }
          </button>
        </form>
      </div>
    </div>
  `
})
export class ProfileComponent implements OnInit {
  profileForm!: FormGroup;
  passwordForm!: FormGroup;

  isLoadingProfile = signal(false);
  isLoadingPassword = signal(false);
  successMessage = signal<string | null>(null);
  errorMessage = signal<string | null>(null);

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const user = this.authService.currentUser();

    this.profileForm = this.fb.group({
      firstName: [user?.firstName || ''],
      lastName: [user?.lastName || ''],
      email: [user?.email || ''],
      phone: [user?.phone || '']
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required]
    });
  }

  onSubmitProfile(): void {
    this.isLoadingProfile.set(true);
    this.clearMessages();

    const { firstName, lastName, phone } = this.profileForm.value;

    this.http.put(`${environment.apiUrl}/auth/me`, { firstName, lastName, phone })
      .subscribe({
        next: () => {
          this.isLoadingProfile.set(false);
          this.successMessage.set('Profile updated successfully');
        },
        error: (error) => {
          this.isLoadingProfile.set(false);
          this.errorMessage.set(error.error?.detail || 'Failed to update profile');
        }
      });
  }

  onSubmitPassword(): void {
    if (this.passwordForm.value.newPassword !== this.passwordForm.value.confirmPassword) {
      this.errorMessage.set('New passwords do not match');
      return;
    }

    this.isLoadingPassword.set(true);
    this.clearMessages();

    this.http.put(`${environment.apiUrl}/auth/change-password`, this.passwordForm.value)
      .subscribe({
        next: () => {
          this.isLoadingPassword.set(false);
          this.successMessage.set('Password changed successfully');
          this.passwordForm.reset();
        },
        error: (error) => {
          this.isLoadingPassword.set(false);
          this.errorMessage.set(error.error?.detail || 'Failed to change password');
        }
      });
  }

  private clearMessages(): void {
    this.successMessage.set(null);
    this.errorMessage.set(null);
  }
}
```

---

## 6. E2E Tests (Playwright - NO MOCKING)

### Task AUTH-021: Playwright Test Setup

**Priority:** Critical
**Estimate:** 2 hours

**Description:**
Configure Playwright for E2E testing with real API calls.

**Implementation:**

```typescript
// tests/e2e/playwright.config.ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false, // Run sequentially to avoid race conditions with real DB
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1, // Single worker for DB consistency
  reporter: 'html',
  use: {
    baseURL: process.env.E2E_BASE_URL || 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: [
    {
      command: 'dotnet run --project src/ClimaSite.Api',
      url: 'http://localhost:5000/health',
      reuseExistingServer: !process.env.CI,
      timeout: 120000,
    },
    {
      command: 'cd src/ClimaSite.Web && ng serve',
      url: 'http://localhost:4200',
      reuseExistingServer: !process.env.CI,
      timeout: 120000,
    },
  ],
});
```

```typescript
// tests/e2e/fixtures/auth.fixture.ts
import { test as base, expect } from '@playwright/test';

interface AuthFixtures {
  apiUrl: string;
  createTestUser: (overrides?: Partial<TestUser>) => Promise<TestUser>;
  cleanupTestUsers: () => Promise<void>;
}

interface TestUser {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export const test = base.extend<AuthFixtures>({
  apiUrl: async ({}, use) => {
    await use(process.env.E2E_API_URL || 'http://localhost:5000/api/v1');
  },

  createTestUser: async ({ request, apiUrl }, use) => {
    const createdUsers: string[] = [];

    const createUser = async (overrides: Partial<TestUser> = {}): Promise<TestUser> => {
      const user: TestUser = {
        email: `test-${Date.now()}-${Math.random().toString(36).substr(2, 9)}@example.com`,
        password: 'SecurePass123!',
        firstName: 'Test',
        lastName: 'User',
        ...overrides,
      };

      const response = await request.post(`${apiUrl}/auth/register`, {
        data: {
          email: user.email,
          password: user.password,
          confirmPassword: user.password,
          firstName: user.firstName,
          lastName: user.lastName,
        },
      });

      if (!response.ok()) {
        throw new Error(`Failed to create test user: ${await response.text()}`);
      }

      createdUsers.push(user.email);
      return user;
    };

    await use(createUser);
  },

  cleanupTestUsers: async ({ request, apiUrl }, use) => {
    // Cleanup is handled by test database reset or admin cleanup endpoint
    await use(async () => {
      // In a real implementation, call an admin endpoint to clean up test users
      // or use a separate test database that gets reset
    });
  },
});

export { expect };
```

---

### Task AUTH-022: User Registration E2E Tests

**Priority:** Critical
**Estimate:** 4 hours

**Implementation:**

```typescript
// tests/e2e/tests/auth/registration.spec.ts
import { test, expect } from '../../fixtures/auth.fixture';

test.describe('User Registration', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/register');
  });

  test('AUTH-E2E-001: new user can register with valid credentials', async ({ page }) => {
    const uniqueEmail = `test-${Date.now()}@example.com`;

    // Fill registration form
    await page.fill('[data-testid="email-input"]', uniqueEmail);
    await page.fill('[data-testid="password-input"]', 'SecurePass123!');
    await page.fill('[data-testid="confirm-password-input"]', 'SecurePass123!');
    await page.fill('[data-testid="first-name-input"]', 'Test');
    await page.fill('[data-testid="last-name-input"]', 'User');

    // Submit form
    await page.click('[data-testid="register-button"]');

    // Should redirect to dashboard
    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });

    // Verify user is logged in
    await expect(page.getByText('Welcome, Test')).toBeVisible();
  });

  test('AUTH-E2E-002: registration fails with invalid email format', async ({ page }) => {
    await page.fill('[data-testid="email-input"]', 'invalid-email');
    await page.fill('[data-testid="password-input"]', 'SecurePass123!');
    await page.fill('[data-testid="confirm-password-input"]', 'SecurePass123!');

    await page.click('[data-testid="register-button"]');

    // Should show validation error
    await expect(page.getByText('Please enter a valid email')).toBeVisible();

    // Should remain on registration page
    await expect(page).toHaveURL('/register');
  });

  test('AUTH-E2E-003: registration fails with weak password', async ({ page }) => {
    const uniqueEmail = `test-${Date.now()}@example.com`;

    await page.fill('[data-testid="email-input"]', uniqueEmail);
    await page.fill('[data-testid="password-input"]', 'weak');
    await page.fill('[data-testid="confirm-password-input"]', 'weak');

    await page.click('[data-testid="register-button"]');

    // Should show password requirements error
    await expect(page.getByText(/Password must be at least 8 characters/)).toBeVisible();
  });

  test('AUTH-E2E-004: registration fails with mismatched passwords', async ({ page }) => {
    const uniqueEmail = `test-${Date.now()}@example.com`;

    await page.fill('[data-testid="email-input"]', uniqueEmail);
    await page.fill('[data-testid="password-input"]', 'SecurePass123!');
    await page.fill('[data-testid="confirm-password-input"]', 'DifferentPass123!');

    await page.click('[data-testid="register-button"]');

    // Should show password mismatch error
    await expect(page.getByText('Passwords do not match')).toBeVisible();
  });

  test('AUTH-E2E-005: registration fails with duplicate email', async ({ page, createTestUser }) => {
    // First create a user via API
    const existingUser = await createTestUser();

    // Try to register with same email
    await page.fill('[data-testid="email-input"]', existingUser.email);
    await page.fill('[data-testid="password-input"]', 'SecurePass123!');
    await page.fill('[data-testid="confirm-password-input"]', 'SecurePass123!');
    await page.fill('[data-testid="first-name-input"]', 'Another');
    await page.fill('[data-testid="last-name-input"]', 'User');

    await page.click('[data-testid="register-button"]');

    // Should show duplicate email error
    await expect(page.getByText(/email already/i)).toBeVisible();
  });

  test('AUTH-E2E-006: registration stores user data correctly', async ({ page, request, apiUrl }) => {
    const uniqueEmail = `test-${Date.now()}@example.com`;
    const firstName = 'John';
    const lastName = 'Doe';
    const phone = '+1-555-123-4567';

    // Register via UI
    await page.fill('[data-testid="email-input"]', uniqueEmail);
    await page.fill('[data-testid="password-input"]', 'SecurePass123!');
    await page.fill('[data-testid="confirm-password-input"]', 'SecurePass123!');
    await page.fill('[data-testid="first-name-input"]', firstName);
    await page.fill('[data-testid="last-name-input"]', lastName);
    await page.fill('[data-testid="phone-input"]', phone);

    await page.click('[data-testid="register-button"]');

    // Wait for redirect
    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });

    // Get the stored token from the page context
    const accessToken = await page.evaluate(() => {
      // Access token should be available in the app's state
      return localStorage.getItem('user');
    });

    expect(accessToken).toBeTruthy();

    // Verify user data via profile page
    await page.goto('/profile');

    await expect(page.locator('[data-testid="first-name-input"]')).toHaveValue(firstName);
    await expect(page.locator('[data-testid="last-name-input"]')).toHaveValue(lastName);
    await expect(page.locator('[data-testid="phone-input"]')).toHaveValue(phone);
  });
});
```

---

### Task AUTH-023: User Login E2E Tests

**Priority:** Critical
**Estimate:** 4 hours

**Implementation:**

```typescript
// tests/e2e/tests/auth/login.spec.ts
import { test, expect } from '../../fixtures/auth.fixture';

test.describe('User Login', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
  });

  test('AUTH-E2E-010: registered user can login successfully', async ({ page, createTestUser }) => {
    // Create a user first
    const user = await createTestUser({
      firstName: 'Login',
      lastName: 'Test',
    });

    // Login via UI
    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', user.password);
    await page.click('[data-testid="login-button"]');

    // Should redirect to dashboard
    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });

    // Verify user info displayed
    await expect(page.getByText(`Welcome, ${user.firstName}`)).toBeVisible();
  });

  test('AUTH-E2E-011: login fails with incorrect password', async ({ page, createTestUser }) => {
    const user = await createTestUser();

    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', 'WrongPassword123!');
    await page.click('[data-testid="login-button"]');

    // Should show error message
    await expect(page.getByText(/Invalid email or password/i)).toBeVisible();

    // Should remain on login page
    await expect(page).toHaveURL('/login');
  });

  test('AUTH-E2E-012: login fails with non-existent email', async ({ page }) => {
    await page.fill('[data-testid="email-input"]', 'nonexistent@example.com');
    await page.fill('[data-testid="password-input"]', 'SomePassword123!');
    await page.click('[data-testid="login-button"]');

    // Should show generic error (not revealing if email exists)
    await expect(page.getByText(/Invalid email or password/i)).toBeVisible();
  });

  test('AUTH-E2E-013: account lockout after multiple failed attempts', async ({ page, createTestUser }) => {
    const user = await createTestUser();

    // Attempt to login with wrong password 5 times
    for (let i = 0; i < 5; i++) {
      await page.fill('[data-testid="email-input"]', user.email);
      await page.fill('[data-testid="password-input"]', 'WrongPassword123!');
      await page.click('[data-testid="login-button"]');
      await page.waitForTimeout(500); // Wait between attempts
    }

    // 6th attempt should show lockout message
    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', user.password); // Even with correct password
    await page.click('[data-testid="login-button"]');

    await expect(page.getByText(/Account locked|Too many failed/i)).toBeVisible();
  });

  test('AUTH-E2E-014: login redirects to requested page after authentication', async ({ page, createTestUser }) => {
    const user = await createTestUser();

    // Try to access protected page
    await page.goto('/profile');

    // Should redirect to login with returnUrl
    await expect(page).toHaveURL(/\/login\?returnUrl=/);

    // Login
    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', user.password);
    await page.click('[data-testid="login-button"]');

    // Should redirect to originally requested page
    await expect(page).toHaveURL('/profile', { timeout: 10000 });
  });

  test('AUTH-E2E-015: email field is case insensitive', async ({ page, createTestUser }) => {
    const user = await createTestUser({
      email: `test-${Date.now()}@example.com`,
    });

    // Login with different case
    await page.fill('[data-testid="email-input"]', user.email.toUpperCase());
    await page.fill('[data-testid="password-input"]', user.password);
    await page.click('[data-testid="login-button"]');

    // Should login successfully
    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });
  });

  test('AUTH-E2E-016: user session persists after page refresh', async ({ page, createTestUser }) => {
    const user = await createTestUser({ firstName: 'Persist' });

    // Login
    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', user.password);
    await page.click('[data-testid="login-button"]');

    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });

    // Refresh page
    await page.reload();

    // Should still be logged in (token refresh should work)
    await expect(page.getByText(`Welcome, ${user.firstName}`)).toBeVisible();
  });
});
```

---

### Task AUTH-024: Logout E2E Tests

**Priority:** High
**Estimate:** 2 hours

**Implementation:**

```typescript
// tests/e2e/tests/auth/logout.spec.ts
import { test, expect } from '../../fixtures/auth.fixture';

test.describe('User Logout', () => {
  test('AUTH-E2E-020: user can logout successfully', async ({ page, createTestUser }) => {
    const user = await createTestUser();

    // Login first
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', user.password);
    await page.click('[data-testid="login-button"]');

    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });

    // Logout
    await page.click('[data-testid="user-menu-button"]');
    await page.click('[data-testid="logout-button"]');

    // Should redirect to login
    await expect(page).toHaveURL('/login', { timeout: 5000 });
  });

  test('AUTH-E2E-021: logout clears session and prevents access to protected routes', async ({ page, createTestUser }) => {
    const user = await createTestUser();

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', user.password);
    await page.click('[data-testid="login-button"]');

    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });

    // Logout
    await page.click('[data-testid="user-menu-button"]');
    await page.click('[data-testid="logout-button"]');

    // Try to access protected route
    await page.goto('/profile');

    // Should redirect to login
    await expect(page).toHaveURL(/\/login/);
  });

  test('AUTH-E2E-022: logout invalidates refresh token', async ({ page, createTestUser, request, apiUrl }) => {
    const user = await createTestUser();

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', user.password);
    await page.click('[data-testid="login-button"]');

    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });

    // Get cookies before logout
    const cookiesBefore = await page.context().cookies();
    const refreshTokenBefore = cookiesBefore.find(c => c.name === 'refreshToken');

    // Logout
    await page.click('[data-testid="user-menu-button"]');
    await page.click('[data-testid="logout-button"]');

    await expect(page).toHaveURL('/login', { timeout: 5000 });

    // Try to use the old refresh token (should fail)
    if (refreshTokenBefore) {
      const response = await request.post(`${apiUrl}/auth/refresh`, {
        headers: {
          Cookie: `refreshToken=${refreshTokenBefore.value}`,
        },
      });

      expect(response.status()).toBe(401);
    }
  });
});
```

---

### Task AUTH-025: Password Reset E2E Tests

**Priority:** High
**Estimate:** 3 hours

**Implementation:**

```typescript
// tests/e2e/tests/auth/password-reset.spec.ts
import { test, expect } from '../../fixtures/auth.fixture';

test.describe('Password Reset', () => {
  test('AUTH-E2E-030: forgot password form submits successfully', async ({ page, createTestUser }) => {
    const user = await createTestUser();

    await page.goto('/forgot-password');

    await page.fill('[data-testid="email-input"]', user.email);
    await page.click('[data-testid="submit-button"]');

    // Should show success message (even if email doesn't exist for security)
    await expect(page.getByText(/reset link has been sent/i)).toBeVisible();
  });

  test('AUTH-E2E-031: forgot password shows same message for non-existent email', async ({ page }) => {
    await page.goto('/forgot-password');

    await page.fill('[data-testid="email-input"]', 'nonexistent@example.com');
    await page.click('[data-testid="submit-button"]');

    // Should show same message (prevents email enumeration)
    await expect(page.getByText(/reset link has been sent/i)).toBeVisible();
  });

  // Note: Full password reset flow test requires email access
  // In a real test environment, you would either:
  // 1. Use a test email service (like Mailhog)
  // 2. Query the database directly for the reset token
  // 3. Use a special test endpoint that returns the token

  test('AUTH-E2E-032: reset password page validates token', async ({ page }) => {
    // Access reset page with invalid token
    await page.goto('/reset-password?token=invalid-token');

    await page.fill('[data-testid="new-password-input"]', 'NewSecurePass123!');
    await page.fill('[data-testid="confirm-password-input"]', 'NewSecurePass123!');
    await page.click('[data-testid="submit-button"]');

    // Should show error for invalid token
    await expect(page.getByText(/Invalid or expired/i)).toBeVisible();
  });

  test('AUTH-E2E-033: reset password enforces password requirements', async ({ page }) => {
    await page.goto('/reset-password?token=some-token');

    await page.fill('[data-testid="new-password-input"]', 'weak');
    await page.fill('[data-testid="confirm-password-input"]', 'weak');
    await page.click('[data-testid="submit-button"]');

    // Should show password requirements error
    await expect(page.getByText(/Password must be at least 8 characters/)).toBeVisible();
  });
});
```

---

### Task AUTH-026: Profile Management E2E Tests

**Priority:** High
**Estimate:** 3 hours

**Implementation:**

```typescript
// tests/e2e/tests/auth/profile.spec.ts
import { test, expect } from '../../fixtures/auth.fixture';

test.describe('Profile Management', () => {
  test('AUTH-E2E-040: user can view their profile', async ({ page, createTestUser }) => {
    const user = await createTestUser({
      firstName: 'Profile',
      lastName: 'Test',
    });

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', user.password);
    await page.click('[data-testid="login-button"]');

    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });

    // Go to profile
    await page.goto('/profile');

    // Verify profile data is displayed
    await expect(page.locator('[data-testid="first-name-input"]')).toHaveValue(user.firstName);
    await expect(page.locator('[data-testid="last-name-input"]')).toHaveValue(user.lastName);
  });

  test('AUTH-E2E-041: user can update their profile', async ({ page, createTestUser }) => {
    const user = await createTestUser();

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', user.password);
    await page.click('[data-testid="login-button"]');

    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });

    // Go to profile
    await page.goto('/profile');

    // Update profile
    await page.fill('[data-testid="first-name-input"]', 'Updated');
    await page.fill('[data-testid="last-name-input"]', 'Name');
    await page.fill('[data-testid="phone-input"]', '+1-555-987-6543');
    await page.click('[data-testid="save-profile-button"]');

    // Should show success message
    await expect(page.getByText(/Profile updated/i)).toBeVisible();

    // Refresh and verify data persisted
    await page.reload();
    await expect(page.locator('[data-testid="first-name-input"]')).toHaveValue('Updated');
    await expect(page.locator('[data-testid="last-name-input"]')).toHaveValue('Name');
    await expect(page.locator('[data-testid="phone-input"]')).toHaveValue('+1-555-987-6543');
  });

  test('AUTH-E2E-042: user can change their password', async ({ page, createTestUser }) => {
    const user = await createTestUser();
    const newPassword = 'NewSecurePass456!';

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', user.password);
    await page.click('[data-testid="login-button"]');

    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });

    // Go to profile
    await page.goto('/profile');

    // Change password
    await page.fill('[data-testid="current-password-input"]', user.password);
    await page.fill('[data-testid="new-password-input"]', newPassword);
    await page.fill('[data-testid="confirm-new-password-input"]', newPassword);
    await page.click('[data-testid="change-password-button"]');

    // Should show success message
    await expect(page.getByText(/Password changed/i)).toBeVisible();

    // Logout and login with new password
    await page.click('[data-testid="user-menu-button"]');
    await page.click('[data-testid="logout-button"]');

    await expect(page).toHaveURL('/login', { timeout: 5000 });

    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', newPassword);
    await page.click('[data-testid="login-button"]');

    // Should login successfully with new password
    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });
  });

  test('AUTH-E2E-043: change password fails with incorrect current password', async ({ page, createTestUser }) => {
    const user = await createTestUser();

    // Login
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', user.password);
    await page.click('[data-testid="login-button"]');

    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });

    // Go to profile
    await page.goto('/profile');

    // Try to change password with wrong current password
    await page.fill('[data-testid="current-password-input"]', 'WrongPassword123!');
    await page.fill('[data-testid="new-password-input"]', 'NewSecurePass456!');
    await page.fill('[data-testid="confirm-new-password-input"]', 'NewSecurePass456!');
    await page.click('[data-testid="change-password-button"]');

    // Should show error
    await expect(page.getByText(/Current password is incorrect/i)).toBeVisible();
  });
});
```

---

### Task AUTH-027: Admin User Management E2E Tests

**Priority:** Medium
**Estimate:** 3 hours

**Implementation:**

```typescript
// tests/e2e/tests/admin/users.spec.ts
import { test, expect } from '../../fixtures/auth.fixture';

test.describe('Admin User Management', () => {
  // Helper to login as admin
  async function loginAsAdmin(page, request, apiUrl) {
    // Create or use predefined admin user
    // In a real scenario, you'd have a seeded admin user or create one via API
    const adminEmail = 'admin@climasite.com';
    const adminPassword = 'AdminPass123!';

    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', adminEmail);
    await page.fill('[data-testid="password-input"]', adminPassword);
    await page.click('[data-testid="login-button"]');

    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });
  }

  test('AUTH-E2E-050: admin can view user list', async ({ page, request, apiUrl }) => {
    await loginAsAdmin(page, request, apiUrl);

    await page.goto('/admin/users');

    // Should see user list
    await expect(page.locator('[data-testid="users-table"]')).toBeVisible();
    await expect(page.locator('[data-testid="user-row"]').first()).toBeVisible();
  });

  test('AUTH-E2E-051: admin can search users', async ({ page, request, apiUrl, createTestUser }) => {
    const user = await createTestUser({ firstName: 'Searchable' });

    await loginAsAdmin(page, request, apiUrl);

    await page.goto('/admin/users');

    // Search for the user
    await page.fill('[data-testid="search-input"]', 'Searchable');
    await page.click('[data-testid="search-button"]');

    // Should find the user
    await expect(page.getByText('Searchable')).toBeVisible();
  });

  test('AUTH-E2E-052: admin can change user role', async ({ page, request, apiUrl, createTestUser }) => {
    const user = await createTestUser();

    await loginAsAdmin(page, request, apiUrl);

    await page.goto('/admin/users');

    // Find and click on the user
    await page.fill('[data-testid="search-input"]', user.email);
    await page.click('[data-testid="search-button"]');

    // Open user details
    await page.click(`[data-testid="user-row-${user.email}"]`);

    // Change role to Admin
    await page.selectOption('[data-testid="role-select"]', 'Admin');
    await page.click('[data-testid="save-role-button"]');

    // Should show success
    await expect(page.getByText(/Role updated/i)).toBeVisible();
  });

  test('AUTH-E2E-053: non-admin cannot access admin routes', async ({ page, createTestUser }) => {
    const user = await createTestUser(); // Regular customer

    // Login as regular user
    await page.goto('/login');
    await page.fill('[data-testid="email-input"]', user.email);
    await page.fill('[data-testid="password-input"]', user.password);
    await page.click('[data-testid="login-button"]');

    await expect(page).toHaveURL('/dashboard', { timeout: 10000 });

    // Try to access admin route
    await page.goto('/admin/users');

    // Should be redirected to unauthorized page
    await expect(page).toHaveURL('/unauthorized');
  });
});
```

---

## 7. Security Checklist

### Authentication Security
- [ ] Passwords hashed with Argon2id (OWASP parameters)
- [ ] JWT tokens use HS256 or RS256
- [ ] Access tokens expire in 15 minutes
- [ ] Refresh tokens stored in httpOnly cookies
- [ ] Refresh token rotation implemented
- [ ] Token reuse detection (revoke all tokens on detection)
- [ ] Account lockout after 5 failed attempts

### API Security
- [ ] Rate limiting on authentication endpoints
- [ ] CORS properly configured
- [ ] HTTPS enforced in production
- [ ] SQL injection prevention (parameterized queries)
- [ ] Input validation with FluentValidation

### Session Security
- [ ] Session timeout after inactivity
- [ ] Secure cookie flags set (HttpOnly, Secure, SameSite)
- [ ] CSRF protection enabled

### Audit & Monitoring
- [ ] All authentication events logged
- [ ] Failed login attempts tracked
- [ ] Password changes logged
- [ ] Suspicious activity alerts configured

---

## 8. Configuration

### appsettings.json

```json
{
  "Jwt": {
    "Secret": "your-256-bit-secret-key-here-minimum-32-characters",
    "Issuer": "ClimaSite",
    "Audience": "ClimaSite",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Security": {
    "MaxFailedLoginAttempts": 5,
    "LockoutDurationMinutes": 15,
    "PasswordResetTokenExpirationHours": 1
  },
  "RateLimiting": {
    "Login": {
      "PermitLimit": 5,
      "Window": 60
    },
    "Registration": {
      "PermitLimit": 3,
      "Window": 60
    },
    "PasswordReset": {
      "PermitLimit": 3,
      "Window": 300
    }
  }
}
```

### Environment Variables (Production)

```bash
JWT_SECRET=<production-secret-minimum-256-bits>
DATABASE_URL=<postgresql-connection-string>
SMTP_HOST=<smtp-host>
SMTP_PORT=<smtp-port>
SMTP_USER=<smtp-user>
SMTP_PASSWORD=<smtp-password>
```

---

## 9. Dependencies

### NuGet Packages

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
<PackageReference Include="Konscious.Security.Cryptography.Argon2" Version="1.3.0" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
```

### NPM Packages

```json
{
  "@angular/core": "^19.0.0",
  "rxjs": "^7.8.0"
}
```

### Dev Dependencies

```json
{
  "@playwright/test": "^1.40.0"
}
```

---

## 10. Estimated Timeline

| Phase | Tasks | Duration |
|-------|-------|----------|
| Database Setup | AUTH-001 | 2 hours |
| Core Security | AUTH-002, AUTH-003 | 6 hours |
| Auth Endpoints | AUTH-004 to AUTH-013 | 24 hours |
| Frontend Components | AUTH-014 to AUTH-020 | 20 hours |
| E2E Tests | AUTH-021 to AUTH-027 | 21 hours |
| Testing & QA | Manual testing, bug fixes | 8 hours |
| **Total** | | **~81 hours** |

---

## 11. Acceptance Criteria Summary

The authentication system is complete when:

1. Users can register with email/password
2. Users can login and receive JWT tokens
3. Access tokens refresh automatically before expiry
4. Users can logout (refresh token revoked)
5. Users can reset forgotten passwords via email
6. Users can change their password
7. Users can view and update their profile
8. Admins can view and manage all users
9. Role-based access control works correctly
10. All E2E tests pass without mocking
11. Security checklist items are verified
12. No critical or high vulnerabilities in security scan
