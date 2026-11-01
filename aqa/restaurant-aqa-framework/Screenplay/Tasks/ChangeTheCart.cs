using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrder;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>A guest adds one more portion of a dish already in the cart (<paramref name="portions"/> = 1) or takes one
/// out (−1). Remembers a <see cref="CartChange"/> with the amount the guest's subtotal moved by. Throws if refused.</summary>
public sealed class ChangeTheCart(string guestName, int portions) : ITask
{
    public static ChangeTheCart AddAPortion(string guestName) => new(guestName, 1);

    public static ChangeTheCart TakeOutAPortion(string guestName) => new(guestName, -1);

    public async Task PerformAsAsync(Actor actor)
    {
        var guest = actor.Recall<GroupGuest>(guestName);
        var line = guest.Cart.FirstOrDefault()
            ?? throw new InvalidOperationException($"{guestName}'s cart is empty, so there is no portion to change.");

        var api = new GroupPreOrderApi(actor.UsingTheApi());
        var request = new GuestDishRequest(line.DishId, Math.Abs(portions));
        var response = portions > 0
            ? await api.AddDishAsync(guest.SessionId, guest.SessionToken, request)
            : await api.RemoveDishAsync(guest.SessionId, guest.SessionToken, request);

        if (!response.Ok)
            throw new InvalidOperationException(
                $"{guestName} could not change the portions of {line.DishName}: {response.Status} {await response.ReadErrorMessageAsync()}");

        guest.Put(line.DishId, line.DishName, line.Price, portions);
        actor.Remember(new CartChange(guestName, line.Price * portions));
    }
}
