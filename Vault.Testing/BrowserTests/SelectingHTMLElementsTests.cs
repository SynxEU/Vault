using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.ObjectModel;
using Xunit;

namespace Vault.Testing.BrowserTests;

public class SelectingHTMLElements
{
    const string homeUrl = "https://localhost:7117/";

    [Fact]
    [Trait("Category", "Application")]
    public void HomePageHasVaultHeader()
    {
        using (IWebDriver driver = DemoHelper.CreateChromeDriver())
        {
            driver.Navigate().GoToUrl(homeUrl);

            IWebElement header = driver.FindElement(By.XPath("//h1[contains(normalize-space(.), 'VAULT')]") );
            Assert.NotNull(header);
        }
    }

    [Fact]
    [Trait("Category", "Application")]
    public void WaitForVaultHeader()
    {
        using (IWebDriver driver = DemoHelper.CreateChromeDriver())
        {
            driver.Navigate().GoToUrl(homeUrl);

            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            IWebElement header = wait.Until(d => d.FindElement(By.XPath("//h1[contains(normalize-space(.), 'VAULT')]") ) );

            Assert.NotNull(header);
        }
    }
}
