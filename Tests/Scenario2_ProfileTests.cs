using NUnit.Framework; 
using UnsplashAutomation.Pages; 
using OpenQA.Selenium; 
using System; 
using UnsplashAutomation.Utilities;

namespace UnsplashAutomation.Tests
{
    [TestFixture] // Đánh dấu đây là một lớp chứa các test case
    public class Scenario2_ProfileTests : BaseTest // Kế thừa từ BaseTest để dùng chung Setup/TearDown
    {
        // Khai báo các đối diện trang web sẽ sử dụng trong bộ test này
        private LoginPage loginPage;
        private ProfilePage profilePage;
        private EditProfilePage editProfilePage;

        [SetUp] // Lệnh khởi tạo các trang TRƯỚC khi bắt đầu mỗi test case
        public void TestSetup()
        {
            loginPage = new LoginPage(driver);
            profilePage = new ProfilePage(driver);
            editProfilePage = new EditProfilePage(driver);
        }

        [Test] // Đánh dấu đây là một hàm chạy Test thực sự
        [Description("Scenario 2: Update the username URL in the Profile page")]
        public void UpdateUsernameSuccessfully()
        {
            // Các biến dữ liệu đầu vào được lấy/tạo từ TestData
            string newUsername = TestData.GenerateUniqueUsername();

            // Bước 1: Đăng nhập vào hệ thống Unsplash
            Console.WriteLine("Logging in...");
            loginPage.Login(TestData.UserEmail, TestData.UserPassword);

            // Bước 2: Điều hướng đến trang Hồ sơ cá nhân (Profile)
            Console.WriteLine("Navigating to Profile page...");
            // Tìm link dẫn đến trang cá nhân từ navbar (thường chứa avatar và href bắt đầu bằng /@)
            var profileLink = driver.FindElement(By.XPath("//button[@aria-label='Your profile'] | //a[contains(@href, '/@')]"));
            profileLink.Click(); // Nhấn vào avatar/profile link

            // Bước 3: Nhấn nút "Edit profile" để mở trang chỉnh sửa thông tin
            profilePage.GoToEditProfile();

            // Bước 4: Thực hiện thay đổi Username và nhấn Cập nhật (Update)
            editProfilePage.UpdateUsername(newUsername);

            // Bước 5: Kiểm tra kết quả bằng cách truy cập trực tiếp vào URL profile mới
            // Cửa sổ trình duyệt sẽ mở địa chỉ: https://unsplash.com/@<username_mới>
            string newProfileUrl = $"https://unsplash.com/@{newUsername}";
            Console.WriteLine($"Navigating to new profile URL: {newProfileUrl}");
            driver.Navigate().GoToUrl(newProfileUrl);

            // Bước 6: Xác nhận rằng URL hiện tại đã thay đổi đúng theo username mới
            Assert.That(driver.Url.ToLower(), Does.Contain(newUsername.ToLower()), "URL should contain the new username.");
            
            // Bước 7: Xác nhận Tên đầy đủ (Full Name) vẫn hiển thị chính xác từ TestData
            string actualFullName = profilePage.GetFullName();
            Console.WriteLine($"Final Full Name displayed: {actualFullName}");
            Assert.That(actualFullName, Is.EqualTo(TestData.ExpectedFullName), "Full name should match expected.");
        }
    }
}
