using System.Globalization;

namespace NasIndexer.Services
{
    public class AppFileLogger
    {
        private readonly SemaphoreSlim writeLock = new(1, 1);
        private readonly string logDirectory;

        public AppFileLogger(IWebHostEnvironment environment, IConfiguration configuration)
        {
            logDirectory = configuration["AppFileLogging:RootPath"]
                ?? Path.Combine(environment.ContentRootPath, "logs");
        }

        public async Task LogAsync(string level, string message, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(logDirectory);

            var now = DateTimeOffset.Now;
            var fileName = $"app-{now:yyyyMMdd}.log";
            var path = Path.Combine(logDirectory, fileName);
            var line = string.Create(
                CultureInfo.InvariantCulture,
                $"{now:O} level={Sanitize(level)} {message}{Environment.NewLine}");

            await writeLock.WaitAsync(cancellationToken);
            try
            {
                await File.AppendAllTextAsync(path, line, cancellationToken);
            }
            finally
            {
                writeLock.Release();
            }
        }

        public string GetCurrentLogFilePath()
        {
            var fileName = $"app-{DateTimeOffset.Now:yyyyMMdd}.log";
            return Path.Combine(logDirectory, fileName);
        }

        public static string Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "-";
            }

            return value
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal)
                .Replace("\t", " ", StringComparison.Ordinal)
                .Trim();
        }
    }
}
