using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Tasks;

/// <summary>Opens the login page and submits the form, waiting only for the sign-in service to answer — unlike
/// <see cref="SignInThroughTheLoginForm"/>, which waits to leave the login page and so cannot be used for a sign-in
/// that is meant to be refused.</summary>
public sealed class TryToSignInThroughTheLoginForm(string email, string password) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var loginPage = new LoginPage(actor.LookingAtThePage());
        await loginPage.NavigateAsync();
        await loginPage.SignInAndWaitForTheAnswerAsync(email, password);
    }
}
