using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Support;
using System;
using System.Linq;
using OpenQA.Selenium.DevTools.V129.Debugger;

class LogInWebPage
{
    public static void LogIn(string targetUrl,string username,string password, IWebDriver driver)
    {

        // Go to main page link 
        driver.Navigate().GoToUrl(targetUrl);

        // Wait for redirection to the login page
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        try
        {
            // Wait until the URL contains "login" or a username field appears
           wait.Until(d => d.Url.Contains("login") || d.FindElement(By.CssSelector("[formcontrolname='email']")).Displayed);

            Console.WriteLine("Redirected to the login page.");
        }
        catch (WebDriverTimeoutException)
        {
            Console.WriteLine("No redirection detected within the timeout period.");
        }
        IWebElement usernameField = wait.Until(d => d.FindElement(By.CssSelector("[formcontrolname='email']"))); // Replace "username" with the actual name/id
        IWebElement passwordField = wait.Until(d => d.FindElement(By.CssSelector("[formcontrolname='password']"))); // password element - (formcontrol name found in inspect OuterHTML)
        IWebElement loginButton = driver.FindElement(By.Id("login-btn")); // Log in button id, found when ispecting

        usernameField.SendKeys(username);
        passwordField.SendKeys(password);

        // Click the login button
        loginButton.Click();

        // Confirm successful login (optional)
        wait.Until(d => d.Url.Contains("399?page=")); // exclusive part of the loaded site url
        Console.WriteLine("Successfully logged in!");
    }
}
