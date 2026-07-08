using Microsoft.Playwright;

namespace Labosi_ASP.NET.E2ETests
{
    public class AdminTagWorkflowE2ETests : IClassFixture<E2EWebAppFixture>
    {
        private readonly E2EWebAppFixture app;

        public AdminTagWorkflowE2ETests(E2EWebAppFixture app)
        {
            this.app = app;
        }

        [Fact]
        public async Task AdminCanSearchCreateOpenEditAndVerifyTag()
        {
            var tagName = $"E2E Tag {Guid.NewGuid():N}"[..22];
            var updatedDescription = $"Updated by Playwright {Guid.NewGuid():N}"[..45];

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                ExecutablePath = FindInstalledChromium()
            });
            var page = await browser.NewPageAsync();

            // Step 1: Admin opens the public home page.
            await page.GotoAsync(app.BaseUrl);
            await ExpectVisibleAsync(page.GetByRole(AriaRole.Heading, new() { Name = "NAS Operations Control Center" }));

            // Step 2: Admin opens the local login page.
            await page.GetByRole(AriaRole.Link, new() { Name = "Login" }).ClickAsync();
            await ExpectVisibleAsync(page.GetByRole(AriaRole.Heading, new() { Name = "Log in", Exact = true }));

            // Step 3: Admin logs in with the isolated E2E account.
            await page.GetByLabel("Email").FillAsync(app.AdminEmail);
            await page.GetByLabel("Password").FillAsync(app.AdminPassword);
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new("Log in|Login", System.Text.RegularExpressions.RegexOptions.IgnoreCase) }).ClickAsync();
            await ExpectVisibleAsync(page.GetByText(app.AdminEmail));

            // Step 4: Admin uses global search for the Tags page.
            await page.GetByLabel("Global search").FillAsync("tags");
            await page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();
            await ExpectVisibleAsync(page.GetByRole(AriaRole.Heading, new() { Name = "Find NAS Indexer Data" }));

            // Step 5: Admin opens the Tags page from the search results.
            await page.GetByRole(AriaRole.Link, new() { Name = "Tags" }).First.ClickAsync();
            await ExpectVisibleAsync(page.GetByRole(AriaRole.Heading, new() { Name = "File Tag Registry" }));

            // Step 6: Admin opens the Create Tag form.
            await page.GetByRole(AriaRole.Link, new() { Name = "Create tag" }).ClickAsync();
            await ExpectVisibleAsync(page.GetByRole(AriaRole.Heading, new() { Name = "Create File Tag" }));

            // Step 7: Admin creates a safe NAS Indexer tag.
            await page.GetByLabel("Name").FillAsync(tagName);
            await page.GetByLabel("Description").FillAsync("Created by Playwright E2E");
            await page.GetByLabel("Color").FillAsync("#22AA99");
            await page.GetByRole(AriaRole.Button, new() { Name = "Create" }).ClickAsync();
            await ExpectVisibleAsync(page.GetByText(tagName, new() { Exact = true }));

            // Step 8: Admin searches globally for the new tag.
            await page.GetByLabel("Global search").FillAsync(tagName);
            await page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();
            await ExpectVisibleAsync(page.GetByRole(AriaRole.Heading, new() { Name = "Tags" }));

            // Step 9: Admin opens the tag details page from global search.
            await page.GetByRole(AriaRole.Link, new() { Name = tagName }).ClickAsync();
            await ExpectVisibleAsync(page.GetByRole(AriaRole.Heading, new() { Name = tagName }));

            // Step 10: Admin opens the edit page.
            await page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync();
            await ExpectVisibleAsync(page.GetByRole(AriaRole.Heading, new() { Name = "Edit File Tag" }));

            // Step 11: Admin changes one safe text field and saves.
            await page.GetByLabel("Description").FillAsync(updatedDescription);
            await page.GetByRole(AriaRole.Button, new() { Name = "Save changes" }).ClickAsync();

            // Step 12: Admin verifies the updated content appears back on the list.
            await ExpectVisibleAsync(page.GetByRole(AriaRole.Heading, new() { Name = "File Tag Registry" }));
            await ExpectVisibleAsync(page.GetByText(updatedDescription));
            await ExpectVisibleAsync(page.GetByText(tagName, new() { Exact = true }));
        }

        private static async Task ExpectVisibleAsync(ILocator locator)
        {
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 30_000
            });
        }

        private static string? FindInstalledChromium()
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ms-playwright");

            if (!Directory.Exists(root))
            {
                return null;
            }

            return Directory
                .GetDirectories(root, "chromium-*")
                .Select(directory => Path.Combine(directory, "chrome-win64", "chrome.exe"))
                .FirstOrDefault(File.Exists);
        }
    }
}
