using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.BureauProfiles;
using Lumineux.Application.Contracts.BureauProfiles;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class CreateBureauProfileTests
{
    private readonly IBureauProfileRepository _profiles = Substitute.For<IBureauProfileRepository>();
    private readonly IPermissionCatalog _catalog = Substitute.For<IPermissionCatalog>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    public CreateBureauProfileTests()
    {
        _catalog.Contains("manage_attendance").Returns(true);
        _catalog.Contains("manage_members").Returns(true);
        _catalog.Contains("manage_bureau_profiles").Returns(true);
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(true);
    }

    private CreateBureauProfileHandler CreateHandler() =>
        new(_profiles, _catalog, _user, _audit, new BureauProfileWriteValidator());

    private static readonly BureauProfileWriteRequest Request =
        new("Gestion des présences", "Sessions", new[] { "manage_attendance" });

    [Fact]
    public async Task Create_valid_returns_detail()
    {
        _profiles.GetByNameNormalizedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((BureauProfile?)null);

        var result = await CreateHandler().HandleAsync(Request);

        result.Name.Should().Be(Request.Name);
        result.Permissions.Should().ContainSingle().Which.Should().Be("manage_attendance");
        await _profiles.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_duplicate_name_throws_conflict_duplicate_name()
    {
        var existing = BureauProfile.Create("Gestion des présences", null, Array.Empty<string>(), _catalog);
        _profiles.GetByNameNormalizedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(existing);

        var act = () => CreateHandler().HandleAsync(Request);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("duplicate_name");
    }

    [Fact]
    public async Task Create_unknown_permission_throws_domain_exception()
    {
        _profiles.GetByNameNormalizedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((BureauProfile?)null);

        var act = () => CreateHandler().HandleAsync(Request with { Permissions = new[] { "unknown" } });

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Create_without_manage_bureau_profiles_throws_forbidden()
    {
        _user.HasPermission(Permissions.ManageBureauProfiles).Returns(false);
        // Le validator passe donc on peut aller jusqu'à la vérification de droit dans le handler.

        var act = () => new CreateBureauProfileHandler(_profiles, _catalog, _user, _audit, new BureauProfileWriteValidator())
            .HandleAsync(Request);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Create_empty_name_fails_validation()
    {
        var act = () => CreateHandler().HandleAsync(Request with { Name = "" });

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
