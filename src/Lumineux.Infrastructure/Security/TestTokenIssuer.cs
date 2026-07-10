using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lumineux.Application.Abstractions;
using Lumineux.Domain.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Lumineux.Infrastructure.Security;

/// <summary>
/// Émetteur de jetons JWT pour le développement et les tests uniquement (research §5).
/// **Non enregistré en production** (voir DependencyInjection, dette m1). L'émission définitive
/// des jetons relève de la fonctionnalité d'authentification (<see cref="JwtTokenIssuer"/>).
/// </summary>
public sealed class TestTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly AuthOptions _auth;
    private readonly IClock _clock;

    public TestTokenIssuer(IOptions<JwtOptions> options, IOptions<AuthOptions> auth, IClock clock)
    {
        _options = options.Value;
        _auth = auth.Value;
        _clock = clock;
    }

    public string Issue(int memberId, string userName, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new("member_id", memberId.ToString()),
            new(ClaimTypes.Name, userName),
        };
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = _clock.UtcNow;

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_auth.AccessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
