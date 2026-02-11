using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.IO;
using System.Linq;

namespace UnsplashAutomation.Pages
{
    public class PhotoDetailsPage
    {
        private readonly IWebDriver driver;
        private readonly WebDriverWait wait;

        // Trình giữ chỗ cho các Locator
        private By DownloadBtn => By.XPath("//a[text()='Download free'] | //a[contains(@title, 'Download')] | //a[@data-testid='photo-header-download-button'] | //a[contains(@href, '/download?force=true')]");
        private By PhotoTitle => By.TagName("h1");

        public PhotoDetailsPage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
        }

        public void ClickDownload() // Nhấn nút tải ảnh xuống
        {
            // Đảm bảo đang ở trang chi tiết ảnh
            if (!driver.Url.Contains("/photos/"))
            {
                Console.WriteLine("Not on a photo details page. Current URL: " + driver.Url);
                return;
            }

            Console.WriteLine("Finding and clicking download button...");
            var btn = wait.Until(ExpectedConditions.ElementToBeClickable(DownloadBtn));
            
            // Lấy ID ảnh từ URL để log
            string photoId = GetPhotoId();
            Console.WriteLine($"Downloading photo ID: {photoId}");

            // Lấy href
            string? downloadUrl = btn.GetAttribute("href");
            if (string.IsNullOrEmpty(downloadUrl) || !downloadUrl.Contains(photoId))
            {
                Console.WriteLine("Download button href does not match photo ID or is empty. Retrying locator...");
                btn = driver.FindElements(DownloadBtn).FirstOrDefault(e => (e.GetAttribute("href") ?? "").Contains(photoId)) ?? btn;
                downloadUrl = btn.GetAttribute("href");
            }

            try { 
                btn.Click(); 
                Console.WriteLine("Click performed.");
            } catch { 
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", btn); 
                Console.WriteLine("JS click performed.");
            }
            
            // Chỉ navigate trực tiếp nếu click không có tác dụng và URL hợp lệ
            System.Threading.Thread.Sleep(5000);
            if (!string.IsNullOrEmpty(downloadUrl) && downloadUrl.Contains(photoId) && !driver.PageSource.Contains("Thanks"))
            {
                if (!downloadUrl.Contains("force=true")) {
                    downloadUrl += (downloadUrl.Contains("?") ? "&" : "?") + "force=true";
                }
                Console.WriteLine($"Click might have failed. Navigating directly to: {downloadUrl}");
                driver.Navigate().GoToUrl(downloadUrl);
                System.Threading.Thread.Sleep(3000);
            }
        }

        public string GetPhotoId() // Lấy mã ID của tấm ảnh từ URL hiện tại
        {
            // URL thường có dạng: https://unsplash.com/photos/abc-xyz-123
            // "abc-xyz" là phần mô tả, "123" là ID thực tế dùng trong tên file
            string url = driver.Url;
            string lastSegment = url.TrimEnd('/').Split('/').Last();
            
            // Nếu ID có gạch ngang, ID thực tế thường là phần cuối cùng (ví dụ: photo-name-ID)
            if (lastSegment.Contains("-"))
            {
                return lastSegment.Split('-').Last();
            }
            return lastSegment;
        }

        public string GetPhotoIdFromUrl() => GetPhotoId(); // Alias cho Scenario 1

