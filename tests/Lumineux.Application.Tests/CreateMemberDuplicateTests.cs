using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Members;
using Lumineux.Application.Members;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class CreateMemberDuplicateTests
{
    private static readonly DateTime Now = new(2026, 7, 3, 9, 0, 0, DateTimeKind.Utc);

    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly IMemberAccountRepository _accounts = Substitute.For<IMemberAccountRepository>();
    private readonly IReferenceLookupRepository _lookup = Substitute.For<IReferenceLookupRepository>();
    private readonly IMemberReferenceGenerator _reference = Substitute.For<IMemberReferenceGenerator>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private CreateMemberHandler CreateHandler()
    {
        _clock.UtcNow.Returns(Now);
        _reference.NextAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns("LUM-2026-00001");
        _hasher.GenerateTemporaryPassword().Returns("Temp-1234");
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");
        _lookup.AntennaExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _user.HasPermission(Permissions.ManageMembers).Returns(true);
        _email.SendInvitationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(EmailSendOutcome.Sent);
        return new CreateMemberHandler(
            _members, _accounts, _lookup, _reference, _hasher, _email, _clock, _user, _audit, new CreateMemberValidator());
    }

    private static CreateMemberRequest Request(bool confirm = false) => new(
        "Doe", "Jane", "F", Mobile: null, Email: "jane@example.com", AntennaId: 1,
        CivilityId: null, BirthDate: null, BirthPlaceId: null, BirthCityId: null,
        Address: null, DistrictId: null, NationalityId: null, IntroducerId: null, ConfirmDuplicate: confirm);

    [Fact]
    public async Task Homonym_without_confirmation_throws_duplicate_name()
    {
        var handler = CreateHandler();
        _members.IsContactUsedByActiveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(false);
        _members.FindActiveByNameAsync("Doe", "Jane", Arg.Any<CancellationToken>())
            .Returns(new List<Member> { CreateExisting(5) });

        var act = () => handler.HandleAsync(Request(confirm: false));

        var ex = await act.Should().ThrowAsync<DuplicateMemberException>();
        ex.Which.Code.Should().Be("duplicate_name");
        ex.Which.DuplicateMemberIds.Should().Contain(5);
        await _members.DidNotReceive().AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Homonym_with_confirmation_creates_member()
    {
        var handler = CreateHandler();
        _members.IsContactUsedByActiveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await handler.HandleAsync(Request(confirm: true));

        result.LoginId.Should().Be("LUM-2026-00001");
        await _members.Received(1).AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>());
        await _members.DidNotReceive().FindActiveByNameAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Contact_used_by_active_throws_contact_in_use_even_with_confirmation()
    {
        var handler = CreateHandler();
        _members.IsContactUsedByActiveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(true);

        var act = () => handler.HandleAsync(Request(confirm: true));

        var ex = await act.Should().ThrowAsync<DuplicateMemberException>();
        ex.Which.Code.Should().Be("contact_in_use");
        await _members.DidNotReceive().AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>());
    }

    private static Member CreateExisting(int id)
    {
        var m = Member.Create("LUM-2026-09999", Now, "Doe", "Jane", "F", 1);
        m.Id = id;
        return m;
    }
}
