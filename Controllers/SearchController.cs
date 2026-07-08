using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NasIndexer.Data;
using NasIndexer.ViewModels;

namespace NasIndexer.Controllers
{
    public class SearchController : Controller
    {
        private const int GroupLimit = 10;
        private readonly NasIndexerDbContext dbContext;

        public SearchController(NasIndexerDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q)
        {
            var query = NormalizeQuery(q);
            var viewModel = new GlobalSearchViewModel
            {
                Query = query
            };

            if (string.IsNullOrWhiteSpace(query))
            {
                viewModel.Message = "Enter a search term to find pages and NAS index data.";
                return View(viewModel);
            }

            viewModel.Groups.Add(SearchPages(query));
            viewModel.Groups.Add(await SearchNasServersAsync(query));
            viewModel.Groups.Add(await SearchDirectoriesAsync(query));
            viewModel.Groups.Add(await SearchFilesAsync(query));
            viewModel.Groups.Add(await SearchTagsAsync(query));
            viewModel.Groups.Add(await SearchScanJobsAsync(query));
            viewModel.Groups.Add(await SearchAttachmentsAsync(query));

            viewModel.Groups = viewModel.Groups
                .Where(group => group.Results.Any())
                .ToList();

            if (!viewModel.HasResults)
            {
                viewModel.Message = "No pages or NAS index records matched the search term.";
            }

            return View(viewModel);
        }

        private static string NormalizeQuery(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return string.Empty;
            }

            var trimmed = query.Trim();
            return trimmed.Length <= GlobalSearchViewModel.MaxQueryLength
                ? trimmed
                : trimmed[..GlobalSearchViewModel.MaxQueryLength];
        }

        private GlobalSearchGroupViewModel SearchPages(string query)
        {
            var pages = new[]
            {
                Page("Dashboard", "Page", "Operations summary and recent NAS activity.", Url.Action("Index", "Home") ?? "/"),
                Page("NAS Servers", "Page", "List, search, and manage NAS endpoint metadata.", Url.Action("Index", "NasServers") ?? "/servers"),
                Page("Scan Jobs", "Page", "List, search, and manage scan job telemetry.", Url.Action("Index", "ScanJobs") ?? "/scan-jobs"),
                Page("Directories", "Page", "List, search, and manage indexed directories.", Url.Action("Index", "Directories") ?? "/directories"),
                Page("Files", "Page", "List, search, and manage indexed files and attachments.", Url.Action("Index", "FileItems") ?? "/files"),
                Page("Tags", "Page", "List, search, and manage file tags.", Url.Action("Index", "Tags") ?? "/tags"),
                Page("Change Logs", "Page", "Search read-only file change history.", Url.Action("Index", "FileChangeLogs") ?? "/FileChangeLogs"),
                Page("Admins", "Page", "List and manage NAS operator metadata.", Url.Action("Index", "Admins") ?? "/admins")
            };

            return Group("Pages", "Page", pages
                .Where(page => Matches(query, page.Title, page.Description))
                .Take(GroupLimit)
                .ToList());
        }

        private async Task<GlobalSearchGroupViewModel> SearchNasServersAsync(string query)
        {
            var pattern = LikePattern(query);
            var canLinkDetails = User.Identity?.IsAuthenticated == true;

            var results = await dbContext.NasServers
                .AsNoTracking()
                .Where(server =>
                    EF.Functions.Like(server.Name, pattern) ||
                    EF.Functions.Like(server.IpAddress, pattern) ||
                    EF.Functions.Like(server.Username, pattern))
                .OrderBy(server => server.Name)
                .Take(GroupLimit)
                .Select(server => new
                {
                    server.Id,
                    server.Name,
                    server.IpAddress,
                    server.Port,
                    server.IsActive
                })
                .ToListAsync();

            return Group("NAS servers", "NAS server", results.Select(server => Result(
                server.Name,
                "NAS server",
                $"{server.IpAddress}:{server.Port} - {(server.IsActive ? "Active" : "Inactive")}",
                canLinkDetails ? Url.Action("Details", "NasServers", new { id = server.Id }) : Url.Action("Index", "NasServers"))));
        }

        private async Task<GlobalSearchGroupViewModel> SearchDirectoriesAsync(string query)
        {
            var pattern = LikePattern(query);
            var canLinkDetails = User.Identity?.IsAuthenticated == true;

            var results = await dbContext.DirectoryItems
                .AsNoTracking()
                .Where(directory =>
                    EF.Functions.Like(directory.Name, pattern) ||
                    EF.Functions.Like(directory.Path, pattern))
                .OrderBy(directory => directory.Path)
                .Take(GroupLimit)
                .Select(directory => new
                {
                    directory.Id,
                    directory.Name,
                    directory.Path
                })
                .ToListAsync();

            return Group("Directories", "Directory", results.Select(directory => Result(
                directory.Name,
                "Directory",
                directory.Path,
                canLinkDetails ? Url.Action("Details", "Directories", new { id = directory.Id }) : Url.Action("Index", "Directories"))));
        }

