using NUnit.Framework; 
using UnsplashAutomation.Pages; 
using OpenQA.Selenium; 
using System; 
using UnsplashAutomation.Utilities;

namespace UnsplashAutomation.Tests
{
    [TestFixture] // Đánh dấu đây là một lớp chứa các test case
    public class Scenario1_BookmarkTests : BaseTest // Kế thừa từ BaseTest để dùng chung Setup/TearDown
    {
        // Khai báo các đối diện trang web sẽ sử dụng trong bộ test này
        private LoginPage loginPage;
        private HomePage homePage;
        private PhotoDetailsPage photoDetailsPage;

        [SetUp] // Lệnh khởi tạo các trang TRƯỚC khi bắt đầu mỗi test case
        public void TestSetup()
        {
            loginPage = new LoginPage(driver);
            homePage = new HomePage(driver);
            photoDetailsPage = new PhotoDetailsPage(driver);
        }

        [Test] // Đánh dấu đây là một hàm chạy Test thực sự
        [Description("Scenario 1: Bookmark the first available photo successfully")]
        public void BookmarkFirstPhotoSuccessfully()
        {
            // Bước 1: Thực hiện đăng nhập với tài khoản và mật khẩu từ TestData
            loginPage.Login(TestData.UserEmail, TestData.UserPassword);
 
            // Bước 2: Truy cập vào trang chủ để xem danh sách ảnh
            homePage.BrowsePhotos();
 
            // Bước 3: Tìm và Bookmark tấm ảnh đầu tiên chưa được lưu trên lưới (Grid)
            // Hàm này cũng sẽ tự động Click vào ảnh để mở trang chi tiết sau khi lưu thành công
            homePage.BookmarkFirstAvailablePhoto();
            
            // Bước 4: Kiểm tra trạng thái nút bookmark trên trang chi tiết (đã được mở)
            // Lệnh Assert.That dùng để khẳng định kết quả: "Phải Đang Được Bookmark"
            Assert.That(photoDetailsPage.IsBookmarked(), Is.True, "The photo should be bookmarked.");
 
            // Bước 5: Lấy ID của tấm ảnh vừa lưu từ thanh địa chỉ URL
            string photoId = photoDetailsPage.GetPhotoIdFromUrl();
            Console.WriteLine($"Bookmarked photo ID: {photoId}"); // In ID ra màn hình kết quả test
            
            // Khẳng định rằng ID lấy được không được để trống
            Assert.That(string.IsNullOrEmpty(photoId), Is.False, "Photo ID should be captured from the URL.");
        }
    }
}
