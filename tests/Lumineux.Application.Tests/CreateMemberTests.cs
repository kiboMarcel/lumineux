using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Members;
using Lumineux.Application.Members;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class CreateMemberTests
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
        _hasher.Hash("Temp-1234").Returns("hashed");
        _lookup.AntennaExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        return new CreateMemberHandler(
            _members, _accounts, _lookup, _reference, _hasher, _email, _clock, _user, _audit, new CreateMemberValidator());
    }

    private void GivenManager() => _user.HasPermission(Permissions.ManageMembers).Returns(true);

    private static CreateMemberRequest WithEmail => new(
        "Doe", "Jane", "F", Mobile: null, Email: "jane@example.com", AntennaId: 1,
        CivilityId: null, BirthDate: null, BirthPlaceId: null, BirthCityId: null,
        Address: null, DistrictId: null, NationalityId: null, IntroducerId: null);

    [Fact]
    public async Task Create_with_email_sends_invitation_and_persists_atomically()
    {
        GivenManager();
        _email.SendInvitationAsync("jane@example.com", "LUM-2026-00001", "Temp-1234", Arg.Any<CancellationToken>())
            .Returns(EmailSendOutcome.Sent);

        var result = await CreateHandler().HandleAsync(WithEmail);

        result.CredentialsDelivery.Should().Be(CredentialsDelivery.EmailSent);
        result.TemporaryPassword.Should().BeNull(); // pas de mot de passe exposé quand l'e-mail part
        result.LoginId.Should().Be("LUM-2026-00001");
        await _members.Received(1).AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>());
        await _accounts.Received(1).AddAsync(Arg.Any<MemberAccount>(), Arg.Any<CancellationToken>());
        await _members.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_without_email_falls_back_to_bureau_handout()
    {
        GivenManager();
        var request = new CreateMemberRequest(
            "Traore", "Ali", "M", Mobile: "+2250700000000", Email: null, AntennaId: 1,
            null, null, null, null, null, null, null, null);
        _email.SendInvitationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(EmailSendOutcome.NoRecipient);

        var result = await CreateHandler().HandleAsync(request);

        result.CredentialsDelivery.Should().Be(CredentialsDelivery.BureauHandout);
        result.TemporaryPassword.Should().Be("Temp-1234"); // remis une fois au bureau
    }

    [Fact]
    public async Task Create_when_email_send_fails_falls_back_to_bureau_handout()
    {
        GivenManager();
        _email.SendInvitationAsync("jane@example.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(EmailSendOutcome.Failed);

        var result = await CreateHandler().HandleAsync(WithEmail);

        result.CredentialsDelivery.Should().Be(CredentialsDelivery.BureauHandout);
        result.TemporaryPassword.Should().Be("Temp-1234");
    }

    [Fact]
    public async Task Create_without_permission_throws_forbidden()
    {
        _user.HasPermission(Permissions.ManageMembers).Returns(false);

        var act = () => CreateHandler().HandleAsync(WithEmail);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _members.DidNotReceive().AddAsync(Arg.Any<Member>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_with_unknown_antenna_throws_not_found()
    {
        GivenManager();
        var handler = CreateHandler();
        _lookup.AntennaExistsAsync(1, Arg.Any<CancellationToken>()).Returns(false); // après CreateHandler (qui stubbe true)

        var act = () => handler.HandleAsync(WithEmail);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_without_any_contact_fails_validation()
    {
        GivenManager();
        var request = new CreateMemberRequest(
            "Doe", "Jane", "F", Mobile: null, Email: null, AntennaId: 1,
            null, null, null, null, null, null, null, null);

        var act = () => CreateHandler().HandleAsync(request);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
