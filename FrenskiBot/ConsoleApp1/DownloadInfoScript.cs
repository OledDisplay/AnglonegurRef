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
    private static string LastPage;
    private static string BaseDir;
    private static string DirectoryTempPath;

    public static async Task DownloadScript(string pageUrl, string root, string LoginName, string LoginPass, int urok, int RunLoop, bool AgressiveDownload)
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
        LastPage = Path.Combine(BaseDir, "lastpage.txt");
 
        DirectoryTempPath = "placeholder";

        //update page if savefile exists
        if (File.Exists(LastPage) && int.TryParse(File.ReadAllText(LastPage), out int savedPage)) {
         page = savedPage;
        }

        // Initialize WebDriver
        InitializeDriver();

        // Log in
        LogInWebPage.LogIn(pageUrl + page.ToString(), LoginName, LoginPass, _driver);

        // Run page download loop

        // Handle corrupted directories
        if(Directory.Exists(BaseDir)){
         // get max dir
         int maxNumber = Directory.GetDirectories(BaseDir)
            .Select(dir => Path.GetFileName(dir))  // Get directory name
            .Where(name => int.TryParse(name, out _)) // Ensure it's a number
            .Select(name => int.Parse(name)) // Convert to int
            .DefaultIfEmpty(0) // Handle empty case
            .Max(); // Get the highest number of a dir in the directiory 

         if (Directory.GetDirectories(BaseDir).Length != maxNumber +1){
            Directory.Delete(BaseDir, true);
            Console.WriteLine("Textbook has either been tampered with or is corrupted. Deleting for safety...");
         }
        }
        Directory.CreateDirectory(BaseDir);

        if (RunLoop == 1){
            do{
                num =  ScreenshotScript.ExtractNumberFromElement(_driver, themeKeyEl, heightKeyEl);
                await LoopBody(pageUrl, themeKeyEl, heightKeyEl);
                page += 2;
                _driver.Navigate().GoToUrl(pageUrl + page.ToString());
            }
            while (output != 0);

            File.Create("Uchebnik\\Data.flag").Dispose();
            Console.WriteLine("Flag file created");
        }
        else{
         Thread.Sleep(500); 
         page = 0;

         //Check for agressivedownload
         if(AgressiveDownload){
           page = urok*4-2; //estimation of page
         }
         
            do{
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

        //Save last page
        File.WriteAllText(LastPage,(page-4).ToString());

        // Close Driver
        CleanupDriver();
    }

    public static void InitializeDriver(){
        if (_driver == null){
            Console.WriteLine("Creating a new ChromeDriver instance...");
            _driver = ScreenshotScript.GetHeadlessChromeDriver();
            _driver.Manage().Window.Maximize();
        }
    }

    public static void CleanupDriver() {
        if (_driver != null){
            _driver.Quit();
            _driver = null;
            Console.WriteLine("ChromeDriver instance cleaned up.");
        }
    }

    private static async Task LoopBody(string pageUrl, List<string> themeKeyEl, List<string> heightKeyEl){
         if (!Directory.Exists(Path.Combine(BaseDir, $"{num}"))){
            counter = 0;
            DirectoryTempPath = Path.Combine(BaseDir, $"{num}");
            Directory.CreateDirectory(DirectoryTempPath);
         }

         string ScreenshotPath = Path.Combine(DirectoryTempPath, $"pishki{counter}.png");
         output = ScreenshotScript.TakeScreenshot(_driver, ScreenshotPath, themeKeyEl, heightKeyEl);
         if(output == -3){
            Console.WriteLine("Deleting urok dir - incomplete from last download..");
            Directory.Delete(Path.Combine(BaseDir, $"{num}"),true);
            num-=1;
         }

         if (output > 0){
            counter++;
            num = output;
         }

         File.WriteAllText(LastPage,(page-2).ToString());
    }
}