        private async Task<GlobalSearchGroupViewModel> SearchFilesAsync(string query)
        {
            var pattern = LikePattern(query);
            var canLinkDetails = User.Identity?.IsAuthenticated == true;

            var results = await dbContext.FileItems
                .AsNoTracking()
                .Where(file =>
                    EF.Functions.Like(file.Name, pattern) ||
                    EF.Functions.Like(file.Path, pattern) ||
                    EF.Functions.Like(file.Extension, pattern) ||
                    file.Tags.Any(tag => EF.Functions.Like(tag.Name, pattern)))
                .OrderBy(file => file.Name)
                .Take(GroupLimit)
                .Select(file => new
                {
                    file.Id,
                    file.Name,
                    file.Path,
                    file.Extension
                })
                .ToListAsync();

            return Group("Files", "File", results.Select(file => Result(
                file.Name,
                "File",
                $"{file.Extension} - {file.Path}",
                canLinkDetails ? Url.Action("Details", "FileItems", new { id = file.Id }) : Url.Action("Index", "FileItems"))));
        }

        private async Task<GlobalSearchGroupViewModel> SearchTagsAsync(string query)
        {
            var pattern = LikePattern(query);
            var canLinkDetails = User.Identity?.IsAuthenticated == true;

            var results = await dbContext.FileTags
                .AsNoTracking()
                .Where(tag =>
                    EF.Functions.Like(tag.Name, pattern) ||
                    EF.Functions.Like(tag.Description, pattern) ||
                    EF.Functions.Like(tag.Color, pattern))
                .OrderBy(tag => tag.Name)
                .Take(GroupLimit)
                .Select(tag => new
                {
                    tag.Id,
                    tag.Name,
                    tag.Description,
                    tag.Color
                })
                .ToListAsync();

            return Group("Tags", "Tag", results.Select(tag => Result(
                tag.Name,
                "Tag",
                $"{tag.Color} - {tag.Description}",
                canLinkDetails ? Url.Action("Details", "Tags", new { id = tag.Id }) : Url.Action("Index", "Tags"))));
        }

        private async Task<GlobalSearchGroupViewModel> SearchScanJobsAsync(string query)
        {
            var pattern = LikePattern(query);
            var canLinkDetails = User.Identity?.IsAuthenticated == true;

            var results = await dbContext.ScanJobs
                .AsNoTracking()
                .Where(scanJob =>
                    EF.Functions.Like(scanJob.RootPath, pattern) ||
                    EF.Functions.Like(scanJob.NasServer.Name, pattern))
                .OrderByDescending(scanJob => scanJob.StartTime)
                .Take(GroupLimit)
                .Select(scanJob => new
                {
                    scanJob.Id,
                    scanJob.RootPath,
                    scanJob.Status,
                    ServerName = scanJob.NasServer.Name
                })
                .ToListAsync();

            return Group("Scan jobs", "Scan job", results.Select(scanJob => Result(
                $"Scan job {scanJob.Id}",
                "Scan job",
                $"{scanJob.Status} - {scanJob.ServerName} - {scanJob.RootPath}",
                canLinkDetails ? Url.Action("Details", "ScanJobs", new { id = scanJob.Id }) : Url.Action("Index", "ScanJobs"))));
        }

        private async Task<GlobalSearchGroupViewModel> SearchAttachmentsAsync(string query)
        {
            var pattern = LikePattern(query);
            var canLinkDetails = User.Identity?.IsAuthenticated == true;

            var results = await dbContext.FileAttachments
                .AsNoTracking()
                .Where(attachment => EF.Functions.Like(attachment.OriginalFileName, pattern))
                .OrderBy(attachment => attachment.OriginalFileName)
                .Take(GroupLimit)
                .Select(attachment => new
                {
                    attachment.OriginalFileName,
                    attachment.FileSize,
                    attachment.FileItemId,
                    FileName = attachment.FileItem.Name
                })
                .ToListAsync();

            return Group("Attachments", "Attachment", results.Select(attachment => Result(
                attachment.OriginalFileName,
                "Attachment",
                $"Attached to {attachment.FileName} - {attachment.FileSize} bytes",
                canLinkDetails ? Url.Action("Details", "FileItems", new { id = attachment.FileItemId }) : Url.Action("Index", "FileItems"))));
        }

        private static GlobalSearchGroupViewModel Group(
            string name,
            string typeLabel,
            IEnumerable<GlobalSearchResultViewModel> results)
        {
            return new GlobalSearchGroupViewModel
            {
                Name = name,
                TypeLabel = typeLabel,
                Results = results.ToList()
            };
        }

        private static GlobalSearchResultViewModel Page(
            string title,
            string typeLabel,
            string description,
            string url)
        {
            return Result(title, typeLabel, description, url);
        }

        private static GlobalSearchResultViewModel Result(
            string title,
            string typeLabel,
            string description,
            string? url)
        {
            return new GlobalSearchResultViewModel
            {
                Title = title,
                TypeLabel = typeLabel,
                Description = description,
                Url = string.IsNullOrWhiteSpace(url) ? "/" : url
            };
        }

        private static bool Matches(string query, params string[] values)
        {
            return values.Any(value => value.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        private static string LikePattern(string query)
        {
            return $"%{query}%";
        }
    }
}
