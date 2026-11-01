using System.Globalization;
using Restaurant_Aqa_Framework.Api.Dtos.GroupPreOrder;
using Restaurant_Aqa_Framework.Configuration;
using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;

namespace Restaurant_Aqa_Framework.StepDefinitions;

[Binding]
public sealed class GroupPreOrderStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    // ---- Given ----

    [Given("a customer has booked a table for a group of {int}")]
    public Task GivenACustomerHasBookedATableForAGroup(int people) =>
        Actor.AttemptsToAsync(new RegisterACustomer(GroupNames.Owner), new BookATableForAGroup(people));

    /// <summary>One row per guest: who joins, how many different dishes they pick and how many portions of each,
    /// and whether they have confirmed the cart ("submitted") or are still choosing ("open").</summary>
    [Given("these guests have joined the group:")]
    public async Task GivenTheseGuestsHaveJoinedTheGroup(DataTable guests)
    {
        foreach (var row in guests.Rows)
        {
            var name = row["guest"];
            await Actor.AttemptsToAsync(
                new JoinTheGroup(name),
                new ChooseDishes(
                    name,
                    int.Parse(row["different dishes"], CultureInfo.InvariantCulture),
                    int.Parse(row["portions of each"], CultureInfo.InvariantCulture)));

            if (string.Equals(row["cart"], "submitted", StringComparison.OrdinalIgnoreCase))
                await Actor.AttemptsToAsync(new SubmitTheCart(name));
        }
    }

    [Given("another customer has a group of their own that {word} has joined")]
    public Task GivenAnotherCustomerHasAGroupThatAGuestHasJoined(string guest) =>
        Actor.AttemptsToAsync(
            new RegisterACustomer(GroupNames.AnotherCustomer),
            new BookATableForAGroup(3, GroupNames.AnotherCustomer, GroupNames.TheOtherGroup),
            new JoinTheGroup(guest, GroupNames.TheOtherGroup));

    [Given("the owner has seen the group total")]
    public async Task GivenTheOwnerHasSeenTheGroupTotal()
    {
        await Actor.AttemptsToAsync(new LookAtTheParticipants(Requester.Owner));
        var seen = await Actor.AsksForAsync(new TheParticipantsShown());
        Actor.Remember(GroupNames.Before, new ParticipantsSeen(seen.Total, seen.Participants.Count));
    }

    [Given("the owner has removed {word} from the group")]
    public async Task GivenTheOwnerHasRemovedAGuest(string guest)
    {
        await Actor.AttemptsToAsync(new RemoveAParticipant(guest));
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        if (!outcome.Succeeded)
            throw new InvalidOperationException($"Removing {guest} failed with {outcome.Status}: {outcome.Body}");
    }

    // ---- When ----

    [When("the owner looks at the group's participants")]
    public Task WhenTheOwnerLooksAtTheParticipants() =>
        Actor.AttemptsToAsync(new LookAtTheParticipants(Requester.Owner));

    [When("the owner looks at the participants of a group that does not exist")]
    public Task WhenTheOwnerLooksAtAMissingGroup() =>
        Actor.AttemptsToAsync(LookAtTheParticipants.OfAGroupThatDoesNotExist());

    [When("{requester} tries to look at the group's participants")]
    public Task WhenSomeoneElseTriesToLookAtTheParticipants(Requester someone)
    {
        SkipWhenNoWaiterIsConfigured(someone);
        return Actor.AttemptsToAsync(new LookAtTheParticipants(someone));
    }

    [When("{word} adds another portion to the cart")]
    public Task WhenAGuestAddsAPortion(string guest) => Actor.AttemptsToAsync(ChangeTheCart.AddAPortion(guest));

    [When("{word} takes a portion out of the cart")]
    public Task WhenAGuestTakesOutAPortion(string guest) => Actor.AttemptsToAsync(ChangeTheCart.TakeOutAPortion(guest));

    [When("the owner removes {word} from the group")]
    [When("the owner tries to remove {word} from the group")]
    public Task WhenTheOwnerRemovesAGuest(string guest) => Actor.AttemptsToAsync(new RemoveAParticipant(guest));

    [When("the owner removes every guest from the group")]
    public async Task WhenTheOwnerRemovesEveryGuest()
    {
        foreach (var guest in await Actor.AsksForAsync(new TheGuestsOfTheGroup()))
            await Actor.AttemptsToAsync(new RemoveAParticipant(guest.Name));
    }

    [When("the owner tries to remove a guest who does not exist")]
    public Task WhenTheOwnerRemovesAGuestWhoDoesNotExist() =>
        Actor.AttemptsToAsync(RemoveAParticipant.ANonExistentParticipant());

    [When("{requester} tries to remove {word} from the group")]
    public Task WhenSomeoneElseTriesToRemoveAGuest(Requester someone, string guest)
    {
        SkipWhenNoWaiterIsConfigured(someone);
        return Actor.AttemptsToAsync(new RemoveAParticipant(guest, someone));
    }

    // ---- Then ----

    [Then("the owner sees every guest with their dishes, portions and subtotal")]
    public async Task ThenTheOwnerSeesEveryGuestWithTheirCart()
    {
        var shown = await Actor.AsksForAsync(new TheParticipantsShown());
        var guests = await Actor.AsksForAsync(new TheGuestsOfTheGroup());
        var body = Actor.TheLastResponse().Body;

        await Assert.That(shown.Participants.Count).IsEqualTo(guests.Count).Because($"the owner was shown: {body}");

        foreach (var guest in guests)
        {
            var participant = shown.Participants.FirstOrDefault(p => p.Id == guest.SessionId);
            await Assert.That(participant).IsNotNull().Because($"{guest.Name} is missing from: {body}");

            await Assert.That(participant!.Name).IsEqualTo(guest.Name);
            await Assert.That(participant.Items.Count).IsEqualTo(guest.Cart.Count).Because($"{guest.Name}'s dishes in: {body}");
            foreach (var line in guest.Cart)
            {
                var item = participant.Items.FirstOrDefault(i => i.Dish.Id == line.DishId);
                await Assert.That(item).IsNotNull().Because($"{line.DishName} is missing from {guest.Name}'s dishes in: {body}");
                await Assert.That(item!.Quantity).IsEqualTo(line.Portions).Because($"portions of {line.DishName} for {guest.Name} in: {body}");
            }

            await Assert.That(participant.Total).IsEqualTo(guest.Subtotal).Because($"{guest.Name}'s subtotal in: {body}");
        }
    }

    [Then("the group total is the sum of the guests' subtotals")]
    public async Task ThenTheGroupTotalIsTheSumOfTheSubtotals()
    {
        var shown = await Actor.AsksForAsync(new TheParticipantsShown());
        var guests = await Actor.AsksForAsync(new TheGuestsOfTheGroup());
        var body = Actor.TheLastResponse().Body;

        await Assert.That(shown.Total).IsEqualTo(shown.Participants.Sum(p => p.Total))
            .Because($"the group total should add up the subtotals shown: {body}");
        await Assert.That(shown.Total).IsEqualTo(guests.Sum(g => g.Subtotal))
            .Because($"the group total should add up what the remaining guests chose: {body}");
    }

    [Then("the group total has changed by the price of that portion")]
    public async Task ThenTheGroupTotalHasChangedByThePriceOfThatPortion()
    {
        var before = Recall<ParticipantsSeen>(GroupNames.Before);
        var change = Recall<CartChange>();
        var shown = await Actor.AsksForAsync(new TheParticipantsShown());

        await Assert.That(shown.Total).IsEqualTo(before.Total + change.AmountChanged)
            .Because($"the total was {before.Total} before {change.GuestName} changed the cart by {change.AmountChanged}: {Actor.TheLastResponse().Body}");
    }

    [Then("{word} is shown as having submitted the cart")]
    public Task ThenAGuestIsShownAsSubmitted(string guest) => AssertSubmittedAsync(guest, submitted: true);

    [Then("{word} is shown as still choosing")]
    public Task ThenAGuestIsShownAsStillChoosing(string guest) => AssertSubmittedAsync(guest, submitted: false);

    [Then("{word} is shown with no dishes and nothing to pay")]
    public async Task ThenAGuestIsShownWithAnEmptyCart(string guest)
    {
        var participant = await TheParticipantNamedAsync(guest);
        await Assert.That(participant.Items.Count).IsEqualTo(0).Because($"{guest} has not chosen anything: {Actor.TheLastResponse().Body}");
        await Assert.That(participant.Total).IsEqualTo(0m);
    }

    [Then("{word} is no longer a participant")]
    public async Task ThenAGuestIsNoLongerAParticipant(string guest)
    {
        var removed = Recall<GroupGuest>(guest);
        var shown = await Actor.AsksForAsync(new TheParticipantsShown());

        await Assert.That(shown.Participants.Count(p => p.Id == removed.SessionId)).IsEqualTo(0)
            .Because($"{guest} was removed but is still listed: {Actor.TheLastResponse().Body}");
    }

    /// <summary>Checks the group as the owner sees it now (a fresh look), not the refused request before it.</summary>
    [Then("every guest of the group is still a participant")]
    public async Task ThenEveryGuestIsStillAParticipant()
    {
        var shown = await Actor.AsksForAsync(new TheGroupAsTheOwnerSeesItNow());
        var guests = await Actor.AsksForAsync(new TheGuestsOfTheGroup());
        var missing = guests.Where(g => shown.Participants.All(p => p.Id != g.SessionId)).Select(g => g.Name).ToList();

        await Assert.That(missing).IsEmpty()
            .Because($"nobody should have been removed; the owner now sees {shown.Participants.Count} guests: {string.Join(", ", shown.Participants.Select(p => p.Name))}");
    }

    [Then("the owner sees that nobody has joined yet")]
    public async Task ThenTheOwnerSeesThatNobodyHasJoined()
    {
        var shown = await Actor.AsksForAsync(new TheParticipantsShown());
        await Assert.That(shown.Participants.Count).IsEqualTo(0).Because($"the owner was shown: {Actor.TheLastResponse().Body}");
        await Assert.That(shown.Total).IsEqualTo(0m);
    }

    // ---- helpers (asking and asserting only) ----

    private async Task AssertSubmittedAsync(string guest, bool submitted)
    {
        var participant = await TheParticipantNamedAsync(guest);
        await Assert.That(participant.IsSubmitted).IsEqualTo(submitted)
            .Because($"{guest} should be shown as {(submitted ? "having submitted" : "still choosing")}: {Actor.TheLastResponse().Body}");
    }

    private async Task<GroupParticipant> TheParticipantNamedAsync(string guest)
    {
        var sessionId = Recall<GroupGuest>(guest).SessionId;
        var shown = await Actor.AsksForAsync(new TheParticipantsShown());
        return shown.Participants.FirstOrDefault(p => p.Id == sessionId)
            ?? throw new InvalidOperationException($"{guest} is not in the list the owner was shown: {Actor.TheLastResponse().Body}");
    }

    private static void SkipWhenNoWaiterIsConfigured(Requester someone)
    {
        if (someone != Requester.Waiter)
            return;

        var settings = Settings.Current;
        if (string.IsNullOrEmpty(settings.WaiterUsername) || string.IsNullOrEmpty(settings.WaiterPassword))
            Skip.Test("No waiter account is configured (WAITER_USERNAME / WAITER_PASSWORD).");
    }
}
