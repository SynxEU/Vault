using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System.Threading;
using OpenQA.Selenium.Support.UI;

namespace Vault.Testing.BrowserTests;

public enum WebBrowser
{
    Chrome,
    Firefox
}
    
public static class DemoHelper
{
    public static IWebDriver CreateChromeDriver()
    {
        var options = new ChromeOptions();

        options.AddArgument("--start-maximized");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--ignore-certificate-errors");
        
        return new ChromeDriver(options);
    }
    
    public static IWebDriver CreateFirefoxDriver()
    {
        var options = new FirefoxOptions();

        options.AcceptInsecureCertificates = true;

        return new FirefoxDriver(options);
    }
    
    public static IWebDriver GetWebDriver(WebBrowser browser)
    {
        return browser switch
        {
            WebBrowser.Chrome => CreateChromeDriver(),
            WebBrowser.Firefox => CreateFirefoxDriver(),
            _ => throw new ArgumentException()
        };
    }
}
