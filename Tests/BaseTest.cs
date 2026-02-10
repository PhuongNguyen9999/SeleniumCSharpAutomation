using OpenQA.Selenium; // Thư viện Selenium để điều khiển trình duyệt
using OpenQA.Selenium.Chrome; // Thư viện điều khiển trình duyệt Chrome
using NUnit.Framework; // Thư viện dùng để viết và chạy các test case
using System; // Thư viện cơ bản của hệ thống
using System.IO; // Thư viện dùng để thao tác với file và thư mục

namespace UnsplashAutomation.Tests
{
    public class BaseTest
    {
        protected IWebDriver driver; // Biến driver dùng để điều khiển trình duyệt trong suốt quá trình test

        [SetUp] // Đánh dấu hàm này sẽ chạy TRƯỚC mỗi test case
        public void Setup()
        {
            var options = new ChromeOptions(); // Tạo các cài đặt tùy chỉnh cho trình duyệt Chrome
            options.AddArgument("--start-maximized"); // Mở trình duyệt ở chế độ toàn màn hình
            options.AddArgument("--disable-blink-features=AutomationControlled"); // Vô hiệu hóa tính năng giúp web nhận diện Chrome đang bị tự động hóa
            options.AddExcludedArgument("enable-automation"); // Loại bỏ thông báo Chrome đang bị điều khiển bởi phần mềm tự động
            options.AddAdditionalChromeOption("useAutomationExtension", false); // Tắt các extension tự động của Chrome
            // Giả lập trình duyệt là người dùng thật bằng cách đặt User-Agent thực tế
            options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            options.AddArgument("--disable-infobars"); // Tắt các thanh thông báo phiền phức của Chrome
            options.AddArgument("--disable-notifications"); // Tắt các thông báo từ trang web

            // Cấu hình thư mục download và tự động tải file không cần hỏi
            if (!Directory.Exists(Utilities.TestData.DownloadDirectory))
            {
                Directory.CreateDirectory(Utilities.TestData.DownloadDirectory);
            }
            options.AddUserProfilePreference("download.default_directory", Utilities.TestData.DownloadDirectory);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            options.AddUserProfilePreference("download.directory_upgrade", true);
            options.AddUserProfilePreference("safebrowsing.enabled", true);
            options.AddUserProfilePreference("profile.default_content_settings.popups", 0);
            options.AddUserProfilePreference("profile.default_content_setting_values.automatic_downloads", 1);
            
            Console.WriteLine($"Initializing ChromeDriver. Downloads will be saved to: {Utilities.TestData.DownloadDirectory}");
            driver = new ChromeDriver(options); // Bắt đầu mở trình duyệt Chrome với các cài đặt trên
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10); // Thiết lập thời gian chờ ngầm định là 10 giây cho mọi phần tử
        }

        [TearDown] // Đánh dấu hàm này sẽ chạy SAU mỗi test case
        public void TearDown()
        {
            if (driver != null) // Nếu trình duyệt đang mở
            {
                // Nếu test case bị thất bại (Failed)
                if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
                {
                    try
                    {
                        // Chụp ảnh màn hình khi có lỗi
                        var screenshot = ((ITakesScreenshot)driver).GetScreenshot(); // Thực hiện lệnh chụp màn hình
                        string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // Lấy thời gian hiện tại để đặt tên ảnh
                        string screenshotName = $"error_{TestContext.CurrentContext.Test.Name}_{timeStamp}.png"; // Tên file ảnh lỗi
                        string screenshotPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, screenshotName); // Đường dẫn lưu file ảnh
                        screenshot.SaveAsFile(screenshotPath); // Lưu ảnh vào ổ cứng
                        Console.WriteLine($"Screenshot saved to: {screenshotPath}"); // In ra thông báo đã lưu ảnh thành công

                        // Lưu mã nguồn HTML của trang web khi có lỗi
                        string sourceName = $"source_{TestContext.CurrentContext.Test.Name}_{timeStamp}.html"; // Tên file HTML
                        string sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, sourceName); // Đường dẫn lưu file HTML
                        File.WriteAllText(sourcePath, driver.PageSource); // Lưu toàn bộ mã nguồn web vào file
                        Console.WriteLine($"Page source saved to: {sourcePath}"); // In ra thông báo đã lưu mã nguồn
                    }
                    catch (Exception ex) // Nếu có lỗi trong quá trình chụp ảnh/lưu mã nguồn
                    {
                        Console.WriteLine($"Failed to capture debug info: {ex.Message}"); // In ra lỗi
                    }
                }
                driver.Quit(); // Đóng trình duyệt hoàn toàn
                driver.Dispose(); // Giải phóng tài nguyên hệ thống
            }
        }
    }
}
