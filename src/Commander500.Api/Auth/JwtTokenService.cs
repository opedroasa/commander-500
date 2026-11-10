using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Commander500.Api.Contracts;
using Commander500.Api.Data;
using Commander500.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Commander500.Api.Auth;

public class JwtOptions
{
    public const string Section = "Jwt";
    /// <summary>HMAC key, at least 32 characters. Set via env var Jwt__Key in production.</summary>
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "commander500";
    public string Audience { get; set; } = "commander500";
    public int ExpiresHours { get; set; } = 12;

    public SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(Key));
}

public class AuthService(AppDbContext db, IPasswordHasher<AdminUser> hasher, IOptions<JwtOptions> options, TimeProvider clock)
{
    public async Task<LoginResponse?> Login(LoginRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var user = await db.AdminUsers.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null) return null;

        var check = hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password ?? "");
        if (check == PasswordVerificationResult.Failed) return null;
        if (check == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, req.Password!);
            await db.SaveChangesAsync();
        }

        var o = options.Value;
        var expires = clock.GetUtcNow().UtcDateTime.AddHours(o.ExpiresHours);
        var token = new JwtSecurityToken(
            issuer: o.Issuer,
            audience: o.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, "Admin"),
            ],
            expires: expires,
            signingCredentials: new SigningCredentials(o.SigningKey(), SecurityAlgorithms.HmacSha256));

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, user.Email);
    }
}
