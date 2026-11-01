using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrder;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>
/// A guest puts <paramref name="differentDishes"/> dishes in their cart, <paramref name="portionsOfEach"/> portions
/// of each. The dishes come from the menu of the booked location (whatever is available there), each guest starting at a different place in
/// it so two guests do not necessarily order the same thing. What went in is recorded on the
/// <see cref="GroupGuest"/>, which is the expected side when the owner's view is checked. A set-up task.
/// </summary>
public sealed class ChooseDishes(string guestName, int differentDishes, int portionsOfEach) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        if (differentDishes == 0)
            return;

        var guest = actor.Recall<GroupGuest>(guestName);
        var menu = await TheGroupMenu.OfAsync(actor, guest);
        if (menu.Dishes.Count < differentDishes)
            throw new InvalidOperationException(
                $"The menu has only {menu.Dishes.Count} available dishes; {guestName} needs {differentDishes}.");

        var api = new GroupPreOrderApi(actor.UsingTheApi());
        var start = actor.RecallAll<GroupGuest>().Count;
        for (var i = 0; i < differentDishes; i++)
        {
            var dish = menu.Dishes[(start + i) % menu.Dishes.Count];
            var response = await api.AddDishAsync(guest.SessionId, guest.SessionToken, new GuestDishRequest(dish.Id, portionsOfEach));
            if (!response.Ok)
                throw new InvalidOperationException(
                    $"{guestName} could not add {dish.Name}: {response.Status} {await response.ReadErrorMessageAsync()}");

            guest.Put(dish.Id, dish.Name, dish.Price, portionsOfEach);
        }
    }
}

/// <summary>The menu of the location the group booked, as a guest of the group sees it: the dishes that are available
/// and can be picked. Fetched once per group and remembered under the group's name.</summary>
public static class TheGroupMenu
{
    public static async Task<GroupMenu> OfAsync(Actor actor, GroupGuest guest)
    {
        var known = actor.RecallAll<GroupMenu>().LastOrDefault(m => m.GroupName == guest.GroupName);
        if (known is not null)
            return known;

        var response = await new GroupPreOrderApi(actor.UsingTheApi()).GetGuestMenuAsync(guest.SessionId, guest.SessionToken);
        if (!response.Ok)
            throw new InvalidOperationException($"Reading the group's menu failed with {response.Status}: {await response.ReadErrorMessageAsync()}");

        var dishes = (await response.ReadAsAsync<GuestMenuResponse>()).Dishes.Values
            .SelectMany(byType => byType)
            .Where(d => d.Price > 0
                && d.Selectable != false
                && (d.State is null || string.Equals(d.State, "Available", StringComparison.OrdinalIgnoreCase)))
            .Select(d => new GroupMenuDish(d.Id, d.Name, d.Price))
            .ToList();

        var menu = new GroupMenu(guest.GroupName, dishes);
        actor.Remember(guest.GroupName, menu);
        return menu;
    }
}
