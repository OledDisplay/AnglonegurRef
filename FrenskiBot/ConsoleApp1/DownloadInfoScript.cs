using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Threading.Tasks;

class DownloadInfoScript
{
    private static IWebDriver _driver;

    public static int MaxPage = 0;

    // Class-level variables for shared access
    private static int output;
    private static int page = 2;
    private static int num = 0;
    private static int counter = 0;
    private static string BaseDir;
    private static string DirectoryTempPath;

    public static async Task DownloadScript(string pageUrl, string root, string LoginName, string LoginPass, int urok, int RunLoop)
    {
        if (File.Exists("Uchebnik\\Data.flag")  || Directory.Exists($"Uchebnik\\{urok}")) // add flag for downloadiing for second condition
        {
            Console.WriteLine("Local download found. Ps");
            return;
        }
      
        // Close driver upon the program crashing to prevent a resource Leaks
        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
         CleanupDriver();
        };


        // Initialize variables
        List<string> heightKeyEl = new List<string> { "h38", "h42" }; // CONDITIONAL PARTS OF REPEATING THEMES
        List<string> themeKeyEl = new List<string> { "t", "m0" }; // NECESSARY PARTS OF REPEATING THEMES
        BaseDir = Path.Combine(root, "Uchebnik");
        DirectoryTempPath = "placeholder";

        // Initialize WebDriver
        InitializeDriver();

        // Log in
        LogInWebPage.LogIn(pageUrl + page.ToString(), LoginName, LoginPass, _driver);

        // Run page download loop

        // Handle corrupted directories
        if (Directory.Exists(BaseDir))
        {
            Directory.Delete(BaseDir, true);
            Console.WriteLine("Textbook seems to be corrupted or incomplete. Deleting...");
        }
        Directory.CreateDirectory(BaseDir);

        if (RunLoop == 1)
        {
            do
            {
                num =  ScreenshotScript.ExtractNumberFromElement(_driver, themeKeyEl, heightKeyEl);
                await LoopBody(pageUrl, themeKeyEl, heightKeyEl);
                page += 2;
                _driver.Navigate().GoToUrl(pageUrl + page.ToString());
            }
            while (output != 0);

            File.Create("Uchebnik\\Data.flag").Dispose();
            Console.WriteLine("Flag file created");
        }
        else
        {
         Thread.Sleep(500); 
         page = 0;
            do
            {
                page += 2;
                _driver.Navigate().GoToUrl(pageUrl + page.ToString());
                num = ScreenshotScript.ExtractNumberFromElement(_driver, themeKeyEl, heightKeyEl);

            }
            while (num < urok);

            await LoopBody(pageUrl, themeKeyEl, heightKeyEl);
            page += 2;
            _driver.Navigate().GoToUrl(pageUrl + page.ToString());
            await LoopBody(pageUrl, themeKeyEl, heightKeyEl);
        }

        // Close Driver
        CleanupDriver();
    }

    public static void InitializeDriver()
    {
        if (_driver == null)
        {
            Console.WriteLine("Creating a new ChromeDriver instance...");
            _driver = ScreenshotScript.GetHeadlessChromeDriver();
            _driver.Manage().Window.Maximize();
        }
    }

    public static void CleanupDriver()
    {
        if (_driver != null)
        {
            _driver.Quit();
            _driver = null;
            Console.WriteLine("ChromeDriver instance cleaned up.");
        }
    }

    private static async Task LoopBody(string pageUrl, List<string> themeKeyEl, List<string> heightKeyEl)
    {
        if (!Directory.Exists(Path.Combine(BaseDir, $"{num}")))
        {
            counter = 0;
            DirectoryTempPath = Path.Combine(BaseDir, $"{num}");
            Directory.CreateDirectory(DirectoryTempPath);
        }

        string ScreenshotPath = Path.Combine(DirectoryTempPath, $"pishki{counter}.png");
        output = ScreenshotScript.TakeScreenshot(_driver, ScreenshotPath, themeKeyEl, heightKeyEl);

        if (output > 0)
        {
            counter++;
            num = output;
        }
    }
}
