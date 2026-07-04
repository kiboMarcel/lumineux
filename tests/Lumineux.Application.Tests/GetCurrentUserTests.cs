using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Auth;
using Lumineux.Domain.Abstractions;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>
/// Tests unitaires du cas d'usage « profil de session » (feature 007, US1, T003). Vérifient le
/// mapping identité + droits depuis <see cref="ICurrentUser"/>, la gestion d'une liste de droits
/// vide, et la garde défensive (contexte sans membre → refus 401 journalisé).
/// </summary>
public sealed class GetCurrentUserTests
{
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private GetCurrentUserHandler CreateHandler() => new(_user, _audit);

    [Fact]
    public void Returns_identity_and_effective_permissions()
    {
        _user.MemberId.Returns(42);
        _user.UserName.Returns("Jane Doe");
        _user.Permissions.Returns(new[] { "manage_members", "manage_attendance" });

        var response = CreateHandler().Handle();

        response.MemberId.Should().Be(42);
        response.DisplayName.Should().Be("Jane Doe");
        response.Permissions.Should().BeEquivalentTo("manage_members", "manage_attendance");
    }

    [Fact]
    public void Returns_empty_permissions_when_user_has_no_rights()
    {
        _user.MemberId.Returns(108);
        _user.UserName.Returns("John Roe");
        _user.Permissions.Returns(Array.Empty<string>());

        var response = CreateHandler().Handle();

        response.MemberId.Should().Be(108);
        response.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void Missing_member_context_throws_unauthorized_and_is_audited()
    {
        _user.MemberId.Returns((int?)null);

        var act = () => CreateHandler().Handle();

        act.Should().Throw<UnauthorizedException>();
        _audit.Received().Refused("CurrentUser", Arg.Any<string>());
    }
}
