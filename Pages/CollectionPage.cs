using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Text.RegularExpressions;
using OpenQA.Selenium.Interactions;
using System.Linq;

namespace UnsplashAutomation.Pages
{
    public class CollectionPage // Trang hiển thị bộ sưu tập (Collection)
    {
        private readonly IWebDriver driver;
        private readonly WebDriverWait wait;

        public CollectionPage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
        }

        // Locator cho các tấm ảnh trong bộ sưu tập (có thể là photo-grid hoặc asset-grid)
        private By PhotoGridItem => By.XPath("//figure[@data-testid='photo-grid-masonry-figure' or @data-testid='asset-grid-masonry-figure']");
        
        // Locator cho nút "Remove from collection" hiển thị khi hover
        private By RemoveBtn => By.XPath("//button[contains(@title, 'Remove') or contains(@aria-label, 'Remove')]");

        // Locator cho số lượng ảnh hiển thị trong bộ sưu tập
        // Thường là text có dạng "3 photos"
        private By PhotoCountLabel => By.XPath("//div[contains(@class, 'Photos')]//span | //h1/following-sibling::div[contains(., 'photos')]");

        // Locator cho danh sách các tấm ảnh trong bộ sưu tập
        private By CollectionPhotos => By.CssSelector("[data-testid='asset-grid-masonry-figure']");

        public int GetPhotoCountFromLabel() // Lấy số lượng ảnh từ nhãn văn bản (Label)
        {
            try {
                // Thử tìm label có đuôi 'photos'
                var element = wait.Until(d => {
                    var els = d.FindElements(By.XPath("//div[contains(text(), 'photos')] | //span[contains(text(), 'photos')]"));
                    return els.FirstOrDefault(e => e.Displayed && Regex.IsMatch(e.Text, @"\d+ photos"));
                });
                
                if (element != null) {
                    string text = element.Text;
                    Console.WriteLine($"Collection label text detected: {text}");
                    var match = Regex.Match(text, @"\d+");
                    if (match.Success) return int.Parse(match.Value);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Warning: Could not get count from label: {ex.Message}. Falling back to grid count.");
            }
            
            return GetActualPhotoCountInGrid();
        }

        public int GetActualPhotoCountInGrid() // Đếm số lượng ảnh thực tế hiển thị trên lưới
        {
            System.Threading.Thread.Sleep(3000); // Đợi lưới ảnh load xong hoàn toàn
            var photos = driver.FindElements(PhotoGridItem);
            
            // Fallback nếu không tìm thấy figure (đôi khi chỉ có img)
            if (photos.Count == 0) {
                photos = driver.FindElements(By.XPath("//img[@alt and (contains(@src, 'photo') or contains(@src, 'images.unsplash.com'))]"));
            }
            
            Console.WriteLine($"Actual photos in grid: {photos.Count}");
            return photos.Count;
        }

        public string GetCollectionIdFromUrl() // Lấy ID của bộ sưu tập từ URL trang hiện tại
        {
            // URL dạng: https://unsplash.com/collections/12345/collection-name
            string url = driver.Url;
            var match = Regex.Match(url, @"collections/([^/]+)");
            return match.Success ? match.Groups[1].Value : "";
        }

        public void RemoveFirstPhoto() // Xóa tấm ảnh đầu tiên khỏi bộ sưu tập
        {
            Console.WriteLine("Attempting to remove the first photo from collection...");
            
            // Tìm tất cả các tấm ảnh trong lưới
            var photos = wait.Until(d => d.FindElements(PhotoGridItem));
            if (photos.Count == 0) throw new Exception("No photos found in collection to remove.");

            var firstPhoto = photos[0];
            
            // Di chuột vào ảnh để kích hoạt các nút chức năng
            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", firstPhoto);
            System.Threading.Thread.Sleep(500);
            new Actions(driver).MoveToElement(firstPhoto).Perform();
            System.Threading.Thread.Sleep(1000);

            // CHIẾN LƯỢC 1: Tìm nút Remove trực tiếp (Title, Label, hoặc Text)
            var removeBtn = driver.FindElements(By.TagName("button"))
                .FirstOrDefault(b => (b.GetAttribute("title") ?? "").Contains("Remove") 
                                  || (b.GetAttribute("aria-label") ?? "").Contains("Remove")
                                  || (b.Text ?? "").Contains("Remove"));

            if (removeBtn != null && removeBtn.Displayed)
            {
                Console.WriteLine("Found direct Remove button. Clicking...");
                try { removeBtn.Click(); } catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", removeBtn); }
            }
            else
            {
                // CHIẾN LƯỢC 2: Sử dụng modal "Add to Collection" (Uncheck) làm fallback
                Console.WriteLine("Direct Remove button not found. Using 'Add to Collection' modal to toggle...");
                var addBtn = firstPhoto.FindElements(By.XPath(".//button[@aria-label='Add to Collection']")).FirstOrDefault();
                
                if (addBtn != null)
                {
                    try { addBtn.Click(); } catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", addBtn); }
                    System.Threading.Thread.Sleep(1500);
                    
                    // Tìm collection hiện tại (đã được chọn) trong danh sách
                    var selectedItems = driver.FindElements(By.XPath("//div[@data-selected='true' or @aria-selected='true']//h4 | //div[contains(@class, 'item')]//svg[contains(@class, 'removeAction')]"));
                    
                    if (selectedItems.Count > 0)
                    {
                        Console.WriteLine("Unselecting collection in modal...");
                        try { selectedItems[0].Click(); } catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", selectedItems[0]); }
                    }
                    else
                    {
                        // Fallback: Click option đầu tiên nếu không xác định được trạng thái selected
                        var firstOption = driver.FindElements(By.XPath("//div[@role='option']")).FirstOrDefault();
                        if (firstOption != null) firstOption.Click();
                    }
                    
                    // Đóng modal bằng phím ESC
                    System.Threading.Thread.Sleep(1000);
                    new Actions(driver).SendKeys(Keys.Escape).Perform();
                }
                else
                {
                    throw new Exception("Could not find any removal mechanism (direct button or '+' button).");
                }
            }
            
            Console.WriteLine("Removal action performed. Refreshing page for update...");
            System.Threading.Thread.Sleep(2000);
            driver.Navigate().Refresh();
            System.Threading.Thread.Sleep(3000);
        }
    }
}
