using OpenQA.Selenium; // Thư viện cơ bản của Selenium
using SeleniumExtras.WaitHelpers; // Thư viện hỗ trợ các điều kiện chờ
using OpenQA.Selenium.Support.UI; // Thư viện hỗ trợ WebDriverWait
using System; // Thư viện hệ thống cơ bản
using System.Collections.Generic; // Thư viện dùng cho danh sách (List)
using System.Linq; // Thư viện dùng cho các phép truy vấn dữ liệu

namespace UnsplashAutomation.Pages
{
    public class HomePage
    {
        private readonly IWebDriver driver; // Trình duyệt điều khiển
        private readonly WebDriverWait wait; // Biến thiết lập thời gian chờ

        // Hàm khởi tạo class HomePage
        public HomePage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        // Định nghĩa Locator để tìm các tấm ảnh trên trang chủ
        private By PhotoCards => By.CssSelector("[data-testid='asset-grid-masonry-figure']");
        
        // Định nghĩa Locator để tìm nút Bookmark (Lưu ảnh)
        private By BookmarkButton => By.XPath("//button[@aria-label='Bookmark']");

        // Hàm truy cập trang chủ và kiểm tra ảnh có hiển thị không
        public void BrowsePhotos()
        {
            driver.Navigate().GoToUrl("https://unsplash.com/"); // Mở trang chủ Unsplash
            wait.Until(ExpectedConditions.ElementIsVisible(PhotoCards)); // Đợi cho ảnh đầu tiên xuất hiện
        }

        // Hàm chính: Tìm và bookmark tấm ảnh đầu tiên chưa được lưu
        public string BookmarkFirstAvailablePhoto()
        {
            Console.WriteLine("Scanning photo grid for an unbookmarked image..."); // Thông báo bắt đầu quét ảnh
            var photos = wait.Until(d => d.FindElements(PhotoCards)); // Lấy danh sách tất cả các ảnh đang hiện trên màn hình
            
            // Chỉ kiểm tra 5 tấm ảnh đầu tiên để đảm bảo tốc độ
            int scanLimit = Math.Min(5, photos.Count);
            for (int i = 0; i < scanLimit; i++)
            {
                var photo = photos[i]; // Lấy tấm ảnh thứ i trong danh sách
                try {
                    // Di chuột (Hover) vào tấm ảnh để các nút chức năng (như Bookmark) hiện lên
                    new OpenQA.Selenium.Interactions.Actions(driver).MoveToElement(photo).Perform();
                    System.Threading.Thread.Sleep(500); // Chờ 0.5 giây để hiệu ứng di chuột hoàn tất
                    
                    // Tìm nút bookmark bên trong tấm ảnh đang được di chuột vào
                    var bookmarkBtn = photo.FindElement(By.CssSelector("button[aria-label*='bookmark' i]"));
                    
                    // Nếu ảnh này ĐÃ được bookmark rồi (nút hiện chữ 'Remove') thì bỏ qua
                    if (IsAlreadyBookmarked(bookmarkBtn))
                    {
                        Console.WriteLine($"Photo {i + 1} is already bookmarked. Skipping...");
                        continue;
                    }

                    // Nếu ảnh CHƯA được bookmark, tiến hành click lưu ảnh
                    Console.WriteLine($"Photo {i + 1} is available. Bookmarking...");
                    bookmarkBtn.Click(); // Thực hiện click nút bookmark
                    
                    // Click vào tấm ảnh để mở trang chi tiết (Details Page) để kiểm tra thêm
                    photo.Click();
                    return "Success: Found and bookmarked an available photo."; // Trả về kết quả thành công
                }
                catch (Exception ex) {
                    // Nếu gặp lỗi với tấm ảnh này (ví dụ: bị quảng cáo che), in ra cảnh báo và thử ảnh tiếp theo
                    Console.WriteLine($"Warning: Skipping photo {i + 1} due to error: {ex.Message}");
                }
            }
            // Nếu đã quét hết 5 ảnh mà không tìm được tấm nào chưa lưu thì báo lỗi
            throw new Exception("Reached scan limit without finding any unbookmarked photos.");
        }

        // Hàm phụ: Kiểm tra xem nút bookmark đang ở trạng thái "Đã lưu" hay chưa
        private bool IsAlreadyBookmarked(IWebElement button)
        {
            // Lấy nội dung của thuộc tính 'aria-label' và 'title' để kiểm tra chữ "Remove"
            string label = button.GetAttribute("aria-label") ?? "";
            string title = button.GetAttribute("title") ?? "";
            // Nếu chứa chữ "Remove" thì nghĩa là ảnh đã được lưu (Người dùng click vào để 'Xóa' khỏi danh sách lưu)
            return label.Contains("Remove", StringComparison.OrdinalIgnoreCase) || 
                   title.Contains("Remove", StringComparison.OrdinalIgnoreCase);
        }

