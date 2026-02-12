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
        private By DownloadBtn => By.XPath("//a[text()='Download free'] | //a[contains(translate(@title, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'download')] | //a[@data-testid='photo-header-download-button'] | //a[contains(@href, '/download')]");
        private By PhotoTitle => By.TagName("h1");

        public PhotoDetailsPage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(45));
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

            // Dismiss common overlays (cookie banners, modals) that may block clicks
            DismissOverlays();

            // Find the download button, scroll into view and wait until clickable
            var btn = wait.Until(d => {
                try {
                    DismissOverlays();
                    var el = d.FindElements(DownloadBtn).FirstOrDefault();
                    if (el != null) {
                        ((IJavaScriptExecutor)d).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", el);
                        if (el.Displayed && el.Enabled) return el;
                    }
                } catch (StaleElementReferenceException) { }
                return null;
            });

            // Fallback: try open the 'more' menu (three dots) and locate a download link inside
            if (btn == null)
            {
                Console.WriteLine("Download button not found via primary locator. Trying menu fallback...");
                btn = TryOpenMenuAndFindDownload();
            }
            
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

        // Cố gắng đóng các overlay/phần tử che chắn (cookie banner, modal) nếu có
        private void DismissOverlays()
        {
            try
            {
                // Các locator phổ biến cho nút chấp nhận/cài đặt cookie hoặc đóng modal
                var candidates = new By[] {
                    By.XPath("//button[contains(., 'Accept') or contains(., 'Accept all') or contains(., 'I agree') or contains(., 'Got it') or contains(., 'Agree')]") ,
                    By.CssSelector("button[aria-label='Close'], button[title='Close'], button.dismiss, .cookie-accept, .cookie-consent button"),
                    By.XPath("//button[contains(@class, 'dismiss') or contains(@aria-label, 'dismiss') or contains(@class, 'close')]")
                };

                foreach (var sel in candidates)
                {
                    var els = driver.FindElements(sel);
                    foreach (var el in els)
                    {
                        try
                        {
                            if (el.Displayed)
                            {
                                try { el.Click(); } catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", el); }
                                Console.WriteLine($"Dismissed overlay using {sel}");
                                System.Threading.Thread.Sleep(500);
                                return;
                            }
                        }
                        catch { /* ignore individual element errors */ }
                    }
                }

                // Nếu modal 'Join Unsplash' hiện lên, đóng nó
                var joinModal = driver.FindElements(By.XPath("//h1[contains(., 'Join Unsplash') or contains(., 'Unsplash+')] ")).FirstOrDefault();
                if (joinModal != null)
                {
                    var closeBtn = driver.FindElements(By.XPath("//button[contains(@class, 'dismiss') or @aria-label='Close']")).FirstOrDefault();
                    if (closeBtn != null && closeBtn.Displayed)
                    {
                        try { closeBtn.Click(); } catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", closeBtn); }
                        Console.WriteLine("Closed Join modal.");
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
            catch (Exception)
            {
                // Không cần crash nếu không thể đóng overlay
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
            try
            {
                var locators = new By[] {
                    By.XPath("//button[contains(translate(@aria-label, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'bookmark') or contains(translate(@aria-label, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'collection')]") ,
                    By.XPath("//button[contains(translate(@aria-label, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'save') or contains(., 'Save')]") ,
                    By.XPath("//button[contains(@title, 'Remove') or contains(., 'Remove')]") ,
                    By.CssSelector("button[data-testid*='save']"),
                    By.XPath("//button[.//svg[contains(@class, 'bookmarked')]]")
                };

                var bookmarkWait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                return bookmarkWait.Until(d => {
                    try {
                        IWebElement el = null;
                        foreach (var locator in locators)
                        {
                            var found = d.FindElements(locator).FirstOrDefault();
                            if (found != null && found.Displayed)
                            {
                                el = found;
                                break;
                            }
                        }

                        if (el == null) return false;

                        string label = el.GetAttribute("aria-label") ?? "";
                        string title = el.GetAttribute("title") ?? "";
                        bool isBookmarked = label.Contains("Remove", StringComparison.OrdinalIgnoreCase) ||
                                           title.Contains("Remove", StringComparison.OrdinalIgnoreCase) ||
                                           el.FindElements(By.XPath(".//*[contains(@class, 'bookmarked')]")).Any();

                        if (!isBookmarked) Console.WriteLine($"Current label: '{label}', Title: '{title}' - Waiting for 'Remove' or 'bookmarked' class...");
                        return isBookmarked;
                    }
                    catch (StaleElementReferenceException)
                    {
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking bookmark status: {ex.Message}");
                        return false;
                    }
                });
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("Timeout waiting for bookmark state to be active.");
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

        // Thử mở menu thêm (three dots) để tìm link download nếu button chính không hiện
        private IWebElement TryOpenMenuAndFindDownload()
        {
            try
            {
                var menuSelectors = new By[] {
                    By.XPath("//button[contains(@aria-label, 'More') or contains(@aria-label, 'more') or contains(@aria-label, 'Options') or contains(@aria-label, 'Share')]") ,
                    By.CssSelector("button[aria-label*='more']"),
                    By.CssSelector("button[aria-label*='share']"),
                    By.XPath("//button[contains(@class,'more') or contains(@class,'options')]")
                };

                foreach (var sel in menuSelectors)
                {
                    var menus = driver.FindElements(sel);
                    foreach (var m in menus)
                    {
                        try {
                            if (m.Displayed) {
                                try { m.Click(); } catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", m); }
                                System.Threading.Thread.Sleep(500);

                                // Try find download link inside menu
                                var candidate = driver.FindElements(By.XPath("//a[contains(@href, '/download') or contains(translate(., 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'download')]"))
                                    .FirstOrDefault(e => e.Displayed);
                                if (candidate != null) {
                                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", candidate);
                                    return candidate;
                                }
                            }
                        } catch { }
                    }
                }

                // As a last resort, search the whole DOM for anchors with '/download'
                var fallback = driver.FindElements(By.CssSelector("a[href*='/download']")).FirstOrDefault(e => e.Displayed);
                if (fallback != null) return fallback;
            }
            catch (Exception) { }
            return null;
        }
    }
}
