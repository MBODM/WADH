using System.Runtime.CompilerServices;
using Microsoft.Web.WebView2.Core;

namespace WADH.WebView
{
    public interface IDebugWriter
    {
        void PrintEventHeader([CallerMemberName] string caller = "");
        void PrintEventNavigationStarting(CoreWebView2NavigationStartingEventArgs e);
        void PrintEventDOMContentLoaded(CoreWebView2DOMContentLoadedEventArgs e, string url);
        void PrintEventNavigationCompleted(CoreWebView2NavigationCompletedEventArgs e, string url);
        void PrintEventDownloadStarting(CoreWebView2DownloadStartingEventArgs e);
        void PrintEventReceivedChanged(CoreWebView2DownloadOperation sender);
        void PrintEventStateChanged(CoreWebView2DownloadOperation sender);
        void PrintInfo(string s);
    }
}
