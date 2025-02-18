using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Chrome;
using SeleniumExtras.WaitHelpers;

using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

class ScreenshotScript
{
    public static int TakeScreenshot(IWebDriver driver, string savePath, List<string> themeXpath, List<string> themeXpathOR)
    {
        try
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

            // **Wait for the main container to load**
            Stopwatch sw = Stopwatch.StartNew();
            IWebElement mainElement = wait.Until(ExpectedConditions.ElementExists(By.CssSelector(".outer-pages-wrapper")));
            Console.WriteLine($"Main element loaded in {sw.ElapsedMilliseconds} ms");

            // **Wait for all child elements to be fully loaded & visible**
            bool allVisibleChildrenLoaded = wait.Until(d =>
            {
                var children = mainElement.FindElements(By.CssSelector("*"));
                var visibleChildren = children.Where(child => child.Displayed).ToList();
                Console.WriteLine($"Visible children count: {visibleChildren.Count}");
                return visibleChildren.Count > 5; // Ensure at least 10 elements are visible
            });

            if (!allVisibleChildrenLoaded)
            {
                Console.WriteLine("Not all children loaded.");
                return -1;
            }

            // Extract MaxPage
            if (DownloadInfoScript.MaxPage == 0)
            {
                int maxPage = ExtractNumberFromElement(driver, "//*[@fxhide.lt-sm]", null);
                Console.WriteLine($"Current MaxPage: {maxPage}");
                DownloadInfoScript.MaxPage = maxPage;
            }

            // Extract Num
            int num = ExtractNumberFromElement(driver, themeXpath, themeXpathOR);
            if (num == -1) return -2;

            // Close popup for better OCR
            var popups = driver.FindElements(By.CssSelector(".introjs-skipbutton"));
            if (popups.Count > 0)
            {
                popups[0].Click();
                Thread.Sleep(100); // Keeping your sleep timing
            }

            // **Ensure all elements in main container are fully rendered before taking the screenshot**
            wait.Until(ExpectedConditions.ElementExists(By.TagName("body")));

            // **Get Device Pixel Ratio for scaling**
            IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;
            double devicePixelRatio = Convert.ToDouble(jsExecutor.ExecuteScript("return window.devicePixelRatio;"));

            var elementLocation = mainElement.Location;
            var elementSize = mainElement.Size;

            int adjustedX = (int)(elementLocation.X * devicePixelRatio);
            int adjustedY = (int)(elementLocation.Y * devicePixelRatio);
            int adjustedWidth = (int)(elementSize.Width * devicePixelRatio);
            int adjustedHeight = (int)(elementSize.Height * devicePixelRatio);

            // **Take full-page screenshot**
            Screenshot fullScreenshot = ((ITakesScreenshot)driver).GetScreenshot();
            string tempPath = Path.Combine(AppContext.BaseDirectory, "tempScreenshot.png");
            fullScreenshot.SaveAsFile(tempPath);

            // **Crop the screenshot to capture only the required area**
            var cropArea = new Rectangle(adjustedX, adjustedY, adjustedWidth, adjustedHeight);

            using (var fullImage = new Bitmap(tempPath))
            {
                cropArea.Width = Math.Min(cropArea.Width, fullImage.Width - cropArea.X);
                cropArea.Height = Math.Min(cropArea.Height, fullImage.Height - cropArea.Y);

                using (var croppedImage = fullImage.Clone(cropArea, fullImage.PixelFormat))
                {
                    croppedImage.Save(savePath);
                    Console.WriteLine($"Cropped screenshot saved at: {savePath}");
                }
            }

            File.Delete(tempPath);

            return num <= DownloadInfoScript.MaxPage ? num : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while taking screenshot: {ex.Message}");
            if(ex.Message == @" The directory placeholder of the filename placeholder\pishki0.png does not exist.") return -3;
            return -1;
        }
    }

public static IWebDriver GetHeadlessChromeDriver()
{
    ChromeOptions options = new ChromeOptions();
    options.AddArgument("--headless=new"); // NEW headless mode (fixes viewport issues)
    options.AddArgument("--disable-software-rasterizer");
    options.AddArgument("--enable-gpu");
    options.AddArgument("--disable-extensions");
    options.AddArgument("--window-size=1920x1080");
    options.AddArgument("--start-maximized"); 
    options.AddArgument("--force-device-scale-factor=1");
    options.AddArgument("--max-texture-size=8192");
    options.AddArgument("--no-sandbox");
    options.AddArgument("--disable-dev-shm-usage");
    options.AddArgument("--disable-blink-features=AutomationControlled");

    IWebDriver driver = new ChromeDriver(options);

    // Force correct viewport size using DevTools
    IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;
    jsExecutor.ExecuteScript("document.body.style.zoom='100%'");

    // **Ensure the window is maximized properly**
    driver.Manage().Window.Size = new System.Drawing.Size(1920, 1080);

    return driver;
}

    public static int ExtractNumberFromElement(IWebDriver driver, object childClass, List<string> childOrClass)
    {
        try
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(0.5));

            string xpath;
            if (childClass is string singleClass)
            {
                xpath = singleClass;
            }
            else if (childClass is List<string> classParts)
            {
                string childConditions = string.Join(" and ", classParts.Select(part => $"contains(@class, '{part}')"));
                string orConditions = string.Join(" or ", childOrClass.Select(part => $"contains(@class, '{part}')"));
                xpath = $"//*[{childConditions} and ({orConditions})]";
            }
            else
            {
                throw new ArgumentException("Invalid childClass argument.");
            }

            IWebElement element = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath(xpath)));
            string elementText = element.Text.Trim();

            string[] parts = elementText.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            string numberPart = parts.Last();
            int extractedNumber = int.Parse(numberPart);

            Console.WriteLine($"Extracted Number: {extractedNumber}");
            return extractedNumber;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting number: {ex.Message}");
            return -1;
        }
    }
}
