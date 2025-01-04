using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Chrome;
using SeleniumExtras.WaitHelpers;

using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;

class ScreenshotScript
{
    public static int TakeScreenshot(IWebDriver driver, string savePath, List<string> themeXpath, List<string> themeXpathOR)
    {
        try
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

            // Wait for the main container to load
            IWebElement mainElement = wait.Until(d =>
            {
                var el = d.FindElement(By.CssSelector(".outer-pages-wrapper"));
                return el.Displayed ? el : null;
            });

            Console.WriteLine("Main element loaded.");

            // Wait until visible children of the main container are loaded
            bool allVisibleChildrenLoaded = wait.Until(d =>
            {
                var children = mainElement.FindElements(By.CssSelector("*"));
                var visibleChildren = children.Where(child => child.Displayed).ToList();
                Console.WriteLine($"Visible children count: {visibleChildren.Count}");
                return visibleChildren.Count > 10; // Ensure at least some visible children exist
            });

            if (allVisibleChildrenLoaded)
            {
                // Extract MaxPage
                if (DownloadInfoScript.MaxPage == 0)
                {
                    int maxPage = ExtractNumberFromElement(driver, "//*[@fxhide.lt-sm]", null);
                    Console.WriteLine($"Current MaxPage: {maxPage}");
                    DownloadInfoScript.MaxPage = maxPage;
                }

                // Extract Num
                int num = ExtractNumberFromElement(driver, themeXpath, themeXpathOR); // -1 for error, meaning the page has to be skipped
                if (num == -1) return -2;

                // close popup for better ocr
                if(driver.FindElements(By.CssSelector(".introjs-skipbutton")).Count > 0) {
                 IWebElement popup = driver.FindElement(By.CssSelector(".introjs-skipbutton"));
                 popup.Click();
                }

                // Get Device Pixel Ratio for scaling
                IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;
                double devicePixelRatio = Convert.ToDouble(jsExecutor.ExecuteScript("return window.devicePixelRatio;"));

                var elementLocation = mainElement.Location;
                var elementSize = mainElement.Size;

                int adjustedX = (int)(elementLocation.X * devicePixelRatio);
                int adjustedY = (int)(elementLocation.Y * devicePixelRatio);
                int adjustedWidth = (int)(elementSize.Width * devicePixelRatio);
                int adjustedHeight = (int)(elementSize.Height * devicePixelRatio);

                Screenshot fullScreenshot = ((ITakesScreenshot)driver).GetScreenshot();
                string tempPath = Path.Combine(AppContext.BaseDirectory, "tempScreenshot.png");
                fullScreenshot.SaveAsFile(tempPath);

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

                if (num <= DownloadInfoScript.MaxPage)
                {
                    return num;
                }
                else
                {
                    return 0;
                }
            }

            Console.WriteLine("Visible children not loaded.");
            return -1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while taking screenshot: {ex.Message}");
            return -1;
        }
    }

    public static IWebDriver GetHeadlessChromeDriver()
    {
        ChromeOptions options = new ChromeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-extensions");
        options.AddArgument("--window-size=1920x1080");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");

        return new ChromeDriver(options);
    }

    public static int ExtractNumberFromElement(IWebDriver driver, object childClass, List<string> childOrClass)
    {
        try
        {
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(0.1));

            string xpath;

            // Build XPath dynamically
            if (childClass is string singleClass)
            {
                xpath = singleClass;
            }
            else if (childClass is List<string> classParts)
            {
                string childConditions = string.Join(" and ", classParts.Select(part => $"contains(@class, '{part}')")); // Nessesary condtions
                string orConditions = string.Join(" or ", childOrClass.Select(part => $"contains(@class, '{part}')")); // Or conditions
                xpath = $"//*[{childConditions} and ({orConditions})]";
            }
            else
            {
                throw new ArgumentException("Invalid childClass argument.");
            }

            // Locate the element
            IWebElement element = wait.Until(d =>
            {
                var el = d.FindElement(By.XPath(xpath));
                return el.Displayed ? el : null;
            });


            // Extract text
            string elementText = element.Text.Trim();

            // Extract the number from the text
            string[] parts = elementText.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            string numberPart = parts.Last(); // Extract the number
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
