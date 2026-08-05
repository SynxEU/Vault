// Browser manipulation: https://www.selenium.dev/documentation/en/webdriver/browser_manipulation/

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using Xunit;

namespace Vault.Testing.BrowserTests;

public class BrowserManipulation
{
    const string homeUrl = "https://localhost:7117/";
    const string registerUrl = "https://localhost:7117/register";
    const string homeTitle = "Vault - Secure Banking";

    [Fact]
    [Trait("Category", "Smoke")]
    public void LoadApplicationPage()
    {
        using (IWebDriver driver = DemoHelper.CreateChromeDriver())
        {
            driver.Navigate().GoToUrl(homeUrl);


            Assert.Equal(homeTitle, driver.Title);
            Assert.Equal(homeUrl, driver.Url);
        }
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void ReloadHomePage()
    {
        using (IWebDriver driver = DemoHelper.CreateChromeDriver())
        {
            driver.Navigate().GoToUrl(homeUrl);
            
            driver.Navigate().Refresh();
            
            Assert.Equal(homeTitle, driver.Title);
            Assert.Equal(homeUrl, driver.Url);
        }
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void ReloadHomePageOnForward()
    {
        using (IWebDriver driver = DemoHelper.CreateChromeDriver())
        {
            driver.Navigate().GoToUrl(registerUrl);

            driver.Navigate().GoToUrl(homeUrl);

            driver.Navigate().Refresh();
            
            Assert.Equal(homeTitle, driver.Title);
            Assert.Equal(homeUrl, driver.Url);
        }
    }

    [Theory]
    [InlineData(WebBrowser.Chrome)]
    [InlineData(WebBrowser.Firefox)]
    [Trait("Category", "Smoke")]
    public void LoadApplicationPage_MultiBrowser(WebBrowser browser)
    {
        using (IWebDriver driver = DemoHelper.GetWebDriver(browser))
        {
            driver.Navigate().GoToUrl(homeUrl);
            
            Assert.Equal(homeTitle, driver.Title);
            Assert.Equal(homeUrl, driver.Url);
        }
    }
}
