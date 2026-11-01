using Restaurant_Aqa_Framework.Screenplay;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;
using Restaurant_Aqa_Framework.Screenplay.Questions;
using Restaurant_Aqa_Framework.Screenplay.Tasks;

namespace Restaurant_Aqa_Framework.StepDefinitions;

[Binding]
public sealed class StorefrontStepDefinitions(Actor actor) : StepDefinitionBase(actor)
{
    private const string Customer = "customer";
    private const string WrongPassword = "Wr0ng-Passw0rd!";

    [Given("a customer has registered")]
    public Task GivenACustomerHasRegistered() => Actor.AttemptsToAsync(new RegisterACustomer(Customer));

    [When("the customer signs in on the storefront")]
    public Task WhenTheCustomerSignsIn()
    {
        var customer = Recall<CustomerAccount>(Customer);
        return Actor.AttemptsToAsync(new SignInThroughTheLoginForm(customer.Email, customer.Password));
    }

    [When("the customer signs in on the storefront with a wrong password")]
    public Task WhenTheCustomerSignsInWithAWrongPassword()
    {
        var customer = Recall<CustomerAccount>(Customer);
        return Actor.AttemptsToAsync(new TryToSignInThroughTheLoginForm(customer.Email, WrongPassword));
    }

    [Then("the storefront shows the customer as signed in")]
    public async Task ThenTheStorefrontShowsTheCustomerAsSignedIn()
    {
        var customer = Recall<CustomerAccount>(Customer);
        var signedIn = await Actor.AsksForAsync(new TheSignedInUser());

        await Assert.That(signedIn).IsNotNull().Because("the header shows nobody signed in");
        await Assert.That(signedIn!).Contains(customer.FirstName).Because($"the header shows '{signedIn}'");
    }

    [Then("the customer is still asked to sign in")]
    public async Task ThenTheCustomerIsStillAskedToSignIn() =>
        await Assert.That(await Actor.AsksForAsync(new TheSignInFormIsStillShown())).IsTrue()
            .Because($"the page moved on to {Actor.LookingAtThePage().Url}");
}
