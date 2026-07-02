using FluentAssertions;
using Lumineux.Infrastructure.Security;
using Xunit;

namespace Lumineux.Infrastructure.Tests;

// Note : le service impl. IQrTokenService réside dans Infrastructure ; les tests unitaires
// (T020) sont donc placés dans Infrastructure.Tests plutôt qu'Application.Tests.
public sealed class QrTokenServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc);

    private readonly QrTokenService _service = new();

    [Fact]
    public void GetCurrentToken_returns_eight_digit_token()
    {
        var secret = _service.GenerateSecret();

        var token = _service.GetCurrentToken(secret, 30, Now);

        token.Token.Should().HaveLength(8).And.MatchRegex("^[0-9]{8}$");
        token.StepSeconds.Should().Be(30);
        token.ExpiresAt.Should().BeAfter(Now);
    }

    [Fact]
    public void Validate_accepts_current_token()
    {
        var secret = _service.GenerateSecret();
        var token = _service.GetCurrentToken(secret, 30, Now).Token;

        _service.Validate(secret, 30, token, Now).Should().BeTrue();
    }

    [Fact]
    public void Validate_rejects_wrong_token()
    {
        var secret = _service.GenerateSecret();

        _service.Validate(secret, 30, "00000001", Now).Should().BeFalse();
        _service.Validate(secret, 30, "not-a-token", Now).Should().BeFalse();
        _service.Validate(secret, 30, "", Now).Should().BeFalse();
    }

    [Fact]
    public void Validate_tolerates_one_step_drift_but_rejects_far_windows()
    {
        var secret = _service.GenerateSecret();
        var token = _service.GetCurrentToken(secret, 30, Now).Token;

        // Fenêtre suivante (dérive d'un pas) → toléré.
        _service.Validate(secret, 30, token, Now.AddSeconds(31)).Should().BeTrue();

        // Fenêtre lointaine → rejeté (photo périmée, FR-013a).
        _service.Validate(secret, 30, token, Now.AddSeconds(120)).Should().BeFalse();
    }

    [Fact]
    public void Token_changes_across_windows()
    {
        var secret = _service.GenerateSecret();

        var first = _service.GetCurrentToken(secret, 30, Now).Token;
        var later = _service.GetCurrentToken(secret, 30, Now.AddSeconds(60)).Token;

        first.Should().NotBe(later);
    }
}
