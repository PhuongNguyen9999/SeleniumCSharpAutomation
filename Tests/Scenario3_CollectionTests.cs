using NUnit.Framework;
using UnsplashAutomation.Pages;
using UnsplashAutomation.Services;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;
using SeleniumExtras.WaitHelpers;
using OpenQA.Selenium.Support.UI;
using System.Linq;
using UnsplashAutomation.Utilities;

namespace UnsplashAutomation.Tests
{
    [TestFixture]
    public class Scenario3_CollectionTests : BaseTest
    {
        private LoginPage loginPage;
        private HomePage homePage;
        private CollectionPage collectionPage;
        private ApiService apiService;
        private WebDriverWait wait;
        private string? createdCollectionId;

        [SetUp]
        public void TestSetup()
        {
            loginPage = new LoginPage(driver);
            homePage = new HomePage(driver);
            collectionPage = new CollectionPage(driver);
            apiService = new ApiService();
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
        }

        [Test]
        [Description("Scenario 3: Create a private collection and verify photo counts")]
        public void CreateCollectionAndVerifyPhotos()
        {
            // Dữ liệu mẫu lấy từ TestData
            string collectionName = TestData.GenerateCollectionName();

            // Bước 1: Đăng nhập
            Console.WriteLine("Step 1: Logging in...");
            loginPage.Login(TestData.UserEmail, TestData.UserPassword);

            // Bước 2: Tạo Collection mới và thêm ảnh đầu tiên
            Console.WriteLine("Step 2: Navigating to Home and verifying session...");
            homePage.BrowsePhotos();
            
            // Kiểm tra xem có thực sự đang đăng nhập không (phòng trường hợp mất session)
            bool isActuallyLoggedIn = driver.FindElements(By.XPath("//button[@aria-label='Your profile'] | //button[@aria-label='Your Profile'] | //img[contains(@alt, 'profile') or contains(@alt, 'Avatar')]")).Count > 0;
            if (!isActuallyLoggedIn)
            {
                Console.WriteLine("Warning: Session lost after navigating to Home. Re-attempting login...");
                loginPage.Login(TestData.UserEmail, TestData.UserPassword);
                homePage.BrowsePhotos();
            }
            else
            {
                Console.WriteLine("Session verified. Proceeding with collection creation.");
            }

            homePage.AddFirstPhotoToNewCollection(collectionName, isPrivate: true);

            // Bước 3: Thêm ảnh ngẫu nhiên khác vào collection dựa trên số lượng cấu hình
            Console.WriteLine($"Step 3: Adding {TestData.AdditionalPhotosToAdd} more photos...");
            homePage.AddRandomPhotosToExistingCollection(TestData.AdditionalPhotosToAdd, collectionName);

            // Bước 4: Truy cập trang quản lý Collection để kiểm tra
            // Unsplash thường dẫn đến URL /collections/id/name
            // Ở đây ta sẽ đi tìm link đến collection trong Profile hoặc Click vào thông báo
            // Để đơn giản, ta tìm ID từ modal hoặc URL nếu đã navigate. 
            Console.WriteLine("Step 4: Navigating to Collection page...");
            
            // Thay vì hardcode URL hoặc click menu phức tạp, ta dùng /me redirect
            driver.Navigate().GoToUrl("https://unsplash.com/me");
            
            // Nhấn tab 'Collections'
            var collectionsTab = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//a[contains(@href, '/collections')]")));
            collectionsTab.Click();
            
            var collectionLink = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath($"//a[contains(., '{collectionName}')]")));
            collectionLink.Click();

            // Lấy ID để xóa sau này
            createdCollectionId = collectionPage.GetCollectionIdFromUrl();
            Console.WriteLine($"Captured Collection ID: {createdCollectionId}");

            // Bước 5: Kiểm tra số lượng ảnh
            Console.WriteLine("Step 5: Verifying photo counts...");
            int labelCount = collectionPage.GetPhotoCountFromLabel();
            int actualCount = collectionPage.GetActualPhotoCountInGrid();

            Console.WriteLine($"Label says: {labelCount}, Grid shows: {actualCount}");

            Assert.That(labelCount, Is.EqualTo(TestData.TotalExpectedPhotos), $"The collection label should show {TestData.TotalExpectedPhotos} photos.");
            Assert.That(actualCount, Is.EqualTo(TestData.TotalExpectedPhotos), $"There should be {TestData.TotalExpectedPhotos} photos displayed in the grid.");
        }

        [TearDown]
        public async Task CleanUp()
        {
            // Xóa Collection qua API sau khi test xong (dù Pass hay Fail)
            if (!string.IsNullOrEmpty(createdCollectionId))
            {
                bool deleted = await apiService.DeleteCollection(createdCollectionId);
                if (deleted) Console.WriteLine("Cleanup: Collection deleted via API.");
                else Console.WriteLine("Cleanup Warning: Collection could not be deleted via API.");
            }
        }
    }
}