        public bool IsBookmarked() // Kiểm tra xem ảnh hiện tại có đang được bookmark hay không
        {
            Console.WriteLine($"Checking if photo is bookmarked... URL: {driver.Url}, Title: {driver.Title}");
            IWebElement bookmarkBtn = null;
            try
            {
                // Wait for the button to be visible first
                // Use a more robust locator covering case variations and text content
                var bookmarkBtnLocator = By.XPath("//button[contains(translate(@aria-label, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'bookmark') or contains(translate(@aria-label, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'collection') or contains(., 'Collect') or contains(., 'Bn')] | //button[.//svg[contains(@class, 'bookmarked')]]");
                bookmarkBtn = wait.Until(ExpectedConditions.ElementIsVisible(bookmarkBtnLocator));
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("Timeout: Bookmark button not found on the page.");
                return false;
            }

            try
            {
                // Wait for the state to reflect "bookmarked" (e.g. label contains "Remove" or has class "bookmarked")
                return wait.Until(d => {
                    try {
                        string label = bookmarkBtn.GetAttribute("aria-label") ?? "";
                        string title = bookmarkBtn.GetAttribute("title") ?? "";
                        bool isBookmarked = label.Contains("Remove", StringComparison.OrdinalIgnoreCase) || 
                                          title.Contains("Remove", StringComparison.OrdinalIgnoreCase) || 
                                          bookmarkBtn.FindElements(By.XPath(".//*[contains(@class, 'bookmarked')]")).Any();
                        
                        if (!isBookmarked) {
                            Console.WriteLine($"Current label: '{label}', Title: '{title}' - Waiting for 'Remove' or 'bookmarked' class..."); 
                        }
                        return isBookmarked;
                    } catch (StaleElementReferenceException) {
                        // Element might have been re-rendered, find it again
                        var bookmarkBtnLocator = By.XPath("//button[contains(@aria-label, 'Bookmark') or contains(@aria-label, 'Collection')] | //button[.//svg[contains(@class, 'bookmarked')]]");
                        bookmarkBtn = d.FindElement(bookmarkBtnLocator);
                        return false;
                    }
                });
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("Timeout waiting for bookmark state to be active.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking bookmark status: {ex.Message}");
                return false;
            }
        }

        // Đợi file tải xong (tăng lên 120s)
        public bool WaitForDownloadToComplete(string expectedPattern, int timeoutSeconds = 120) 
        {
            Console.WriteLine($"Waiting for file matching '*{expectedPattern}*' in {Utilities.TestData.DownloadDirectory}...");
            string downloadPath = Utilities.TestData.DownloadDirectory;
            
            DateTime endWait = DateTime.Now.AddSeconds(timeoutSeconds);
            long lastSize = -1;
            
            while (DateTime.Now < endWait)
            {
                var allFiles = Directory.GetFiles(downloadPath);
                if (allFiles.Any())
                {
                    bool inProgress = false;
                    foreach (var file in allFiles)
                    {
                        string fileName = Path.GetFileName(file);
                        long currentSize = new FileInfo(file).Length;
                        
                        // Kiểm tra nếu file là file đang tải (.crdownload hoặc .tmp)
                        if (fileName.EndsWith(".crdownload") || fileName.EndsWith(".tmp"))
                        {
                            inProgress = true;
                            if (currentSize > lastSize)
                            {
                                Console.WriteLine($"Download in progress: {fileName} ({currentSize} bytes and growing...)");
                                lastSize = currentSize;
                            }
                        }
                        // Kiểm tra nếu file đã xong và khớp pattern (chỉ kiểm tra tên file)
                        else if (fileName.Contains(expectedPattern))
                        {
                            if (currentSize > 1024) // Giả định file đã tải xong nếu kích thước lớn hơn 1KB
                            {
                                Console.WriteLine($"Download confirmed: {fileName} ({currentSize} bytes)");
                                return true;
                            }
                        }
                    }
                    
                    if (inProgress && lastSize == -1) {
                         // Console.WriteLine("Files detected but not yet growing or not matching.");
                    }
                }
                else
                {
                    // Console.WriteLine("Directory is empty.");
                }
                System.Threading.Thread.Sleep(3000); // Đợi 3 giây trước khi kiểm tra lại
            }
            Console.WriteLine($"Timeout reached. No file matching '{expectedPattern}' found in {downloadPath}.");
            // List files that were found for debugging
            if (Directory.Exists(downloadPath))
            {
                var files = Directory.GetFiles(downloadPath);
                Console.WriteLine($"Files found in directory: {string.Join(", ", files.Select(Path.GetFileName))}");
            }
            return false;
        }

        public void CleanUpDownloads() // Xóa các file đã tải để làm sạch thư mục sau khi test
        {
            if (Directory.Exists(Utilities.TestData.DownloadDirectory))
            {
                var files = Directory.GetFiles(Utilities.TestData.DownloadDirectory);
                foreach (var file in files)
                {
                    try { File.Delete(file); } catch { /* Ignore */ }
                }
                Console.WriteLine("Download directory cleaned.");
            }
        }
    }
}
