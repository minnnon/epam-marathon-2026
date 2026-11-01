using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>A guest confirms their cart ("I'm done choosing"). A set-up task: throws if refused.</summary>
public sealed class SubmitTheCart(string guestName) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var guest = actor.Recall<GroupGuest>(guestName);
        var response = await new GroupPreOrderApi(actor.UsingTheApi()).SubmitCartAsync(guest.SessionId, guest.SessionToken);
        if (!response.Ok)
            throw new InvalidOperationException(
                $"{guestName} could not submit the cart: {response.Status} {await response.ReadErrorMessageAsync()}");

        guest.MarkSubmitted();
    }
}
