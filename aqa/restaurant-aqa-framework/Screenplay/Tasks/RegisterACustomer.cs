using Restaurant_Aqa_Framework.Api;
using Restaurant_Aqa_Framework.Api.Dtos.Auth;
using Restaurant_Aqa_Framework.Screenplay.GroupPreOrder;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>
/// Registers a brand-new customer (a fresh email every run, so nothing depends on accounts that already exist),
/// signs them in and remembers the account under <paramref name="role"/>. Registration always creates a customer,
/// which is exactly what an owner or "another customer" is. The backend has no way to delete a user, so the unique
/// email is what keeps runs apart. A set-up task: it throws if either call fails.
/// </summary>
public sealed class RegisterACustomer(string role) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var auth = new AuthApi(actor.UsingTheApi());
        var unique = Guid.NewGuid().ToString("N")[..10];

        // Names: Latin letters only (the registration rule); password: 8–16 chars with upper, lower, digit, special.
        var firstName = "Aqa";
        var lastName = string.Concat(role.Where(char.IsLetter));
        var email = $"aqa-{unique}@example.com";
        var password = $"Aqa{unique[..5]}!9x";

        var registered = await auth.RegisterAsync(new RegisterRequest(firstName, lastName, email, password, password));
        if (!registered.Ok)
            throw new InvalidOperationException(
                $"Registering the {role} failed with {registered.Status}: {await registered.ReadErrorMessageAsync()}");

        var signedIn = await auth.SignInAsync(new SignInRequest(email, password));
        if (!signedIn.Ok)
            throw new InvalidOperationException(
                $"Signing in the {role} failed with {signedIn.Status}: {await signedIn.ReadErrorMessageAsync()}");

        var token = (await signedIn.ReadAsAsync<SignInResponse>()).IdToken;
        actor.Remember(role, new CustomerAccount(role, firstName, email, password, token));
    }
}
