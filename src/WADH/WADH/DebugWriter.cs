using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Web.WebView2.Core;

namespace WADH
{
    public sealed class DebugWriter : IDebugWriter
    {
        public void PrintEventHeader([CallerMemberName] string caller = "")
        {
            Debug.WriteLine($"--------------------------------------------------------------------------------");

            if (Debugger.IsAttached)
            {
                var name = $"{nameof(WebViewHelper)}.{caller}";

                if (name.Contains('_'))
                {
                    // At the moment printing happens only inside of event handlers.
                    // Showing only the event name increase debug output readability.

                    name = name.Split('_').Last();
                }

                Debug.WriteLine($"[{name}]");
            }
        }

        public void PrintEventNavigationStarting(CoreWebView2NavigationStartingEventArgs e)
        {
            PrintValue("e.Uri", e.Uri);
            PrintValue("e.NavigationId", e.NavigationId);
            PrintValue("e.Cancel", e.Cancel);
            PrintValue("e.IsRedirected", e.IsRedirected);
        }

        public void PrintEventNavigationCompleted(CoreWebView2NavigationCompletedEventArgs e, string url)
        {
            PrintValue("sender.Source", url);
            PrintValue("e.NavigationId", e.NavigationId);
            PrintValue("e.IsSuccess", e.IsSuccess);
            PrintValue("e.HttpStatusCode", e.HttpStatusCode);
            PrintValue("e.WebErrorStatus", e.WebErrorStatus);
        }

        public void PrintEventDownloadStarting(CoreWebView2DownloadStartingEventArgs e)
        {
            PrintValue("e.DownloadOperation.State", e.DownloadOperation.State);
            PrintValue("e.DownloadOperation.Uri", e.DownloadOperation.Uri);
            PrintValue("e.DownloadOperation.ResultFilePath", e.DownloadOperation.ResultFilePath);
            PrintValue("e.DownloadOperation.TotalBytesToReceive", e.DownloadOperation.TotalBytesToReceive);
            PrintValue("e.Cancel", e.Cancel);
        }

        public void PrintEventReceivedChanged(CoreWebView2DownloadOperation sender)
        {
            PrintValue("sender.State", sender.State);
            PrintValue("sender.Uri", sender.Uri);
        }

        public void PrintEventStateChanged(CoreWebView2DownloadOperation sender)
        {
            PrintValue("sender.State", sender.State);
            PrintValue("sender.Uri", sender.Uri);
            PrintValue("sender.ResultFilePath", sender.ResultFilePath);
            PrintValue("sender.DownloadOperation.BytesReceived", sender.BytesReceived);
            PrintValue("sender.DownloadOperation.TotalBytesToReceive", sender.TotalBytesToReceive);
            PrintValue("sender.InterruptReason", sender.InterruptReason);
        }

        public void PrintInfo(string line)
        {
            Debug.WriteLine($"Info: {line}");
        }

        private static void PrintValue<T>(string key, T value)
        {
            Debug.WriteLine($"{key} = {value}");
        }
    }
}
