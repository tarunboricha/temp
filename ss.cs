public static void CaptureFullElementScreenshot(IWebDriver driver, IWebElement element, string filePath)
{
    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

    // Get element scroll sizes
    int scrollHeight = Convert.ToInt32(js.ExecuteScript("return arguments[0].scrollHeight", element));
    int clientHeight = Convert.ToInt32(js.ExecuteScript("return arguments[0].clientHeight", element));

    int scrollWidth = Convert.ToInt32(js.ExecuteScript("return arguments[0].scrollWidth", element));
    int clientWidth = Convert.ToInt32(js.ExecuteScript("return arguments[0].clientWidth", element));

    // Get element location in viewport
    var elementLocation = element.Location;

    // Prepare final stitched image
    Bitmap finalImage = new Bitmap(scrollWidth, scrollHeight);

    using (Graphics g = Graphics.FromImage(finalImage))
    {
        for (int y = 0; y < scrollHeight; y += clientHeight)
        {
            for (int x = 0; x < scrollWidth; x += clientWidth)
            {
                // Scroll inside element
                js.ExecuteScript($"arguments[0].scrollTo({x}, {y});", element);
                System.Threading.Thread.Sleep(200); // let rendering settle

                // Full screenshot of page
                Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                using (MemoryStream memStream = new MemoryStream(screenshot.AsByteArray))
                using (Bitmap fullImage = new Bitmap(memStream))
                {
                    // Define crop area in the screenshot
                    Rectangle cropArea = new Rectangle(
                        elementLocation.X,
                        elementLocation.Y,
                        Math.Min(clientWidth, scrollWidth - x),
                        Math.Min(clientHeight, scrollHeight - y)
                    );

                    // Clamp crop area to image bounds
                    int safeWidth = Math.Min(cropArea.Width, fullImage.Width - cropArea.X);
                    int safeHeight = Math.Min(cropArea.Height, fullImage.Height - cropArea.Y);

                    if (safeWidth > 0 && safeHeight > 0)
                    {
                        Rectangle safeCrop = new Rectangle(cropArea.X, cropArea.Y, safeWidth, safeHeight);

                        using (Bitmap elementPart = fullImage.Clone(safeCrop, fullImage.PixelFormat))
                        {
                            // Place cropped chunk into the correct spot
                            g.DrawImage(elementPart, x, y);
                        }
                    }
                }
            }
        }
    }

    finalImage.Save(filePath, ImageFormat.Png);
}













int safeWidth = Math.Min(cropArea.Width, fullImage.Width - cropArea.X);
int safeHeight = Math.Min(cropArea.Height, fullImage.Height - cropArea.Y);

Rectangle safeCrop = new Rectangle(cropArea.X, cropArea.Y, safeWidth, safeHeight);

using (Bitmap elementPart = fullImage.Clone(safeCrop, fullImage.PixelFormat))
{
    g.DrawImage(elementPart, x, y);
}






















using System;
using System.Drawing;
using System.IO;
using OpenQA.Selenium;
using System.Threading;

