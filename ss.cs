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
