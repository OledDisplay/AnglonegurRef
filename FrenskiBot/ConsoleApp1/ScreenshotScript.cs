
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using System.Drawing;
using System.Linq;

class ScreenshotScript
{
    public static void TakeScreenshot(IWebDriver driver, string savePath)
    {
     try
{
    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

    // Wait for the main container to load
    IWebElement mainElement = wait.Until(d =>
    {
        var el = d.FindElement(By.CssSelector(".outer-pages-wrapper")); // Adjust the selector as needed
        return el.Displayed ? el : null;
    });

    Console.WriteLine("Main element loaded.");

    // Wait until visible children of the main container are loaded
    bool allVisibleChildrenLoaded = wait.Until(d =>
    {
        var children = mainElement.FindElements(By.CssSelector("*"));
        var visibleChildren = children.Where(child => child.Displayed).ToList();
        Console.WriteLine($"Visible children count: {visibleChildren.Count}");
        return visibleChildren.Count > 0; // Ensure some visible children exist
    });

    if (allVisibleChildrenLoaded)
    {
        Console.WriteLine("All visible children loaded. Taking screenshot...");

        // Take a full-page screenshot
        Screenshot fullScreenshot = ((ITakesScreenshot)driver).GetScreenshot();
        string tempPath = Path.Combine(AppContext.BaseDirectory, "tempScreenshot.png");
        fullScreenshot.SaveAsFile(tempPath);

        // Get main element location and size
        var elementLocation = mainElement.Location;
        var elementSize = mainElement.Size;

        // Calculate the center of the element
        int centerX = elementLocation.X + (elementSize.Width / 2);
        int centerY = elementLocation.Y + (elementSize.Height / 2);

        // Define the crop area using the center as origin
        var cropArea = new Rectangle(
            centerX - (elementSize.Width / 2), // Adjust X to start from the center
            centerY - (elementSize.Height / 2), // Adjust Y to start from the center
            elementSize.Width,
            elementSize.Height
        );
        Console.WriteLine($"Hi {elementSize.Width");

        // Load the full screenshot into a Bitmap
        using (var fullImage = new Bitmap(tempPath))
        {
            // Crop the desired region
            using (var croppedImage = fullImage.Clone(cropArea, fullImage.PixelFormat))
            {
                // Save the cropped image
                croppedImage.Save(savePath);
                Console.WriteLine($"Cropped screenshot saved at: {savePath}");
            }
        }

        // Cleanup temporary file
        File.Delete(tempPath);
    }
}
catch (WebDriverTimeoutException)
{
    Console.WriteLine("Timeout waiting for visible children to load.");
}
catch (NoSuchElementException ex)
{
    Console.WriteLine($"Element not found: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error while taking screenshot: {ex.Message}");
}

}
}
