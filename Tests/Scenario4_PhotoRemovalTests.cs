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
    public class Scenario4_PhotoRemovalTests : BaseTest
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
        [Description("Scenario 4: Remove photos from the collection successfully")]
        public void RemovePhotoFromCollectionSuccessfully()
        {
            // Dữ liệu mẫu lấy từ TestData
            string collectionName = $"RemoveTest_{DateTime.Now:MMddHHmm}";

            // Bước 1: Đăng nhập
            Console.WriteLine("Step 1: Logging in...");
            loginPage.Login(TestData.UserEmail, TestData.UserPassword);

            // Bước 2: Tạo Collection mới và thêm ảnh đầu tiên
            Console.WriteLine("Step 2: Creating a private collection and adding first photo...");
            homePage.BrowsePhotos();
            homePage.AddFirstPhotoToNewCollection(collectionName, isPrivate: true);

            // Bước 3: Thêm 1 ảnh ngẫu nhiên khác (Tổng cộng 2 ảnh)
            Console.WriteLine("Step 3: Adding 1 more random photo...");
            homePage.AddRandomPhotosToExistingCollection(1, collectionName);

            // Bước 4: Điều hướng đến trang Collection
            Console.WriteLine("Step 4: Navigating to Collection page...");
            driver.Navigate().GoToUrl("https://unsplash.com/me");
            
            var collectionsTab = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//a[contains(@href, '/collections')]")));
            collectionsTab.Click();
            
            var collectionLink = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath($"//a[contains(., '{collectionName}')]")));
            collectionLink.Click();

            // Lấy ID để xóa sau này
            createdCollectionId = collectionPage.GetCollectionIdFromUrl();
            Console.WriteLine($"Captured Collection ID: {createdCollectionId}");

            // Kiểm tra số lượng ban đầu (phải là 2)
            int initialCount = collectionPage.GetActualPhotoCountInGrid();
            Assert.That(initialCount, Is.EqualTo(2), "Collection should initially have 2 photos.");

            // Bước 5: Xóa 1 tấm ảnh khỏi bộ sưu tập
            Console.WriteLine("Step 5: Removing 1 photo from the collection...");
            collectionPage.RemoveFirstPhoto();

            // Bước 6: Xác nhận ảnh đã bị xóa và còn lại 1 ảnh
            Console.WriteLine("Step 6: Verifying remaining photo count...");
            int remainingCount = collectionPage.GetActualPhotoCountInGrid();
            Console.WriteLine($"Remaining photos in grid: {remainingCount}");

            Assert.That(remainingCount, Is.EqualTo(1), "The collection should have only 1 photo remaining.");
        }

        [TearDown]
        public async Task CleanUp()
        {
            // Xóa Collection qua API sau khi test xong
            if (!string.IsNullOrEmpty(createdCollectionId))
            {
                bool deleted = await apiService.DeleteCollection(createdCollectionId);
                if (deleted) Console.WriteLine("Cleanup: Collection deleted via API.");
                else Console.WriteLine("Cleanup Warning: Collection could not be deleted via API.");
            }
        }
    }
}
