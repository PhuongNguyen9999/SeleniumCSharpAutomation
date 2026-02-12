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
            try
            {
                var nameWait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
                var result = nameWait.Until(d => {
                    try
                    {
                        // If the page returns a 404 or 'Page not found', bail out and return empty
                        if (!string.IsNullOrEmpty(d.Title) && d.Title.IndexOf("Page not found", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Console.WriteLine("Profile page shows 'Page not found'.");
                            return string.Empty;
                        }

                        // Try several likely selectors for the full name (page templates vary)
                        var candidates = new By[] {
                            FullNameDisplay,
                            By.CssSelector("h1[data-testid='profile-name']"),
                            By.CssSelector("h1.profile-name"),
                            By.XPath("//h1"),
                        };

                        foreach (var sel in candidates)
                        {
                            try
                            {
                                var els = d.FindElements(sel);
                                if (els != null && els.Count > 0)
                                {
                                    var el = els[0];
                                    if (el.Displayed)
                                    {
                                        var txt = el.Text?.Trim();
                                        if (!string.IsNullOrEmpty(txt))
                                        {
                                            Console.WriteLine($"Found full name via {sel}: {txt}");
                                            return txt;
                                        }
                                    }
                                }
                            }
                            catch { }
                        }

                        // As a last resort, check meta og:title which often contains the display name
                        try
                        {
                            var metas = d.FindElements(By.CssSelector("meta[property='og:title']"));
                            if (metas != null && metas.Count > 0)
                            {
                                var content = metas[0].GetAttribute("content")?.Trim();
                                if (!string.IsNullOrEmpty(content)) return content;
                            }
                        }
                        catch { }

                        return null; // keep waiting
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"GetFullName interim error: {ex.Message}");
                        return null;
                    }
                });

                return result ?? string.Empty;
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("Timed out while trying to read full name from Profile page; returning empty string.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in GetFullName: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
