using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>
/// Someone asks to remove a guest from <paramref name="groupName"/>. The guest is named (any remembered
/// <see cref="GroupGuest"/>, even one who joined a different group — that is how the "wrong group" case is built),
/// or <see cref="ANonExistentParticipant"/> for a guest session nobody ever created. The outcome is left as the
/// scenario's last response; a refusal is not an error here. When the removal succeeds the guest is marked removed,
/// so "every guest of the group" stops counting them.
/// </summary>
public sealed class RemoveAParticipant(string? guestName, Requester requester = Requester.Owner, string groupName = GroupNames.TheGroup) : ITask
{
    public static RemoveAParticipant ANonExistentParticipant() => new(guestName: null);

    public async Task PerformAsAsync(Actor actor)
    {
        var group = actor.Recall<GroupBooking>(groupName);
        var guest = guestName is null ? null : actor.Recall<GroupGuest>(guestName);
        var sessionId = guest?.SessionId ?? Guid.NewGuid().ToString();
        var token = await Credentials.OfAsync(actor, requester, group);

        var response = actor.TheLastResponse();
        await response.CaptureAsync(
            await new GroupPreOrderApi(actor.UsingTheApi()).RemoveParticipantAsync(group.GroupReservationId, sessionId, token));

        if (response.Succeeded && guest is not null && guest.GroupName == groupName)
            guest.MarkRemoved();
    }
}
