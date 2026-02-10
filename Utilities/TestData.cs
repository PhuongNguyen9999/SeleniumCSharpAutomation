using System;
using System.IO;

namespace UnsplashAutomation.Utilities
{
    // Class chứa dữ liệu dùng chung cho toàn bộ dự án test
    // Giúp quản lý thông tin đăng nhập, API và các tham số tập trung tại một nơi
    public static class TestData
    {
        // ---------------------------------------------------------
        // 1. Thông tin đăng nhập (Credentials)
        // ---------------------------------------------------------
        public const string UserEmail = "osinsenior@gmail.com";
        public const string UserPassword = "PhuongNguyen@12345";
        
        // ---------------------------------------------------------
        // 2. Thông tin hồ sơ (Profile Data)
        // ---------------------------------------------------------
        public const string BaseUsername = "phuongnguyenquang_";
        public const string ExpectedFullName = "Phuong Nguyen Quang";
        
        // ---------------------------------------------------------
        // 3. Cấu hình API (API Configuration)
        // ---------------------------------------------------------
        public const string ApiBaseUrl = "https://api.unsplash.com";
        public const string ApiAccessKey = "F0-KSlS3_BeI8A1oBKXiklG7afK6OE2ETIrU9-Dl3eU";
        // OAuth Bearer Token (Lấy từ Postman) để thực hiện các thao tác Xóa/Sửa
        public const string ApiBearerToken = "9lvxJOQlhkaI1ILlf_dwATYg3KLb5jAK5IuliMowrmk"; 
        
        // ---------------------------------------------------------
        // 4. Tham số Scenario 3 (Collection Parameters)
        // ---------------------------------------------------------
        public const int AdditionalPhotosToAdd = 2; // Số lượng ảnh thêm vào sau khi tạo collection
        public const int TotalExpectedPhotos = 3;   // Tổng số ảnh mong đợi trong collection (1 + 2)

        // ---------------------------------------------------------
        // 5. Tham số Scenario 5 (Download Parameters)
        // ---------------------------------------------------------
        // Thư mục lưu trữ ảnh tải về, nằm trong thư mục chạy của project
        public static readonly string DownloadDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads");

        // ---------------------------------------------------------
        // 6. Các hàm hỗ trợ tạo dữ liệu động (Helpers)
        // ---------------------------------------------------------
        
        // Tạo username duy nhất kèm timestamp
        public static string GenerateUniqueUsername() => $"{BaseUsername}{DateTime.Now:MMddHHmm}";
        
        // Tạo tên collection duy nhất kèm timestamp
        public static string GenerateCollectionName() => $"MyPrivateTest_{DateTime.Now:MMddHHmm}";
    }
}
