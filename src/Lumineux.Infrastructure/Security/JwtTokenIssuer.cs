using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lumineux.Infrastructure.Security;

/// <summary>
/// Émission des jetons d'accès JWT (feature 003), réutilisant les <see cref="JwtOptions"/> existants
/// (même signature/issuer/audience que la validation des features 001/002).
/// </summary>
public sealed class JwtTokenIssuer : ITokenIssuer
{
    private readonly JwtOptions _jwt;
    private readonly AuthOptions _auth;
    private readonly IClock _clock;

    public JwtTokenIssuer(IOptions<JwtOptions> jwt, IOptions<AuthOptions> auth, IClock clock)
    {
        _jwt = jwt.Value;
        _auth = auth.Value;
        _clock = clock;
    }

    public IssuedToken Issue(int memberId, string userName, IReadOnlyCollection<string> permissions)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(_auth.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new("member_id", memberId.ToString()),
            new(ClaimTypes.Name, userName),
        };
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
