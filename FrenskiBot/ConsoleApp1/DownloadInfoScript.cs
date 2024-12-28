using System;
using System.IO;
using System.Linq;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Collections.Generic;


class DownloadInfoScript {
        private static IWebDriver _driver;

        public static int MaxPage = 0; 

        public static void DownloadScript(string pageUrl,string root, string LoginName, string LoginPass) {
         int page = 2,output,num = 0,counter = 0;
         List<string> heightKeyEl = new List<string> {"h38","h42"}; // CONDITIONAL PARTS OF REPEATING THEMES
         List<string> themeKeyEl = new List<string> {"t","m0"};// NECESSARY PARTS OF REPEATING THEMES
         string BaseDir = Path.Combine(root, "Uchebnik"),DirectoryTempPath;
         Directory.CreateDirectory(BaseDir);
         
         // Initialize webdriver
         InitializeDriver();
         
         // Headless webdriver
         //IWebDriver _driver = ScreenshotScript.GetHeadlessChromeDriver();
        
         // Log in
         LogInWebPage.LogIn(pageUrl + page.ToString(), LoginName, LoginPass, _driver); // page url, log name, log pass, webdriver instanse
    
         //Ss loop (exit on 0, max page reached)

         if(!File.Exists("Uchebnik\\Data.flag")) {  
          if(Directory.Exists(BaseDir)) Directory.Delete(BaseDir,true); // if we have an unfull / broken pic dir things will sorta break
          do {
           DirectoryTempPath = Path.Combine(BaseDir, $"{num}");
           Directory.CreateDirectory(DirectoryTempPath);
           string ScreenshotPath  = Path.Combine(DirectoryTempPath, $"pishki{counter}.png");
           output = ScreenshotScript.TakeScreenshot(_driver,ScreenshotPath,themeKeyEl,heightKeyEl); // webdriver, url, save path
           counter ++;
           page += 2;
           if(output > 0 ) num = output;
           if(Directory.Exists(Path.Combine(BaseDir,$"{num}"))) counter = 0;
           _driver.Navigate().GoToUrl(pageUrl + page.ToString());
          }
          while (output != 0);

          File.Create("Uchebnik\\Data.flag").Dispose();
          Console.WriteLine("FLage file created");
         }
         else Console.WriteLine("Pages prescent");
                 
         // Close chromedriver
         //CleanupDriver();
        }
        public static void InitializeDriver(){
         if (_driver == null){
            Console.WriteLine("Creating a new ChromeDriver instance...");
            _driver = new ChromeDriver();
            _driver.Manage().Window.Maximize();
         }
       }
       
        public static void CleanupDriver(){
         if (_driver != null){
            _driver.Quit();
            _driver = null;
             Console.WriteLine("ChromeDriver instance cleaned up.");
         }
        }

}
