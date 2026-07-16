using Lumineux.Application.Contracts.Members;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.Members;

internal static class MemberMapping
{
    public static MemberResponse ToResponse(this Member m, MemberAccount? account = null) =>
        new(
            m.Id,
            m.Reference,
            m.EntryDate,
            m.LastName,
            m.FirstName,
            m.Gender,
            m.Mobile,
            m.Email,
            m.AntennaId,
            m.CivilityId,
            m.BirthDate,
            m.BirthPlaceId,
            m.BirthCityId,
            m.Address,
            m.DistrictId,
            m.NationalityId,
            m.IntroducerId,
            m.Profession,
            m.Status,
            account?.ActivationState.ToString() ?? Domain.Enums.AccountActivationState.PendingActivation.ToString());

    public static MemberListItem ToListItem(this Member m) =>
        new(m.Id, m.Reference, m.LastName, m.FirstName, m.Mobile, m.Email, m.AntennaId, m.Status);
}
