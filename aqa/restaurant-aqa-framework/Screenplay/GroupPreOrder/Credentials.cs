using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;

namespace Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

/// <summary>Which bearer token each <see cref="Requester"/> sends when asking about a group.</summary>
public static class Credentials
{
    /// <summary>
    /// The token <paramref name="requester"/> presents for <paramref name="group"/>:
    /// the owner's id token; another (freshly registered) customer's id token; the session token of one of the
    /// group's own guests; the standing waiter's id token; or nothing at all for a visitor who is not signed in.
    /// </summary>
    public static async Task<string?> OfAsync(Actor actor, Requester requester, GroupBooking group)
    {
        switch (requester)
        {
            case Requester.Owner:
                return actor.Recall<CustomerAccount>(group.OwnerRole).IdToken;

            case Requester.AnotherCustomer:
                if (!HasAccount(actor, GroupNames.AnotherCustomer))
                    await actor.AttemptsToAsync(new RegisterACustomer(GroupNames.AnotherCustomer));
                return actor.Recall<CustomerAccount>(GroupNames.AnotherCustomer).IdToken;

            case Requester.GuestOfTheGroup:
                var guest = actor.RecallAll<GroupGuest>().FirstOrDefault(g => g.GroupName == group.Name && !g.WasRemoved)
                    ?? throw new InvalidOperationException($"No guest has joined {group.Name} yet, so no guest can ask.");
                return guest.SessionToken;

            case Requester.Waiter:
                return (await actor.AsksForAsync(new TheWaiterSession())).IdToken;

            case Requester.SignedOutVisitor:
                return null;

            default:
                throw new ArgumentOutOfRangeException(nameof(requester), requester, null);
        }
    }

    private static bool HasAccount(Actor actor, string role) =>
        actor.RecallAll<CustomerAccount>().Any(account => account.Role == role);
}
