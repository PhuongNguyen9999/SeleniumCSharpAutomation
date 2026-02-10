using NUnit.Framework;
using UnsplashAutomation.Pages;
using UnsplashAutomation.Utilities;
using System;

namespace UnsplashAutomation.Tests
{
    [TestFixture]
    public class Scenario5_DownloadTests : BaseTest
    {
        private LoginPage loginPage;
        private HomePage homePage;
        private PhotoDetailsPage photoDetailsPage;

        [SetUp]
        public void LocalSetup()
        {
            loginPage = new LoginPage(driver);
            homePage = new HomePage(driver);
            photoDetailsPage = new PhotoDetailsPage(driver);
            photoDetailsPage.CleanUpDownloads();
        }

        [Test]
        [Description("Scenario 5: Download photo successfully and verify file existence")]
        public void DownloadPhotoSuccessfully()
        {
            // Bước 1: Đăng nhập
            Console.WriteLine("Step 1: Logging in...");
            driver.Navigate().GoToUrl("https://unsplash.com/login");
            loginPage.Login(TestData.UserEmail, TestData.UserPassword);
            Assert.That(loginPage.IsLoggedIn(), Is.True, "Login should be successful.");

            // Bước 2: Mở một tấm ảnh ngẫu nhiên
            Console.WriteLine("Step 2: Opening a random photo...");
            driver.Navigate().GoToUrl("https://unsplash.com");
            homePage.OpenRandomPhoto();

            // Bước 3: Thực hiện tải ảnh xuống
            Console.WriteLine("Step 3: Clicking download button...");
            string photoId = photoDetailsPage.GetPhotoId();
            photoDetailsPage.ClickDownload();

            // Bước 4: Xác nhận file đã tải về máy
            Console.WriteLine("Step 4: Verifying download on disk...");
            // Unsplash thường đặt tên file chứa ID tấm ảnh
            bool isDownloaded = photoDetailsPage.WaitForDownloadToComplete(photoId);
            
            Assert.That(isDownloaded, Is.True, $"Photo with ID {photoId} should be downloaded to {TestData.DownloadDirectory}");
            
            Console.WriteLine("Scenario 5 PASSED: Photo downloaded and verified successfully.");
        }

        [TearDown]
        public void LocalCleanup()
        {
            // Làm sạch thư mục download sau mỗi test để không ảnh hưởng lần chạy sau
            photoDetailsPage?.CleanUpDownloads();
        }
    }
}
