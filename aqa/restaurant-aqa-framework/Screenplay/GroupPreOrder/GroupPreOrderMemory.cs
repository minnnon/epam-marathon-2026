namespace Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

/// <summary>Who may ask about a group, other than its owner. Each maps to a different credential.</summary>
public enum Requester
{
    Owner,
    AnotherCustomer,
    GuestOfTheGroup,
    Waiter,
    SignedOutVisitor,
}

/// <summary>A customer account the scenario registered for itself (a fresh email every run), remembered under its
/// role — "owner", "another customer". The id token is what that customer sends as a bearer token.</summary>
public sealed record CustomerAccount(string Role, string FirstName, string Email, string Password, string IdToken);

/// <summary>A table booked for a group, remembered under a name ("the group", "the other group").
/// <paramref name="OwnerRole"/> points back at the <see cref="CustomerAccount"/> that booked it.</summary>
public sealed record GroupBooking(string Name, string ReservationId, string GroupReservationId, string OwnerRole);

/// <summary>One line of a guest's cart: a dish, its price as the menu showed it, and how many portions.</summary>
public sealed record CartLine(string DishId, string DishName, decimal Price, int Portions)
{
    public decimal Subtotal => Price * Portions;
}

/// <summary>
/// A guest who joined a group, remembered under the guest's name. The cart is what the test itself put in it
/// (dish → portions), so it is the expected side of every comparison with what the owner is shown — no figure in
/// a feature file is ever hard-coded against today's menu prices.
/// </summary>
public sealed class GroupGuest(string name, string groupName, string sessionId, string sessionToken)
{
    private readonly Dictionary<string, CartLine> _cart = new();

    public string Name { get; } = name;
    public string GroupName { get; } = groupName;
    public string SessionId { get; } = sessionId;
    public string SessionToken { get; } = sessionToken;
    public bool HasSubmittedTheCart { get; private set; }

    /// <summary>Set once the owner has removed this guest, so "the guests of the group" stops counting them.</summary>
    public bool WasRemoved { get; private set; }

    public IReadOnlyCollection<CartLine> Cart => _cart.Values;

    public decimal Subtotal => _cart.Values.Sum(line => line.Subtotal);

    public void Put(string dishId, string dishName, decimal price, int portions)
    {
        var current = _cart.TryGetValue(dishId, out var line) ? line.Portions : 0;
        var updated = current + portions;
        if (updated <= 0)
            _cart.Remove(dishId);
        else
            _cart[dishId] = new CartLine(dishId, dishName, price, updated);
    }

    public void MarkSubmitted() => HasSubmittedTheCart = true;

    public void MarkRemoved() => WasRemoved = true;
}

/// <summary>The most recent change a guest made to the cart, so a later step can say by how much the total moved.</summary>
public sealed record CartChange(string GuestName, decimal AmountChanged);

/// <summary>What the owner saw at some earlier point of the scenario (remembered under a name, e.g. "before").</summary>
public sealed record ParticipantsSeen(decimal Total, int Count);

/// <summary>The dishes the guests of one group can pick from: what the booked location serves, available and priced.</summary>
public sealed record GroupMenu(string GroupName, IReadOnlyList<GroupMenuDish> Dishes);

public sealed record GroupMenuDish(string Id, string Name, decimal Price);

/// <summary>Names the scenario uses for things it remembers more than one of.</summary>
public static class GroupNames
{
    public const string TheGroup = "the group";
    public const string TheOtherGroup = "the other group";
    public const string Owner = "owner";
    public const string AnotherCustomer = "another customer";
    public const string Before = "before";
}
