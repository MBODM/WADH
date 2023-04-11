﻿using System.Runtime.CompilerServices;
using Microsoft.Web.WebView2.Core;

namespace WADH
{
    public interface IDebugWriter
    {
        void PrintEventHeader([CallerMemberName] string caller = "");
        void PrintEventNavigationStarting(CoreWebView2NavigationStartingEventArgs e);
        void PrintEventNavigationCompleted(CoreWebView2NavigationCompletedEventArgs e, string url);
        void PrintEventDownloadStarting(CoreWebView2DownloadStartingEventArgs e);
        void PrintEventReceivedChanged(CoreWebView2DownloadOperation sender);
        void PrintEventStateChanged(CoreWebView2DownloadOperation sender);
        void PrintInfo(string s);
    }
}
