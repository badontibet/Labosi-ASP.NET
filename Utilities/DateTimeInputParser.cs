using System.Globalization;

namespace NasIndexer.Utilities
{
    public static class DateTimeInputParser
    {
        private static readonly string[] SupportedFormats =
        {
            "dd.MM.yyyy HH:mm",
            "d.M.yyyy HH:mm",
            "MM/dd/yyyy HH:mm",
            "M/d/yyyy HH:mm",
            "yyyy-MM-dd HH:mm"
        };

        public static bool TryParse(string? value, out DateTime dateTime)
        {
            return DateTime.TryParseExact(
                value?.Trim(),
                SupportedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out dateTime);
        }

        public static bool TryParseOptional(string? value, out DateTime? dateTime)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                dateTime = null;
                return true;
            }

            if (TryParse(value, out var parsed))
            {
                dateTime = parsed;
                return true;
            }

            dateTime = null;
            return false;
        }

        public static string Format(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        public static string? Format(DateTime? value)
        {
            return value.HasValue ? Format(value.Value) : null;
        }
    }
}