public static class Selenium3ElementScreenshotHelper
{
    /// <summary>
    /// Takes a screenshot of a specific WebElement in Selenium 3, handling elements with internal scrollbars
    /// </summary>
    /// <param name="driver">The WebDriver instance</param>
    /// <param name="element">The WebElement to capture</param>
    /// <param name="scrollToElement">Whether to scroll the element into view first (default: true)</param>
    /// <param name="resetElementScroll">Whether to reset element's internal scroll to top (default: true)</param>
    /// <returns>Bitmap image of the element</returns>
    public static Bitmap TakeElementScreenshot(IWebDriver driver, IWebElement element, 
                                             bool scrollToElement = true, 
                                             bool resetElementScroll = true)
    {
        try
        {
            var jsExecutor = (IJavaScriptExecutor)driver;

            // Step 1: Scroll element into view if requested
            if (scrollToElement)
            {
                jsExecutor.ExecuteScript("arguments[0].scrollIntoView({block: 'center', behavior: 'smooth'});", element);
                Thread.Sleep(800); // Wait for smooth scroll animation to complete
            }

            // Step 2: Handle internal scrollbar - reset to top if element is scrollable
            if (resetElementScroll && IsElementScrollable(driver, element))
            {
                // Reset element's internal scroll to top-left
                jsExecutor.ExecuteScript("arguments[0].scrollTop = 0; arguments[0].scrollLeft = 0;", element);
                Thread.Sleep(400); // Wait for internal scroll to complete
            }

            // Step 3: Take full page screenshot using Selenium 3's TakesScreenshot interface
            var screenshotDriver = (ITakesScreenshot)driver;
            byte[] screenshotBytes = screenshotDriver.GetScreenshot().AsByteArray;

            using (var fullPageImage = new Bitmap(new MemoryStream(screenshotBytes)))
            {
                // Step 4: Get element location and dimensions
                var elementLocation = element.Location;
                var elementSize = element.Size;

                // Step 5: Calculate crop area with boundary validation
                var cropArea = CalculateCropArea(elementLocation, elementSize, fullPageImage.Size);

                // Step 6: Crop the full page image to get element screenshot
                return CropImage(fullPageImage, cropArea);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to capture element screenshot in Selenium 3: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Takes a screenshot of a scrollable element's full content by stitching multiple crops
    /// </summary>
    /// <param name="driver">The WebDriver instance</param>
    /// <param name="element">The scrollable WebElement to capture</param>
    /// <returns>Bitmap image containing the full scrollable content</returns>
    public static Bitmap TakeFullScrollableElementScreenshot(IWebDriver driver, IWebElement element)
    {
        try
        {
            var jsExecutor = (IJavaScriptExecutor)driver;

            // Scroll element into view first
            jsExecutor.ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
            Thread.Sleep(500);

            // Get scroll dimensions
            var scrollHeight = Convert.ToInt32(jsExecutor.ExecuteScript("return arguments[0].scrollHeight;", element));
            var clientHeight = Convert.ToInt32(jsExecutor.ExecuteScript("return arguments[0].clientHeight;", element));
            var elementWidth = element.Size.Width;

            // If element is not scrollable, return regular screenshot
            if (scrollHeight <= clientHeight)
            {
                return TakeElementScreenshot(driver, element, false, true);
            }

            // Reset scroll to top
            jsExecutor.ExecuteScript("arguments[0].scrollTop = 0;", element);
            Thread.Sleep(300);

            // Calculate scroll step (75% overlap to avoid missing content)
            var scrollStep = (int)(clientHeight * 0.75);
            var screenshots = new List<Bitmap>();
            var currentScrollTop = 0;

            // Take screenshots while scrolling through content
            while (currentScrollTop < scrollHeight)
            {
                // Set scroll position
                jsExecutor.ExecuteScript($"arguments[0].scrollTop = {currentScrollTop};", element);
                Thread.Sleep(300);

                // Take screenshot of current view
                var currentScreenshot = TakeElementScreenshot(driver, element, false, false);
                screenshots.Add(currentScreenshot);

                // Calculate next scroll position
                currentScrollTop += scrollStep;

                // If we've reached the bottom, break
                if (currentScrollTop >= scrollHeight - clientHeight)
                {
                    break;
                }
            }

            // Stitch all screenshots together
            return StitchScreenshotsVertically(screenshots, scrollStep, scrollHeight);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to capture full scrollable element screenshot: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if an element has internal scrollable content
    /// </summary>
    private static bool IsElementScrollable(IWebDriver driver, IWebElement element)
    {
        try
        {
            var jsExecutor = (IJavaScriptExecutor)driver;

            // Check for vertical scrollability
            var hasVerticalScroll = (bool)jsExecutor.ExecuteScript(
                "return arguments[0].scrollHeight > arguments[0].clientHeight;", element);

            // Check for horizontal scrollability
            var hasHorizontalScroll = (bool)jsExecutor.ExecuteScript(
                "return arguments[0].scrollWidth > arguments[0].clientWidth;", element);

            return hasVerticalScroll || hasHorizontalScroll;
        }
        catch
        {
            return false; // Assume not scrollable if detection fails
        }
    }

    /// <summary>
    /// Calculates the crop area ensuring it stays within image boundaries
    /// </summary>
    private static Rectangle CalculateCropArea(Point elementLocation, Size elementSize, Size imageSize)
    {
        var cropArea = new Rectangle(elementLocation.X, elementLocation.Y, elementSize.Width, elementSize.Height);

        // Ensure crop area doesn't exceed image boundaries
        cropArea.X = Math.Max(0, Math.Min(cropArea.X, imageSize.Width - 1));
        cropArea.Y = Math.Max(0, Math.Min(cropArea.Y, imageSize.Height - 1));
        cropArea.Width = Math.Max(1, Math.Min(cropArea.Width, imageSize.Width - cropArea.X));
        cropArea.Height = Math.Max(1, Math.Min(cropArea.Height, imageSize.Height - cropArea.Y));

        return cropArea;
    }

    /// <summary>
    /// Crops an image to the specified rectangle
    /// </summary>
    private static Bitmap CropImage(Bitmap originalImage, Rectangle cropArea)
    {
        try
        {
            return originalImage.Clone(cropArea, originalImage.PixelFormat);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to crop image. Crop area: {cropArea}, Image size: {originalImage.Size}. Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Stitches multiple screenshots together vertically with proper overlap handling
    /// </summary>
    private static Bitmap StitchScreenshotsVertically(List<Bitmap> screenshots, int scrollStep, int totalHeight)
    {
        if (screenshots.Count == 0)
            throw new ArgumentException("No screenshots to stitch");

        if (screenshots.Count == 1)
            return new Bitmap(screenshots[0]); // Return copy of single screenshot

        var width = screenshots[0].Width;
        var stitchedImage = new Bitmap(width, totalHeight);

        using (var graphics = Graphics.FromImage(stitchedImage))
        {
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

            var currentY = 0;
            for (int i = 0; i < screenshots.Count; i++)
            {
                var screenshot = screenshots[i];

                if (i == 0)
                {
                    // First image - draw completely
                    graphics.DrawImage(screenshot, 0, 0);
                    currentY = scrollStep;
                }
                else if (i == screenshots.Count - 1)
                {
                    // Last image - calculate remaining height
                    var remainingHeight = totalHeight - currentY;
                    var sourceRect = new Rectangle(0, screenshot.Height - remainingHeight, width, remainingHeight);
                    var destRect = new Rectangle(0, currentY, width, remainingHeight);
                    graphics.DrawImage(screenshot, destRect, sourceRect, GraphicsUnit.Pixel);
                }
                else
                {
                    // Middle images - draw with overlap consideration
                    var overlapHeight = screenshot.Height - scrollStep;
                    var sourceRect = new Rectangle(0, overlapHeight, width, scrollStep);
                    var destRect = new Rectangle(0, currentY, width, scrollStep);
                    graphics.DrawImage(screenshot, destRect, sourceRect, GraphicsUnit.Pixel);
                    currentY += scrollStep;
                }
            }
        }

        // Cleanup temporary screenshots
        screenshots.ForEach(s => s.Dispose());

        return stitchedImage;
    }

    /// <summary>
    /// Helper method to get element info for debugging
    /// </summary>
    public static string GetElementInfo(IWebDriver driver, IWebElement element)
    {
        try
        {
            var jsExecutor = (IJavaScriptExecutor)driver;
            var location = element.Location;
            var size = element.Size;
            var scrollHeight = jsExecutor.ExecuteScript("return arguments[0].scrollHeight;", element);
            var clientHeight = jsExecutor.ExecuteScript("return arguments[0].clientHeight;", element);
            var isScrollable = IsElementScrollable(driver, element);

            return $"Element Info - Location: ({location.X}, {location.Y}), Size: {size.Width}x{size.Height}, " +
                   $"ScrollHeight: {scrollHeight}, ClientHeight: {clientHeight}, IsScrollable: {isScrollable}";
        }
        catch (Exception ex)
        {
            return $"Failed to get element info: {ex.Message}";
        }
    }
}
