namespace NasIndexer.ViewModels
{
    public class DateTimePickerViewModel
    {
        public string FieldName { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Value { get; set; }
        public bool IsRequired { get; set; }
        public string HelpText { get; set; } = "Format: hr dd.MM.yyyy HH:mm; en MM/dd/yyyy HH:mm; server-safe yyyy-MM-dd HH:mm.";
    }
}
