using System.ComponentModel;
using Microsoft.Web.WebView2.Core;

namespace WADH.WebView
{
    public interface IWebViewHelper
    {
        Task<CoreWebView2Environment> CreateEnvironmentAsync();
        void Initialize(CoreWebView2 coreWebView);
        void ShowStartPage();

        // Not using the TAP pattern here, to encapsulate the EAP pattern (on which WebView2 is built on). On purpose! And here is why:
        // It is technically possible to wrap the TAP pattern around the EAP pattern, with some help of the TaskCompletionSource class.
        // This would be possible here too. But the WebView2 is built on the EAP pattern, to naturally fit the event-driven concepts of
        // a WinForms or WPF application. Using the TAP pattern here would work somewhat against that natural direction. But even when
        // this makes some sense, it is not really a problem or the reason why the TAP pattern is a bad idea here. The real problem is:
        // Since the stuff here shall act as a wrapper (around the WebView2 WinForms/WPF component), the wrapper will have shared state.
        // A typical "async Task DownloadAsync()" TAP method would make a user believe he is able to call that TAP method concurrently,
        // in example in some Task.WhenAll() environment. But this is not true at all! In fact that TAP method would even have to use a
        // boolean lock flag, to protect itself from being started again, if already running. Cause otherwise there would be more than
        // one access to the same shared WebView2 component, at the same time. Which would end up in some unpredictable and error prone
        // behaviour, which will lead to a crashing WebView2 component. The bottom line is: Such a TAP approach would make it necessary
        // to instantiate the WebView2 inside that TAP method. All of this just makes no sense for the given use case. Therefore using
        // the TAP pattern is not beneficial at all here. So, using a typical EAP approach here, based on the following best practices:
        // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/event-based-asynchronous-pattern-overview

        event AsyncCompletedEventHandler DownloadAddonsAsyncCompleted;
        event ProgressChangedEventHandler DownloadAddonsAsyncProgressChanged;

        void DownloadAddonsAsync(IEnumerable<string> addonUrls, string downloadFolder);
        void CancelDownloadAddonsAsync();
    }
}
