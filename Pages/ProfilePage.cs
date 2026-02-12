using OpenQA.Selenium;
using SeleniumExtras.WaitHelpers;
using OpenQA.Selenium.Support.UI;
using System;

namespace UnsplashAutomation.Pages
{
    public class ProfilePage // Trang hồ sơ người dùng
    {
        private readonly IWebDriver driver; // Đối tượng điều khiển trình duyệt
        private readonly WebDriverWait wait; // Đối tượng dùng để chờ các phần tử hiển thị

        public ProfilePage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20)); // Thiết lập thời gian chờ tối đa 20 giây
        }

        // Locator cho nút "Edit profile" (Chỉnh sửa hồ sơ)
        // Tìm thẻ <a> có chứa link đến '/account' và có chứa chữ 'Edit profile'
        private By EditProfileButton => By.XPath("//a[contains(@href, '/account') and contains(., 'Edit profile')]");

        // Locator cho tên hiển thị của người dùng (Full Name)
        // Tìm div nằm trong vùng chứa tên 'nameAndControls'
        public By FullNameDisplay => By.XPath("//div[contains(@class, 'nameAndControls')]//div[contains(@class, 'name')]");

        public void GoToEditProfile() // Phương thức nhấn nút chỉnh sửa hồ sơ
        {
            Console.WriteLine("Navigating to Edit Profile..."); // Thông báo đang chuyển đến trang chỉnh sửa
            var btn = wait.Until(ExpectedConditions.ElementToBeClickable(EditProfileButton)); // Đợi nút có thể nhấn được
            btn.Click(); // Thực hiện nhấn nút
        }

        public string GetFullName() // Phương thức lấy tên đầy đủ hiện đang hiển thị trên trang
        {
            var element = wait.Until(ExpectedConditions.ElementIsVisible(FullNameDisplay)); // Đợi tên hiển thị
            return element.Text; // Lấy nội dung văn bản bên trong
        }
    }
}