        public void BookmarkPhotoOnDetailsPage()
        {
             // Hàm này dự phòng để bookmark ở trang chi tiết nếu cần
             var collectBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//button[contains(.,'Collect')]")));
             collectBtn.Click();
        }

        // --- Các hàm phục vụ Scenario 3: Quản lý Collection ---

        public void AddFirstPhotoToNewCollection(string collectionName, bool isPrivate)
        {
            Console.WriteLine($"Creating new collection: {collectionName}");
            var photos = wait.Until(d => d.FindElements(PhotoCards));
            
            IWebElement? targetPhoto = null;
            int attempt = 0;
            foreach (var photo in photos)
            {
                attempt++;
                // 1. Kiểm tra text (Sponsored, Plus, Premium...)
                string photoText = photo.Text.ToLower();
                if (photoText.Contains("sponsored") || photoText.Contains("plus") || photoText.Contains("premium")) continue;
                
                // 2. Kiểm tra link ảnh (Nếu link dẫn tới plus.unsplash.com thì bỏ qua)
                try {
                    var link = photo.FindElement(By.TagName("a")).GetAttribute("href") ?? "";
                    if (link.Contains("plus.unsplash.com")) continue;
                } catch { /* Bỏ qua nếu ko tìm thấy link */ }

                // 3. Kiểm tra các class hoặc element đặc thù của Unsplash+ (thường có badge hoặc icon riêng)
                if (photo.FindElements(By.XPath(".//span[contains(text(), 'Plus')] | .//*[local-name()='svg' and contains(@class, 'plus')]")).Count > 0) continue;

                targetPhoto = photo;
                
                // Di chuột vào ảnh để hiện nút "+"
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", targetPhoto);
                System.Threading.Thread.Sleep(500);
                new OpenQA.Selenium.Interactions.Actions(driver).MoveToElement(targetPhoto).Perform();
                System.Threading.Thread.Sleep(800);

                // Thử tìm nút "+" lại sau khi hover
                var addBtns = photo.FindElements(By.XPath(".//button[@aria-label='Add to Collection']"));
                if (addBtns.Count == 0) continue; 
                
                var addBtn = addBtns[0];
                try {
                    addBtn.Click();
                } catch {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", addBtn);
                }

                // KIỂM TRA: Nếu vẫn hiện modal "Join Unsplash" thì bỏ qua ảnh này
                System.Threading.Thread.Sleep(1200);
                if (driver.FindElements(By.XPath("//h1[contains(., 'Join Unsplash') or contains(., 'Unsplash+')]")).Count > 0)
                {
                    Console.WriteLine($"Photo {attempt}: Triggered Join modal. Skipping...");
                    try {
                        var dismissBtn = driver.FindElement(By.XPath("//button[contains(@class, 'dismiss') or @aria-label='Close']"));
                        dismissBtn.Click();
                    } catch { 
                        driver.Navigate().Refresh(); 
                        photos = wait.Until(d => d.FindElements(PhotoCards)); // Lấy lại danh sách ảnh sau khi refresh
                    }
                    targetPhoto = null;
                    if (attempt > 15) break; 
                    continue;
                }
                
                break; // Tìm thấy ảnh hợp lệ và đã click thành công
            }

            if (targetPhoto == null) throw new Exception("Could not find a valid photo to add.");

            // 3. Nhấn "Create a new collection" nếu form chưa hiện
            IWebElement? nameInput = null;
            try {
                var nameInputs = driver.FindElements(By.Name("title"));
                if (nameInputs.Count > 0 && nameInputs[0].Displayed) {
                    nameInput = nameInputs[0];
                    Console.WriteLine("Creation form is already open and visible.");
                }
            } catch { }

            if (nameInput == null)
            {
                Console.WriteLine("Directly clicking 'Create a new collection' at modal footer...");
                // Sử dụng XPath tuyệt đối hơn cho nút ở footer modal
                var createNewBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//div[contains(@class, 'createCollectionButtonWrapper')]//button")));
                try {
                    createNewBtn.Click();
                } catch {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", createNewBtn);
                }
                
                // Đợi cho đến khi input 'title' xuất hiện và hiển thị
                nameInput = wait.Until(d => {
                    var el = d.FindElement(By.Name("title"));
                    return el.Displayed ? el : null;
                });
            }

            // 4. Nhập tên Collection
            nameInput.Clear();
            nameInput.SendKeys(collectionName);
            System.Threading.Thread.Sleep(500);

            // 5. Nếu yêu cầu Private
            if (isPrivate)
            {
                try {
                    var privateToggle = driver.FindElement(By.Name("privacy"));
                    if (!privateToggle.Selected) {
                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", privateToggle);
                    }
                } catch { }
            }

            // 6. Nhấn nút "Create collection" (Nút submit của form)
            // Nhấn bằng nút có class 'submit' hoặc text 'Create collection' bên trong 'form'
            var submitBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//form//button[@type='submit' and (contains(., 'Create') or contains(@class, 'submit'))]")));
            try {
                submitBtn.Click();
            } catch {
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", submitBtn);
            }
            
            Console.WriteLine("Collection created and photo added successfully.");
            System.Threading.Thread.Sleep(2000);
        }

