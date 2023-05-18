using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Web.WebView2.Core;
using WADH.Core;

namespace WADH.WebView
{
    internal sealed class LogHelper : ILogHelper
    {
        private readonly IFileLogger fileLogger;

        public LogHelper(IFileLogger fileLogger)
        {
            this.fileLogger = fileLogger ?? throw new ArgumentNullException(nameof(fileLogger));
        }

        public void LogEvent([CallerMemberName] string caller = "",
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            var name = caller != string.Empty ? $"{nameof(WebViewHelper)}.{caller}()" : "Unknown";

            fileLogger.Log($"{name} event handler reached");

            Debug.WriteLine($"[{name.Split('.').Last().TrimEnd(')').TrimEnd('(')}] Reached");
        }

        public void LogNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs e,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            fileLogger.Log(new string[]
            {
                Event("NavigationStarting"),
                Sender(nameof(CoreWebView2)),
                Value("sender.Source", sender.Source),
                Args(nameof(CoreWebView2NavigationStartingEventArgs)),
                Value("e.Uri", e.Uri),
                Value("e.NavigationId", e.NavigationId),
                Value("e.IsRedirected", e.IsRedirected),
                Value("e.IsUserInitiated", e.IsUserInitiated)
            },
            file, line);
        }

        public void LogDOMContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs e,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            fileLogger.Log(new string[]
            {
                Event("DOMContentLoaded"),
                Sender(nameof(CoreWebView2)),
                Value("sender.Source", sender.Source),
                Args(nameof(CoreWebView2DOMContentLoadedEventArgs)),
                Value("e.NavigationId", e.NavigationId)
            },
            file, line);
        }

        public void LogNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs e,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            fileLogger.Log(new string[]
            {
                Event("NavigationCompleted"),
                Sender(nameof(CoreWebView2)),
                Value("sender.Source", sender.Source),
                Args(nameof(CoreWebView2NavigationCompletedEventArgs)),
                Value("e.NavigationId", e.NavigationId),
                Value("e.HttpStatusCode", e.HttpStatusCode),
                Value("e.WebErrorStatus", e.WebErrorStatus),
                Value("e.IsSuccess", e.IsSuccess)
            },
            file, line);
        }

        public void LogDownloadStarting(CoreWebView2 sender, CoreWebView2DownloadStartingEventArgs e,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            fileLogger.Log(new string[]
            {
                Event("DownloadStarting"),
                Sender(nameof(CoreWebView2)),
                Value("sender.Source", sender.Source),
                Args(nameof(CoreWebView2DownloadStartingEventArgs)),
                Value("e.Cancel", e.Cancel),
                Value("e.Handled", e.Handled),
                Value("e.DownloadOperation.Uri", e.DownloadOperation.Uri),
                Value("e.DownloadOperation.State", e.DownloadOperation.State),
                Value("e.DownloadOperation.ResultFilePath", e.DownloadOperation.ResultFilePath),
                Value("e.DownloadOperation.BytesReceived", e.DownloadOperation.BytesReceived),
                Value("e.DownloadOperation.TotalBytesToReceive", e.DownloadOperation.TotalBytesToReceive),
                Value("e.DownloadOperation.InterruptReason", e.DownloadOperation.InterruptReason),
            },
            file, line); ;
        }

        public void LogStateChanged(CoreWebView2DownloadOperation sender, object e,
             [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            LogDownloadOperation("StateChanged", sender, e);
        }

        public void LogBytesReceivedChanged(CoreWebView2DownloadOperation sender, object e,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            LogDownloadOperation("BytesReceivedChanged", sender, e);
        }

        public void LogNavigationStartingError([CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            fileLogger.Log("Error in NavigationStarting event occurred, cause of unexpected Curse behaviour.", file, line);
        }

        public void LogNavigationCompletedError([CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            fileLogger.Log("Error in NavigationCompleted event occurred, cause of unexpected Curse behaviour.", file, line);
        }

        public void LogBeforeScriptExecution(string reason, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            fileLogger.Log($"Execute script now, to {reason}...", file, line);
        }

        public void LogAfterScriptExecution([CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            fileLogger.Log("Script executed.", file, line);
        }

        private void LogDownloadOperation(string name, CoreWebView2DownloadOperation sender, object e,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            fileLogger.Log(new string[]
            {
                Event(name),
                Sender(nameof(CoreWebView2DownloadOperation)),
                Value("sender.Uri", sender.Uri),
                Value("sender.State", sender.State),
                Value("sender.ResultFilePath", sender.ResultFilePath),
                Value("sender.BytesReceived", sender.BytesReceived),
                Value("sender.TotalBytesToReceive", sender.TotalBytesToReceive),
                Value("sender.InterruptReason", sender.InterruptReason),
                Args(nameof(Object)),
                Value("e", e)
            },
            file, line);
        }

        private static string Event(string name)
        {
            return $"{name} event occurred";
        }

        private static string Sender(string type)
        {
            return $"sender is {type}";
        }

        private static string Args(string type)
        {
            return $"e is {type}";
        }

        private static string Value<T>(string name, T value)
        {
            return string.Join(" = ", name, value);
        }
    }
}
