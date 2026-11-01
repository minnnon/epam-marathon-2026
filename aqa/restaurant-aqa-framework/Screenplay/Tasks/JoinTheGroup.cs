using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrder;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>A guest follows the invite and joins <paramref name="groupName"/> under their name — no account needed.
/// Remembers the <see cref="GroupGuest"/> (with its session token) under the guest's name. A set-up task: throws if
/// joining is refused. Guest sessions disappear with the group, so there is nothing extra to clean up.</summary>
public sealed class JoinTheGroup(string guestName, string groupName = GroupNames.TheGroup) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var group = actor.Recall<GroupBooking>(groupName);
        var response = await new GroupPreOrderApi(actor.UsingTheApi())
            .JoinAsync(new JoinGroupRequest(group.GroupReservationId, guestName));

        if (!response.Ok)
            throw new InvalidOperationException(
                $"{guestName} could not join {groupName}: {response.Status} {await response.ReadErrorMessageAsync()}");

        var session = await response.ReadAsAsync<GuestSessionResponse>();
        actor.Remember(guestName, new GroupGuest(guestName, groupName, session.SessionId, session.SessionToken));
    }
}
