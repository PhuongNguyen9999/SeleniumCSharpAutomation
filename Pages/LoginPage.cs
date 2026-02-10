using OpenQA.Selenium; // Thư viện cơ bản của Selenium
using SeleniumExtras.WaitHelpers; // Thư viện hỗ trợ các điều kiện chờ của Selenium
using OpenQA.Selenium.Support.UI; // Thư viện hỗ trợ các tính năng như WebDriverWait
using System; // Thư viện hệ thống cơ bản

namespace UnsplashAutomation.Pages
{
    public class LoginPage
    {
        private readonly IWebDriver driver; // Biến lưu trữ trình duyệt đang được điều khiển
        private readonly WebDriverWait wait; // Biến dùng để thiết lập thời gian chờ cho các phần tử

        // Hàm khởi tạo (Constructor) của class LoginPage
        public LoginPage(IWebDriver driver)
        {
            this.driver = driver; // Gán trình duyệt được truyền vào cho biến driver của class
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10)); // Thiết lập thời gian chờ tối đa 10 giây
        }

        // Khai báo các "Locator" (cách tìm) các phần tử trên trang web
        private By EmailInput => By.Name("email"); // Tìm ô nhập email bằng thuộc tính name="email"
        private By PasswordInput => By.Name("password"); // Tìm ô nhập mật khẩu bằng thuộc tính name="password"
        // Tìm nút Login bằng nhiều cách XPath khác nhau để tăng độ tin cậy
        private By LoginButton => By.XPath("//button[@type='submit' and contains(@class, 'loginButton')] | //form[@action='/login']//button[@type='submit']");

        // Hàm thực hiện hành động đăng nhập
        public void Login(string email, string password)
        {
            Console.WriteLine("Navigating to Unsplash Home..."); // Thông báo đang truy cập trang chủ
            driver.Navigate().GoToUrl("https://unsplash.com/"); // Mở trang web Unsplash
            
            try 
            {
                // Thử tìm nút 'Log in' bằng nhiều chiến thuật khác nhau đề phòng trang web thay đổi
                var loginButtonLocators = new[] {
                    By.XPath("//a[contains(@href, '/login') and not(contains(@class, 'sidebarOnly'))]"),
                    By.XPath("//a[text()='Log in']"),
                    By.LinkText("Log in"),
                    By.CssSelector("a[href*='/login']")
                };

                IWebElement? loginLink = null;
                foreach (var loc in loginButtonLocators)
                {
                    try {
                        // Chờ cho đến khi nút 'Log in' có thể click được
                        loginLink = wait.Until(ExpectedConditions.ElementToBeClickable(loc));
                        if (loginLink != null && loginLink.Displayed) break; // Nếu tìm thấy và đang hiển thị thì dừng tìm kiếm
                    } catch { continue; } // Nếu lỗi thì thử cách tìm tiếp theo
                }

                if (loginLink != null) {
                    Console.WriteLine("Clicking 'Log in' link..."); // Thông báo đang click vào link Log in
                    loginLink.Click(); // Thực hiện click
                } else {
                    Console.WriteLine("'Log in' link not found, navigating directly to /login..."); // Thông báo không thấy link
                    driver.Navigate().GoToUrl("https://unsplash.com/login"); // Chuyển hướng trực tiếp đến trang đăng nhập
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during 'Log in' link search: {ex.Message}. Navigating directly...");
                driver.Navigate().GoToUrl("https://unsplash.com/login");
            }
            
            // Chờ cho đến khi URL hiện tại chứa chữ "/login" để chắc chắn đã vào trang đăng nhập
            try {
                wait.Until(d => d.Url.Contains("/login"));
                Console.WriteLine("Successfully reached the login page.");
            } catch {
                Console.WriteLine("Navigation to /login might have been redirected. Re-attempting direct navigation...");
                driver.Navigate().GoToUrl("https://unsplash.com/login");
            }

            // Thử tắt thông báo chấp nhận Cookies nếu nó xuất hiện
            try 
            {
                var acceptCookies = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[contains(text(),'Accept all cookies')]")));
                acceptCookies.Click();
            }
            catch { /* Bỏ qua nếu không thấy thông báo cookies */ }

            Console.WriteLine("Entering credentials..."); // Thông báo đang nhập thông tin
            var emailElement = wait.Until(ExpectedConditions.ElementIsVisible(EmailInput)); // Đợi ô email xuất hiện
            emailElement.Clear(); // Xóa trắng ô email trước khi nhập
            emailElement.SendKeys(email); // Nhập email của người dùng

            var passwordElement = driver.FindElement(PasswordInput); // Tìm ô mật khẩu
            passwordElement.Clear(); // Xóa trắng ô mật khẩu
            passwordElement.SendKeys(password); // Nhập mật khẩu

            Console.WriteLine("Clicking the login button..."); // Thông báo đang click nút đăng nhập
            driver.FindElement(LoginButton).Click(); // Thực hiện click nút đăng nhập

            // Bước kiểm tra: Đợi cho đến khi thấy nút 'Your profile' hoặc Avatar xuất hiện
            try {
                wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//button[@aria-label='Your profile'] | //button[@aria-label='Your Profile'] | //img[contains(@alt, 'profile') or contains(@alt, 'Avatar')]")));
                Console.WriteLine("Login successful, user profile/avatar detected.");
                
                // Kiểm tra xem có còn ở trang login không
                if (driver.Url.Contains("/login")) {
                    Console.WriteLine("Still on login page despite profile detection. Forcing homepage...");
                    driver.Navigate().GoToUrl("https://unsplash.com/");
                }
            } catch (Exception) {
                Console.WriteLine("Profile element not detected within timeout. Checking URL...");
                if (!driver.Url.Contains("/login")) {
                    Console.WriteLine("URL changed, assuming login success.");
                } else {
                    throw new Exception("Login failed: Still on login page after clicking submit.");
                }
            }
        }

        public bool IsLoggedIn() // Kiểm tra xem người dùng đã đăng nhập thành công chưa
        {
            try
            {
                // Kiểm tra sự xuất hiện của nút profile hoặc avatar
                var profileElement = wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//button[@aria-label='Your profile' or @aria-label='Your Profile'] | //img[contains(@alt, 'profile') or contains(@alt, 'Avatar')]")));
                return profileElement != null && profileElement.Displayed;
            }
            catch
            {
                // Nếu URL không còn là login page thì coi như đã đăng nhập (trường hợp redirect chậm)
                return !driver.Url.Contains("/login");
            }
        }
    }
}
