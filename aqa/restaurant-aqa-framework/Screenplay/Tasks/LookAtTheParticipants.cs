using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>
/// Someone asks to see who joined a group and what they ordered. The outcome — the list, or a refusal — is left as
/// the scenario's last response; this task never throws on a refusal, because who is refused is what several
/// scenarios check. <see cref="OfAGroupThatDoesNotExist"/> asks about a group id nobody ever created.
/// </summary>
public sealed class LookAtTheParticipants(Requester requester, string groupName = GroupNames.TheGroup, bool ofAMissingGroup = false) : ITask
{
    public static LookAtTheParticipants OfAGroupThatDoesNotExist() => new(Requester.Owner, ofAMissingGroup: true);

    public async Task PerformAsAsync(Actor actor)
    {
        var group = actor.Recall<GroupBooking>(groupName);
        var token = await Credentials.OfAsync(actor, requester, group);
        var groupId = ofAMissingGroup ? Guid.NewGuid().ToString() : group.GroupReservationId;

        await actor.TheLastResponse().CaptureAsync(
            await new GroupPreOrderApi(actor.UsingTheApi()).GetParticipantsAsync(groupId, token));
    }
}