        public void AddRandomPhotosToExistingCollection(int count, string collectionName)
        {
            Console.WriteLine($"Adding {count} more photos to collection: {collectionName}");
            var photos = wait.Until(d => d.FindElements(PhotoCards));
            int added = 0;
            
            // Bắt đầu quét từ các ảnh tiếp theo để tránh ảnh đầu tiên đã dùng
            for (int i = 3; i < photos.Count && added < count; i++)
            {
                var photo = photos[i];
                
                // 1. Lọc ảnh hợp lệ (không Sponsored, không Plus)
                string photoText = photo.Text.ToLower();
                if (photoText.Contains("sponsored") || photoText.Contains("plus") || photoText.Contains("premium")) continue;
                try {
                    var link = photo.FindElement(By.TagName("a")).GetAttribute("href") ?? "";
                    if (link.Contains("plus.unsplash.com")) continue;
                } catch { }

                // 2. Di chuột và Click "+"
                try {
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", photo);
                    System.Threading.Thread.Sleep(500);
                    new OpenQA.Selenium.Interactions.Actions(driver).MoveToElement(photo).Perform();
                    System.Threading.Thread.Sleep(800);

                    var addBtn = photo.FindElement(By.XPath(".//button[@aria-label='Add to Collection']"));
                    try { addBtn.Click(); } catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", addBtn); }

                    // 3. Kiểm tra modal Join
                    System.Threading.Thread.Sleep(1000);
                    if (driver.FindElements(By.XPath("//h1[contains(., 'Join Unsplash') or contains(., 'Unsplash+')]")).Count > 0)
                    {
                        Console.WriteLine("Triggered Join modal, skipping...");
                        var dismissBtn = driver.FindElement(By.XPath("//button[contains(@class, 'dismiss') or @aria-label='Close']"));
                        dismissBtn.Click();
                        continue;
                    }

                    // 4. Chọn Collection trong danh sách
                    var targetCollection = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath($"//h4[contains(., '{collectionName}')] | //button[contains(., '{collectionName}')]")));
                    try { targetCollection.Click(); } catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", targetCollection); }
                    
                    // 5. Đóng modal (thường tự đóng hoặc có nút Close hoặc cần ESC)
                    System.Threading.Thread.Sleep(1500);
                    try {
                        var closeBtn = driver.FindElements(By.XPath("//button[@aria-label='Close' or contains(@class, 'dismiss')]")).FirstOrDefault(b => b.Displayed);
                        if (closeBtn != null) {
                            closeBtn.Click();
                        } else {
                            new OpenQA.Selenium.Interactions.Actions(driver).SendKeys(Keys.Escape).Perform();
                        }
                    } catch { 
                        new OpenQA.Selenium.Interactions.Actions(driver).SendKeys(Keys.Escape).Perform();
                    }

                    added++;
                    Console.WriteLine($"Photo {added}/{count} added to '{collectionName}'.");
                    System.Threading.Thread.Sleep(500);

                } catch (Exception ex) {
                    Console.WriteLine($"Error adding photo {i}: {ex.Message}. Skipping...");
                    continue;
                }
            }

            if (added < count) throw new Exception($"Only added {added}/{count} photos.");
        }

        public void OpenRandomPhoto() // Mở một tấm ảnh ngẫu nhiên từ lưới ảnh (bỏ qua Sponsored/Plus)
        {
            Console.WriteLine("Scanning home page for a random photo...");
            
            // Đợi lưới ảnh hiển thị
            wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("figure[data-testid='photo-grid-masonry-figure'], figure[data-testid='asset-grid-masonry-figure']")));
            
            // Thử tối đa 3 lần nếu gặp StaleElement
            for (int retry = 0; retry < 3; retry++)
            {
                try {
                    var photos = driver.FindElements(By.CssSelector("figure[data-testid='photo-grid-masonry-figure'], figure[data-testid='asset-grid-masonry-figure']"));
                    
                    foreach (var photo in photos)
                    {
                        // Kiểm tra nhãn Sponsored/Plus bằng text nội bộ
                        string innerText = photo.Text;
                        if (innerText.Contains("Plus") || innerText.Contains("Sponsored")) continue;

                        // Tìm link ảnh
                        var link = photo.FindElements(By.CssSelector("a[href*='/photos/']")).FirstOrDefault();
                        if (link != null && link.Displayed)
                        {
                            Console.WriteLine($"Found a valid photo: {link.GetAttribute("href")}. Opening...");
                            try { link.Click(); } catch { ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", link); }
                            return;
                        }
                    }
                }
                catch (StaleElementReferenceException) {
                    Console.WriteLine("Stale element encountered. Retrying...");
                    System.Threading.Thread.Sleep(1000);
                }
            }
            
            throw new Exception("Could not find any suitable random photo to open after retries.");
        }
    }
}
