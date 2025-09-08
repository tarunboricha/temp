using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class ElementScreenshotHelper
{
    public static void CaptureFullElementScreenshot(IWebDriver driver, IWebElement element, string filePath)
    {
        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

        // Get element scroll sizes
        int scrollHeight = Convert.ToInt32(js.ExecuteScript("return arguments[0].scrollHeight", element));
        int clientHeight = Convert.ToInt32(js.ExecuteScript("return arguments[0].clientHeight", element));

        int scrollWidth = Convert.ToInt32(js.ExecuteScript("return arguments[0].scrollWidth", element));
        int clientWidth = Convert.ToInt32(js.ExecuteScript("return arguments[0].clientWidth", element));

        // Get element location on page
        var elementLocation = element.Location;

        // Prepare a big blank image
        Bitmap finalImage = new Bitmap(scrollWidth, scrollHeight);

        using (Graphics g = Graphics.FromImage(finalImage))
        {
            for (int y = 0; y < scrollHeight; y += clientHeight)
            {
                for (int x = 0; x < scrollWidth; x += clientWidth)
                {
                    // Scroll inside the element
                    js.ExecuteScript($"arguments[0].scrollTo({x}, {y});", element);
                    System.Threading.Thread.Sleep(200); // small delay to let scroll render

                    // Take full screenshot
                    Screenshot screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                    using (MemoryStream memStream = new MemoryStream(screenshot.AsByteArray))
                    using (Bitmap fullImage = new Bitmap(memStream))
                    {
                        // Crop only visible part of element
                        Rectangle cropArea = new Rectangle(
                            elementLocation.X,
                            elementLocation.Y,
                            Math.Min(clientWidth, scrollWidth - x),
                            Math.Min(clientHeight, scrollHeight - y)
                        );

                        using (Bitmap elementPart = fullImage.Clone(cropArea, fullImage.PixelFormat))
                        {
                            // Paste it into final image at the right place
                            g.DrawImage(elementPart, x, y);
                        }
                    }
                }
            }
        }

        finalImage.Save(filePath, ImageFormat.Png);
    }
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
using OpenQA.Selenium.Support.Extensions;

public static class ElementScreenshotHelper
{
    /// <summary>
    /// Takes a screenshot of a specific WebElement, handling elements with internal scrollbars
    /// </summary>
    /// <param name="driver">The WebDriver instance</param>
    /// <param name="element">The WebElement to capture</param>
    /// <param name="scrollToElement">Whether to scroll the element into view first (default: true)</param>
    /// <param name="captureFullScrollableContent">Whether to capture full content of scrollable elements (default: false)</param>
    /// <returns>Bitmap image of the element</returns>
    public static Bitmap TakeElementScreenshot(IWebDriver driver, IWebElement element, 
                                             bool scrollToElement = true, 
                                             bool captureFullScrollableContent = false)
    {
        try
        {
            // Step 1: Scroll element into view if requested
            if (scrollToElement)
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
                System.Threading.Thread.Sleep(500); // Wait for scroll animation
            }

            // Step 2: Handle internal scrollbar if element is scrollable
            if (captureFullScrollableContent && IsElementScrollable(driver, element))
            {
                // Scroll to top of the element's content first
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollTop = 0;", element);
                System.Threading.Thread.Sleep(300);
            }

            // Step 3: Take screenshot using Selenium 4's native method
            byte[] elementScreenshot = element.GetScreenshotAs(OutputType.BYTES);

            // Step 4: Convert byte array to Bitmap
            using (var ms = new MemoryStream(elementScreenshot))
            {
                return new Bitmap(ms);
            }
        }
        catch (Exception ex)
        {
            // Fallback to manual crop method for older Selenium versions or if native method fails
            Console.WriteLine($"Native element screenshot failed, using fallback method: {ex.Message}");
            return TakeElementScreenshotFallback(driver, element, scrollToElement);
        }
    }

    /// <summary>
    /// Fallback method using full page screenshot and cropping (for Selenium 3 compatibility)
    /// </summary>
    private static Bitmap TakeElementScreenshotFallback(IWebDriver driver, IWebElement element, bool scrollToElement)
    {
        try
        {
            if (scrollToElement)
            {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
                System.Threading.Thread.Sleep(500);
            }

            // Take full page screenshot
            byte[] screenshotBytes = ((ITakesScreenshot)driver).GetScreenshot().AsByteArray;

            using (var fullScreenshot = new Bitmap(new MemoryStream(screenshotBytes)))
            {
                // Get element location and size
                var location = element.Location;
                var size = element.Size;

                // Create rectangle for cropping
                var cropArea = new Rectangle(location.X, location.Y, size.Width, size.Height);

                // Ensure crop area is within image boundaries
                cropArea.X = Math.Max(0, cropArea.X);
                cropArea.Y = Math.Max(0, cropArea.Y);
                cropArea.Width = Math.Min(cropArea.Width, fullScreenshot.Width - cropArea.X);
                cropArea.Height = Math.Min(cropArea.Height, fullScreenshot.Height - cropArea.Y);

                // Crop and return the element image
                return fullScreenshot.Clone(cropArea, fullScreenshot.PixelFormat);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to capture element screenshot: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if an element has internal scrollbars
    /// </summary>
    private static bool IsElementScrollable(IWebDriver driver, IWebElement element)
    {
        try
        {
            var jsExecutor = (IJavaScriptExecutor)driver;

            // Check if element has scrollable content (scrollHeight > clientHeight or scrollWidth > clientWidth)
            var hasVerticalScroll = (bool)jsExecutor.ExecuteScript(
                "return arguments[0].scrollHeight > arguments[0].clientHeight;", element);

            var hasHorizontalScroll = (bool)jsExecutor.ExecuteScript(
                "return arguments[0].scrollWidth > arguments[0].clientWidth;", element);

            return hasVerticalScroll || hasHorizontalScroll;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Advanced method: Captures full scrollable content by stitching multiple screenshots
    /// Use this for elements with lots of internal scrollable content
    /// </summary>
    public static Bitmap TakeFullScrollableElementScreenshot(IWebDriver driver, IWebElement element)
    {
        try
        {
            // Scroll element into view
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
            System.Threading.Thread.Sleep(500);

            var jsExecutor = (IJavaScriptExecutor)driver;

            // Get element's scroll dimensions
            var scrollHeight = Convert.ToInt32(jsExecutor.ExecuteScript("return arguments[0].scrollHeight;", element));
            var clientHeight = Convert.ToInt32(jsExecutor.ExecuteScript("return arguments[0].clientHeight;", element));

            if (scrollHeight <= clientHeight)
            {
                // Element is not scrollable, take regular screenshot
                return TakeElementScreenshot(driver, element, false);
            }

            // Reset scroll position to top
            jsExecutor.ExecuteScript("arguments[0].scrollTop = 0;", element);
            System.Threading.Thread.Sleep(300);

            // Take first screenshot
            var firstScreenshot = TakeElementScreenshot(driver, element, false);
            var elementWidth = firstScreenshot.Width;

            // Calculate how many screenshots we need
            var scrollStep = clientHeight * 3 / 4; // 75% overlap to ensure no content is missed
            var screenshots = new List<Bitmap> { firstScreenshot };
            var currentScrollTop = 0;

            while (currentScrollTop + clientHeight < scrollHeight)
            {
                currentScrollTop += scrollStep;
                jsExecutor.ExecuteScript($"arguments[0].scrollTop = {currentScrollTop};", element);
                System.Threading.Thread.Sleep(300);

                screenshots.Add(TakeElementScreenshot(driver, element, false));
            }

            // Stitch screenshots together (simplified version - in production you'd want better stitching logic)
            var totalHeight = scrollHeight;
            var stitchedBitmap = new Bitmap(elementWidth, totalHeight);

            using (var graphics = Graphics.FromImage(stitchedBitmap))
            {
                var currentY = 0;
                for (int i = 0; i < screenshots.Count; i++)
                {
                    var screenshot = screenshots[i];
                    var drawHeight = (i == screenshots.Count - 1) ? 
                        Math.Min(screenshot.Height, totalHeight - currentY) : scrollStep;

                    graphics.DrawImage(screenshot, 0, currentY, elementWidth, drawHeight);
                    currentY += scrollStep;
                }
            }

            // Cleanup
            screenshots.ForEach(s => s.Dispose());

            return stitchedBitmap;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to capture full scrollable element screenshot: {ex.Message}", ex);
        }
    }
}
