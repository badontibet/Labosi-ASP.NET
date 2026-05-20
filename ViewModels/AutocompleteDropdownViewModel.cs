namespace NasIndexer.ViewModels
{
    public class AutocompleteDropdownViewModel
    {
        public string HiddenInputName { get; set; } = string.Empty;
        public string TextInputName { get; set; } = string.Empty;
        public int? SelectedId { get; set; }
        public string SelectedText { get; set; } = string.Empty;
        public string EndpointUrl { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Placeholder { get; set; } = string.Empty;
        public string ValidationFieldName { get; set; } = string.Empty;
    }
}
