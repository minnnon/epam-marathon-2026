using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.Questions;

namespace Restaurant_Aqa_Framework.StepDefinitions;

/// <summary>Business-level outcomes shared by several features ("the visitor is told ...", "could not be
/// found"). How an outcome shows up on the wire — which status code, which error body — is decided here,
/// so feature files never mention it.</summary>
[Binding]
public sealed class OutcomeStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    [Then("the visitor is told {string}")]
    [Scope(Tag = "api")]
    public async Task ThenVisitorIsTold(string message)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());

        await Assert.That(outcome.Succeeded).IsFalse().Because($"the request unexpectedly succeeded: {outcome.Body}");
        await Assert.That(outcome.ErrorMessage).IsEqualTo(message);
    }

    [Then("sign-in is refused")]
    public async Task ThenSignInIsRefused()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(401).Because($"response body was: {outcome.Body}");
    }

    [Then("the guest is told that the {item} could not be found")]
    public async Task ThenGuestIsToldNotFound(string item)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());

        await Assert.That(outcome.Status).IsEqualTo(404).Because($"response body was: {outcome.Body}");
        await Assert.That(outcome.ErrorMessage).Contains("was not found");
    }

    [Then("the location is not created")]
    public async Task ThenLocationIsNotCreated()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsGreaterThanOrEqualTo(400).Because($"the location was created: {outcome.Body}");
    }

    // ---- group pre-order ----
    // Not signed in (or presenting a guest's session token, which the sign-in gate does not accept) → 401;
    // signed in but not the owner of the booking → 403. Either way the guests' details must not be in the answer.

    [Then("they are not allowed to see the participants")]
    public async Task ThenTheyAreNotAllowedToSeeTheParticipants()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        var guests = await Actor.AsksForAsync(new TheGuestsOfTheGroup());

        await Assert.That(outcome.Status is 401 or 403).IsTrue().Because($"expected a refusal, got {outcome.Status}: {outcome.Body}");
        foreach (var guest in guests)
            await Assert.That(outcome.Body.Contains(guest.Name, StringComparison.Ordinal)).IsFalse()
                .Because($"the refusal leaked {guest.Name}'s details: {outcome.Body}");
    }

    [Then("they are not allowed to remove anyone")]
    public async Task ThenTheyAreNotAllowedToRemoveAnyone()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status is 401 or 403).IsTrue().Because($"expected a refusal, got {outcome.Status}: {outcome.Body}");
    }

    [Then("the owner is told that the group could not be found")]
    [Then("the owner is told that the guest could not be found")]
    public async Task ThenTheOwnerIsToldItCouldNotBeFound()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(404).Because($"response body was: {outcome.Body}");
        await Assert.That(outcome.ErrorMessage).Contains("not found");
    }

    [Then("the owner is told that {word} is not part of this group")]
    public async Task ThenTheOwnerIsToldTheGuestIsNotPartOfThisGroup(string guest)
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Status).IsEqualTo(400).Because($"removing {guest} from the wrong group answered: {outcome.Body}");
        await Assert.That(outcome.Body).Contains("does not belong to this group");
    }

    [Then("the information is available")]
    [Then("the menu is shown")]
    public async Task ThenInformationIsAvailable()
    {
        var outcome = await Actor.AsksForAsync(new TheOutcome());
        await Assert.That(outcome.Succeeded).IsTrue().Because($"response status {outcome.Status}, body: {outcome.Body}");
    }
}
