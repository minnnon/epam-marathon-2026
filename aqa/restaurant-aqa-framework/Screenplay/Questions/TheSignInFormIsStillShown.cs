using Microsoft.Playwright;
using Restaurant_Aqa_Framework.Ui;

namespace Restaurant_Aqa_Framework.Screenplay.Questions;

/// <summary>Whether the visitor is still on the login page with the sign-in form in front of them.</summary>
public sealed class TheSignInFormIsStillShown : IQuestion<bool>
{
    public async Task<bool> AnsweredByAsync(Actor actor)
    {
        var page = actor.LookingAtThePage();
        if (!page.Url.Contains("/login", StringComparison.Ordinal))
            return false;

        try
        {
            await new LoginPage(page).PasswordInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}
