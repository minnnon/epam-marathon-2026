using Microsoft.Playwright;

namespace Restaurant_Aqa_Framework.Ui;

public sealed class LoginPage(IPage page) : BasePage(page)
{
    // Checked on the deployed storefront: the "Email" <label> is not associated with its input (no `for`, does not
    // wrap it), so GetByLabel finds nothing and the input's accessible name is only its placeholder. Both fields are
    // located by their Angular form-control name instead, and the submit button inside the form only — the header
    // has its own "Sign In" buttons (desktop and mobile) that would make a page-wide lookup ambiguous.
    private ILocator Form => Page.Locator("form").Filter(new() { Has = Page.Locator("input[formcontrolname='password']") });

    public ILocator EmailInput => Form.Locator("input[formcontrolname='email']");
    public ILocator PasswordInput => Form.Locator("input[formcontrolname='password']");
    public ILocator SubmitButton => Form.GetByRole(AriaRole.Button, new() { Name = "Sign In", Exact = true });
    public ILocator ErrorMessage => Page.Locator(".response-error-message");

    public Task NavigateAsync() => NavigateToAsync("/login");

    public async Task SignInAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await SubmitButton.ClickAsync();
    }

    /// <summary>Submits the form and waits for the sign-in service to answer, whatever it answers — for a sign-in
    /// that is expected to be refused, where there is no navigation to wait for.</summary>
    public async Task SignInAndWaitForTheAnswerAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await Page.RunAndWaitForResponseAsync(
            () => SubmitButton.ClickAsync(),
            response => response.Request.Method == "POST" && response.Url.Contains("/signin", StringComparison.Ordinal));
    }
}
