using System.Runtime.CompilerServices;
using Microsoft.Web.WebView2.Core;

namespace WADH.WebView
{
    internal interface ILogHelper
    {
        void LogEvent([CallerMemberName] string caller = "",
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void LogNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs e,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void LogDOMContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs e,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void LogNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs e,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void LogDownloadStarting(CoreWebView2 sender, CoreWebView2DownloadStartingEventArgs e,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void LogStateChanged(CoreWebView2DownloadOperation sender, object e,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void LogBytesReceivedChanged(CoreWebView2DownloadOperation sender, object e,
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);

        void LogNavigationStartingError([CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void LogNavigationCompletedError([CallerFilePath] string file = "", [CallerLineNumber] int line = 0);

        void LogBeforeScriptExecution(string reason, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void LogAfterScriptExecution([CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
    }
}
