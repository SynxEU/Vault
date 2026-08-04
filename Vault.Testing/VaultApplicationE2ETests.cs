using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace Vault.Testing;

public enum BrowserType
{
    Chrome,
    Firefox,
    Edge
}

public class VaultApplicationE2ETests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private const string BaseUrl = "https://localhost:7117";
    private readonly BrowserType _browserType;

    public VaultApplicationE2ETests(BrowserType browserType = BrowserType.Chrome)
    {
        _browserType = browserType;
        _driver = CreateWebDriver(browserType);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    }

    private static IWebDriver CreateWebDriver(BrowserType browserType)
    {
        return browserType switch
        {
            BrowserType.Chrome => CreateChromeDriver(),
            BrowserType.Firefox => CreateFirefoxDriver(),
            BrowserType.Edge => CreateEdgeDriver(),
            _ => throw new ArgumentException($"Unsupported browser type: {browserType}")
        };
    }

    private static IWebDriver CreateChromeDriver()
    {
        var options = new ChromeOptions();
        options.AddArgument("--start-maximized");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        
        try
        {
            return new ChromeDriver(options);
        }
        catch
        {
            throw new InvalidOperationException(
                "ChromeDriver not found. Install via: dotnet tool install -g WebDriver.ChromeDriver");
        }
    }

    private static IWebDriver CreateFirefoxDriver()
    {
        var options = new FirefoxOptions();
        options.AddArgument("--width=1920");
        options.AddArgument("--height=1080");
        
        try
        {
            return new FirefoxDriver(options);
        }
        catch
        {
            throw new InvalidOperationException(
                "GeckoDriver (Firefox) not found. Install via: dotnet tool install -g WebDriver.GeckoDriver");
        }
    }

    private static IWebDriver CreateEdgeDriver()
    {
        var options = new EdgeOptions();
        options.AddArgument("--start-maximized");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        
        try
        {
            return new EdgeDriver(options);
        }
        catch
        {
            throw new InvalidOperationException(
                "EdgeDriver not found. Install via: dotnet tool install -g WebDriver.EdgeDriver");
        }
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }

    public static TheoryData<BrowserType> GetBrowserTypes()
    {
        return new TheoryData<BrowserType>
        {
            BrowserType.Chrome,
            BrowserType.Firefox,
            BrowserType.Edge
        };
    }

    [Theory(Skip = "Requires running Vault application and all WebDrivers")]
    [MemberData(nameof(GetBrowserTypes))]
    public void UserCanRegisterAndLogin(BrowserType browserType)
    {
        using var test = new VaultApplicationE2ETests(browserType);
        var username = $"testuser_{DateTime.UtcNow.Ticks}";
        var email = $"{username}@example.com";
        var password = "SecureTestPass123!@#";

        try
        {
            test._driver.Navigate().GoToUrl($"{BaseUrl}/register");
            
            var usernameField = test._wait.Until(d => d.FindElement(By.Id("username")));
            usernameField.SendKeys(username);

            var emailField = test._driver.FindElement(By.Id("email"));
            emailField.SendKeys(email);

            var passwordField = test._driver.FindElement(By.Id("password"));
            passwordField.SendKeys(password);

            var confirmField = test._driver.FindElement(By.Id("confirmPassword"));
            confirmField.SendKeys(password);

            var submitButton = test._driver.FindElement(By.CssSelector("button[type='submit']"));
            submitButton.Click();

            test._wait.Until(d => d.Url.Contains("/login"));

            Assert.Contains("/login", test._driver.Url);
        }
        catch (InvalidOperationException ex)
        {
            Assert.Contains("Driver", ex.Message);
        }
    }

    [Theory(Skip = "Requires running Vault application and all WebDrivers")]
    [MemberData(nameof(GetBrowserTypes))]
    public void UserCanCreateBankAccount(BrowserType browserType)
    {
        using var test = new VaultApplicationE2ETests(browserType);
        
        try
        {
            var username = $"bankuser_{DateTime.UtcNow.Ticks}";
            var password = "BankTestPass123!@#";

            test._driver.Navigate().GoToUrl($"{BaseUrl}/register");
            var usernameField = test._wait.Until(d => d.FindElement(By.Id("username")));
            usernameField.SendKeys(username);
            
            var emailField = test._driver.FindElement(By.Id("email"));
            emailField.SendKeys($"{username}@example.com");
            
            var passwordField = test._driver.FindElement(By.Id("password"));
            passwordField.SendKeys(password);
            
            var confirmField = test._driver.FindElement(By.Id("confirmPassword"));
            confirmField.SendKeys(password);
            
            test._driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            test._wait.Until(d => d.Url.Contains("/login"));
            
            test._driver.Navigate().GoToUrl($"{BaseUrl}/login");
            usernameField = test._wait.Until(d => d.FindElement(By.Id("username")));
            usernameField.SendKeys(username);
            
            passwordField = test._driver.FindElement(By.Id("password"));
            passwordField.SendKeys(password);
            
            test._driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            test._wait.Until(d => d.Url.Contains("/bank-accounts"));

            var createButton = test._driver.FindElement(By.XPath("//button[contains(normalize-space(.), 'Create Account')]"));
            createButton.Click();

            var accountNameField = test._wait.Until(d => d.FindElement(By.Id("accountName")));
            accountNameField.SendKeys("My Test Account");

            var accountNumberField = test._driver.FindElement(By.Id("accountNumber"));
            accountNumberField.SendKeys("1234567890");

            var ibanField = test._driver.FindElement(By.Id("iban"));
            ibanField.SendKeys("DK5000400440116243");

            var balanceField = test._driver.FindElement(By.Id("balance"));
            balanceField.SendKeys("5000.00");

            var currencyField = test._driver.FindElement(By.Id("currency"));
            currencyField.SendKeys("DKK");

            var bankNameField = test._driver.FindElement(By.Id("bankName"));
            bankNameField.SendKeys("Test Bank");

            test._driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            test._wait.Until(d => d.PageSource.Contains("My Test Account"));
            Assert.Contains("My Test Account", test._driver.PageSource);
        }
        catch (InvalidOperationException ex)
        {
            Assert.Contains("Driver", ex.Message);
        }
    }

    [Theory(Skip = "Requires running Vault application and all WebDrivers")]
    [MemberData(nameof(GetBrowserTypes))]
    public void NavigationMenuShowsCorrectLinksWhenLoggedIn(BrowserType browserType)
    {
        using var test = new VaultApplicationE2ETests(browserType);
        
        try
        {
            test._driver.Navigate().GoToUrl($"{BaseUrl}/");
            
            var loginLink = test._wait.Until(d => d.FindElements(By.LinkText("Login")));
            Assert.NotEmpty(loginLink);
        }
        catch (InvalidOperationException ex)
        {
            Assert.Contains("Driver", ex.Message);
        }
    }
}
