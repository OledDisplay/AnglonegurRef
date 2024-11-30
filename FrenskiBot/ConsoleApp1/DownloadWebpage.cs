using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenCvSharp;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

class DownloadWebpage
{
    public static void CaptureAndProcessPage(string url, string screenshotPath, string outputPath)
    {
        // Step 1: Set up Selenium and take a screenshot
        using (var driver = new ChromeDriver())
        {
            driver.Manage().Window.Size = new System.Drawing.Size(1920, 1080); // Set browser size
            driver.Navigate().GoToUrl(url);

            // Take a full-page screenshot
            Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
            screenshot.SaveAsFile(screenshotPath, ScreenshotImageFormat.Png);
            Console.WriteLine($"Screenshot saved to: {screenshotPath}");
        }

        // Step 2: Load the screenshot and filter the usable part
        Mat screenshotImage = Cv2.ImRead(screenshotPath, ImreadModes.Color);

        // Example: Define a region of interest (ROI) to crop the usable area
        // Replace this with the actual coordinates of the desired content
        Rect roi = new Rect(100, 200, 800, 400); // x, y, width, height
        Mat croppedImage = new Mat(screenshotImage, roi);

        // Step 3: Save the cropped image
        Cv2.ImWrite(outputPath, croppedImage);
        Console.WriteLine($"Processed image saved to: {outputPath}");
    }
}
