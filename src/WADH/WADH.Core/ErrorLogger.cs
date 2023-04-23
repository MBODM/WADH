using System.Runtime.CompilerServices;
using System.Text;

namespace WADH.Core
{
    public sealed class ErrorLogger : IErrorLogger
    {
        private readonly object syncRoot = new();
        private readonly string logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WADH.log");
        private readonly string newLine = Environment.NewLine;

        public void Log(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException($"'{nameof(message)}' cannot be null or whitespace.", nameof(message));
            }

            lock (syncRoot)
            {
                WriteLogEntry("Message", file, line, message);
            }
        }

        public void Log(IEnumerable<string> multiLineMessage, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (multiLineMessage is null)
            {
                throw new ArgumentNullException(nameof(multiLineMessage));
            }

            if (!multiLineMessage.Any())
            {
                throw new ArgumentNullException(nameof(multiLineMessage), "Enumerable cannot be empty.");
            }

            var message = string.Join(newLine, multiLineMessage);

            lock (syncRoot)
            {
                WriteLogEntry("Message", file, line, message);
            }
        }

        public void Log(Exception exception, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            var message = $"Exception-Type: {exception.GetType().Name}{newLine}" + $"Exception-Message: {exception.Message}";

            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                var formattedStackTrace = exception.StackTrace.Replace(newLine, string.Empty).Replace("   at ", $"{newLine}at ");

                message += $"{newLine}Exception-StackTrace:{formattedStackTrace}";
            }

            lock (syncRoot)
            {
                WriteLogEntry("Exception", file, line, message);
            }
        }

        private void WriteLogEntry(string header, string file, int line, string message)
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            file = Path.GetFileName(file);

            var text = $"[{now}] {header}{newLine}File: {file}{newLine}Line: {line}{newLine}{message}{newLine}{newLine}";

            File.AppendAllText(logFile, text, Encoding.UTF8);
        }
    }
}
