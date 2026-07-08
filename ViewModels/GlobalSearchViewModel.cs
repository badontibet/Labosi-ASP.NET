namespace NasIndexer.ViewModels
{
    public class GlobalSearchViewModel
    {
        public const int MaxQueryLength = 80;

        public string Query { get; set; } = string.Empty;
        public string? Message { get; set; }
        public List<GlobalSearchGroupViewModel> Groups { get; set; } = new();
        public bool HasQuery => !string.IsNullOrWhiteSpace(Query);
        public bool HasResults => Groups.Any(group => group.Results.Any());
    }

    public class GlobalSearchGroupViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string TypeLabel { get; set; } = string.Empty;
        public List<GlobalSearchResultViewModel> Results { get; set; } = new();
    }

    public class GlobalSearchResultViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string TypeLabel { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
