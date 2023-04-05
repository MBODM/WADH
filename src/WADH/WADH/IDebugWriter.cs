using System.Runtime.CompilerServices;
using Microsoft.Web.WebView2.Core;

namespace WADH
{
    public interface IDebugWriter
    {
        void PrintEventHeader([CallerMemberName] string caller = "");
        void PrintNavigationStarting(CoreWebView2NavigationStartingEventArgs e);
        void PrintNavigationCompleted(CoreWebView2NavigationCompletedEventArgs e, string url);
        void PrintDownloadStarting(CoreWebView2DownloadStartingEventArgs e);
        void PrintBytesReceivedChanged(CoreWebView2DownloadOperation sender);
        void PrintStateChanged(CoreWebView2DownloadOperation sender);
        void PrintInfo(string s);
    }
}
