using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrder;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>The participant list the last request returned (looking at the group, or the snapshot a removal returns).
/// Fails with the status and body if that request was refused.</summary>
public sealed class TheParticipantsShown : IQuestion<GroupParticipantsResponse>
{
    public Task<GroupParticipantsResponse> AnsweredByAsync(Actor actor) =>
        Task.FromResult(actor.TheLastResponse().Json<GroupParticipantsResponse>());
}

/// <summary>The group's participants as its owner sees them right now — a fresh look, which leaves the scenario's last
/// response alone, so a step can check the group after a refused request without losing that refusal.</summary>
public sealed class TheGroupAsTheOwnerSeesItNow(string groupName = GroupNames.TheGroup) : IQuestion<GroupParticipantsResponse>
{
    public async Task<GroupParticipantsResponse> AnsweredByAsync(Actor actor)
    {
        var group = actor.Recall<GroupBooking>(groupName);
        var owner = actor.Recall<CustomerAccount>(group.OwnerRole);
        var response = await new GroupPreOrderApi(actor.UsingTheApi()).GetParticipantsAsync(group.GroupReservationId, owner.IdToken);
        if (!response.Ok)
            throw new InvalidOperationException(
                $"The owner could not look at {groupName}: {response.Status} {await response.ReadErrorMessageAsync()}");

        return await response.ReadAsAsync<GroupParticipantsResponse>();
    }
}

/// <summary>The guests this scenario put into a group and has not removed since — the expected side of every check
/// on what the owner is shown.</summary>
public sealed class TheGuestsOfTheGroup(string groupName = GroupNames.TheGroup) : IQuestion<IReadOnlyList<GroupGuest>>
{
    public Task<IReadOnlyList<GroupGuest>> AnsweredByAsync(Actor actor)
    {
        IReadOnlyList<GroupGuest> guests = [.. actor.RecallAll<GroupGuest>().Where(g => g.GroupName == groupName && !g.WasRemoved)];
        return Task.FromResult(guests);
    }
}
