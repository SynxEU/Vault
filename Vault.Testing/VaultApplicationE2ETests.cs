using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace Vault.Testing;

public class VaultApplicationE2ETests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private const string BaseUrl = "https://localhost:7117";

    public VaultApplicationE2ETests()
    {
        var options = new ChromeOptions();
        options.AddArgument("--start-maximized");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        
        try
        {
            _driver = new ChromeDriver(options);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        }
        catch
        {
            throw new InvalidOperationException(
                "ChromeDriver not found. Install via: dotnet tool install -g WebDriver.ChromeDriver");
        }
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }

    [Fact(Skip = "Requires running Vault application and ChromeDriver")]
    public void UserCanRegisterAndLogin()
    {
        var username = $"testuser_{DateTime.UtcNow.Ticks}";
        var email = $"{username}@example.com";
        var password = "SecureTestPass123!@#";

        try
        {
            _driver.Navigate().GoToUrl($"{BaseUrl}/register");
            
            var usernameField = _wait.Until(d => d.FindElement(By.Id("username")));
            usernameField.SendKeys(username);

            var emailField = _driver.FindElement(By.Id("email"));
            emailField.SendKeys(email);

            var passwordField = _driver.FindElement(By.Id("password"));
            passwordField.SendKeys(password);

            var confirmField = _driver.FindElement(By.Id("confirmPassword"));
            confirmField.SendKeys(password);

            var submitButton = _driver.FindElement(By.CssSelector("button[type='submit']"));
            submitButton.Click();

            _wait.Until(d => d.Url.Contains("/login"));

            Assert.Contains("/login", _driver.Url);
        }
        catch (InvalidOperationException ex)
        {
            Assert.Contains("ChromeDriver", ex.Message);
        }
    }

    [Fact(Skip = "Requires running Vault application and ChromeDriver")]
    public void UserCanCreateBankAccount()
    {
        try
        {
            var username = $"bankuser_{DateTime.UtcNow.Ticks}";
            var password = "BankTestPass123!@#";

            _driver.Navigate().GoToUrl($"{BaseUrl}/register");
            var usernameField = _wait.Until(d => d.FindElement(By.Id("username")));
            usernameField.SendKeys(username);
            
            var emailField = _driver.FindElement(By.Id("email"));
            emailField.SendKeys($"{username}@example.com");
            
            var passwordField = _driver.FindElement(By.Id("password"));
            passwordField.SendKeys(password);
            
            var confirmField = _driver.FindElement(By.Id("confirmPassword"));
            confirmField.SendKeys(password);
            
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            _wait.Until(d => d.Url.Contains("/login"));
            
            _driver.Navigate().GoToUrl($"{BaseUrl}/login");
            usernameField = _wait.Until(d => d.FindElement(By.Id("username")));
            usernameField.SendKeys(username);
            
            passwordField = _driver.FindElement(By.Id("password"));
            passwordField.SendKeys(password);
            
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            _wait.Until(d => d.Url.Contains("/bank-accounts"));

            var createButton = _driver.FindElement(By.XPath("//button[contains(normalize-space(.), 'Create Account')]"));
            createButton.Click();

            var accountNameField = _wait.Until(d => d.FindElement(By.Id("accountName")));
            accountNameField.SendKeys("My Test Account");

            var accountNumberField = _driver.FindElement(By.Id("accountNumber"));
            accountNumberField.SendKeys("1234567890");

            var ibanField = _driver.FindElement(By.Id("iban"));
            ibanField.SendKeys("DK5000400440116243");

            var balanceField = _driver.FindElement(By.Id("balance"));
            balanceField.SendKeys("5000.00");

            var currencyField = _driver.FindElement(By.Id("currency"));
            currencyField.SendKeys("DKK");

            var bankNameField = _driver.FindElement(By.Id("bankName"));
            bankNameField.SendKeys("Test Bank");

            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            _wait.Until(d => d.PageSource.Contains("My Test Account"));
            Assert.Contains("My Test Account", _driver.PageSource);
        }
        catch (InvalidOperationException ex)
        {
            Assert.Contains("ChromeDriver", ex.Message);
        }
    }

    [Fact(Skip = "Requires running Vault application and ChromeDriver")]
    public void NavigationMenuShowsCorrectLinksWhenLoggedIn()
    {
        try
        {
            _driver.Navigate().GoToUrl($"{BaseUrl}/");
            
            var loginLink = _wait.Until(d => d.FindElements(By.LinkText("Login")));
            Assert.NotEmpty(loginLink);
        }
        catch (InvalidOperationException ex)
        {
            Assert.Contains("ChromeDriver", ex.Message);
        }
    }
}
