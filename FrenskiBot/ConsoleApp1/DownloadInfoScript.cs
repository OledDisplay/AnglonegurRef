using System;
using System.IO;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Collections.Generic;


class DownloadInfoScript {
        private static IWebDriver _driver;

        public static void DownloadScript(string pageUrl,string root, string LoginName, string LoginPass) {
         int page = 2;
         string DirectoryTempPath = Path.Combine(root, "Uchebnik");
         Directory.CreateDirectory(DirectoryTempPath);

         // Initialize webdriver
         InitializeDriver();

         // Log in
         LogInWebPage.LogIn(pageUrl + page.ToString(), LoginName, LoginPass, _driver); // page url, log name, log pass, webdriver instanse
         
         //Take screenshot
         string ScreenshotPath  = Path.Combine(DirectoryTempPath, "pishki.png");
         ScreenshotScript.TakeScreenshot(_driver,ScreenshotPath); // webdriver, url, save path

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
