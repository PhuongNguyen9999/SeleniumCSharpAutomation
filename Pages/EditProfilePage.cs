using OpenQA.Selenium;
using SeleniumExtras.WaitHelpers;
using OpenQA.Selenium.Support.UI;
using System;

namespace UnsplashAutomation.Pages
{
    public class EditProfilePage // Trang chỉnh sửa tài khoản
    {
        private readonly IWebDriver driver; // Đối tượng điều khiển trình duyệt
        private readonly WebDriverWait wait; // Đối tượng dùng để chờ các phần tử hiển thị

        public EditProfilePage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10)); // Thiết lập thời gian chờ tối đa 10 giây
        }

        // Các Locator cho trang cài đặt tài khoản
        private By UsernameInput => By.Id("user_username"); // Ô nhập tên người dùng (Username)
        private By UpdateAccountButton => By.XPath("//input[@type='submit' and @value='Update account'] | //button[@type='submit' and contains(., 'Update')]"); // Nút cập nhật

        public void UpdateUsername(string newUsername) // Phương thức thay đổi username
        {
            Console.WriteLine($"Updating username to: {newUsername}"); 
            var input = wait.Until(ExpectedConditions.ElementIsVisible(UsernameInput));
            input.Clear();
            input.SendKeys(newUsername);

            var submit = wait.Until(ExpectedConditions.ElementToBeClickable(UpdateAccountButton));
            submit.Click();
            
            // Đợi một lát để server cập nhật và tránh 404 khi navigate ngay lập tức
            System.Threading.Thread.Sleep(3000);
            
            // Có thể kiểm tra Message Success nếu có
            try {
                var flash = driver.FindElement(By.XPath("//div[contains(@class, 'flash')] | //div[contains(., 'account was updated')]"));
                Console.WriteLine($"Update Status: {flash.Text}");
            } catch {
                Console.WriteLine("No flash message detected, assuming success.");
            }
        }

        // Truy cập trực tiếp vào trang chỉnh sửa hồ sơ
        public void GoTo()
        {
            driver.Navigate().GoToUrl("https://unsplash.com/account");
            System.Threading.Thread.Sleep(1000);
        }

        // Lấy tên đầy đủ (Full name) từ form chỉnh sửa nếu có
        public string GetFullName()
        {
            try {
                var possible = new By[] {
                    By.Id("user_name"),
                    By.Id("user_full_name"),
                    By.Name("name"),
                    By.Name("full_name"),
                    By.CssSelector("input[placeholder='Full name']")
                };

                foreach (var sel in possible)
                {
                    try {
                        var el = wait.Until(ExpectedConditions.ElementIsVisible(sel));
                        if (el != null) return el.GetAttribute("value") ?? el.Text;
                    } catch { }
                }
            } catch { }
            return string.Empty;
        }
    }
}
