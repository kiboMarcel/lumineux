using FluentAssertions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Xunit;

namespace Lumineux.Domain.Tests;

public sealed class BureauProfileTests
{
    private sealed class FakeCatalog : IPermissionCatalog
    {
        private static readonly HashSet<string> Known =
            new(StringComparer.Ordinal) { "manage_attendance", "manage_members", "manage_bureau_profiles" };

        public bool Contains(string permission) => Known.Contains(permission);

        public IReadOnlyList<PermissionDescriptor> All() =>
            Known.Select(k => new PermissionDescriptor(k, k)).ToList();
    }

    private readonly IPermissionCatalog _catalog = new FakeCatalog();

    [Fact]
    public void Create_valid_sets_name_and_normalized_and_permissions()
    {
        var profile = BureauProfile.Create("Gestion des présences", "Sessions",
            new[] { "manage_attendance", "manage_attendance" }, _catalog);

        profile.Name.Should().Be("Gestion des présences");
        profile.NameNormalized.Should().Be("gestion des présences");
        profile.Description.Should().Be("Sessions");
        profile.Permissions.Should().ContainSingle().Which.Permission.Should().Be("manage_attendance");
    }

    [Fact]
    public void Create_empty_name_throws()
    {
        var act = () => BureauProfile.Create("  ", null, Array.Empty<string>(), _catalog);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_name_too_long_throws()
    {
        var longName = new string('A', BureauProfile.NameMaxLength + 1);

        var act = () => BureauProfile.Create(longName, null, Array.Empty<string>(), _catalog);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetPermissions_with_unknown_permission_throws()
    {
        var profile = BureauProfile.Create("P", null, Array.Empty<string>(), _catalog);

        var act = () => profile.SetPermissions(new[] { "unknown_right" }, _catalog);

        act.Should().Throw<DomainException>().WithMessage("*unknown_right*");
    }

    [Fact]
    public void SetPermissions_deduplicates()
    {
        var profile = BureauProfile.Create("P", null,
            new[] { "manage_attendance", "manage_members", "manage_attendance" }, _catalog);

        profile.Permissions.Select(p => p.Permission).Should()
            .BeEquivalentTo(new[] { "manage_attendance", "manage_members" });
    }

    [Fact]
    public void Rename_updates_normalized()
    {
        var profile = BureauProfile.Create("Initial", null, Array.Empty<string>(), _catalog);

        profile.Rename("Nouveau NOM");

        profile.Name.Should().Be("Nouveau NOM");
        profile.NameNormalized.Should().Be("nouveau nom");
    }

    [Fact]
    public void UpdateDescription_too_long_throws()
    {
        var profile = BureauProfile.Create("P", null, Array.Empty<string>(), _catalog);
        var tooLong = new string('X', BureauProfile.DescriptionMaxLength + 1);

        var act = () => profile.UpdateDescription(tooLong);

        act.Should().Throw<DomainException>();
    }
}
