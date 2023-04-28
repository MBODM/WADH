using System.ComponentModel;
using Microsoft.Web.WebView2.Core;

namespace WADH.WebView
{
    public interface IWebViewHelper
    {
        Task<CoreWebView2Environment> CreateEnvironmentAsync();
        void Initialize(CoreWebView2 coreWebView);
        void ShowStartPage();

        // Not using some TAP pattern here, to encapsulate the EAP pattern (on which WebView2 is built on). On purpose. Here is why:
        // It is technically possible to wrap an EAP pattern into the TAP pattern, with some help of the TaskCompletionSource class.
        // This would be possible here too. But the WebView2 is built on the EAP pattern, to naturally fit the event-driven concepts
        // of a WinForms or WPF application. Using the TAP pattern here would work somewhat against that natural direction. But even
        // when this makes sense in some form, it is not really a problem or the real reason why the TAP pattern is a bad idea here.
        // The real problem is: Since this shall be a wrapper (around the WebView2 WinForms/WPF component) it has shared state. Some
        // typically "async Task DownloadAsync()" TAP method would make the user believe he can run the method concurrently, i.e. in
        // some Task.WhenAll() environment. But this is not true at all. Indeed that method even needs to use some boolen lock flag,
        // to prevent being started again if it is already running. Otherwise there will be more than one access to the same shared
        // WebView2 component, at the same time. Which would end up in some unpredictable and error prone behaviour and will lead to
        // a crashing WebView2 component. For such a TAP approach it would be necessary to also instantiate the WebView2 inside the
        // TAP method. Which just makes no sense for this use case. Therefore creating some TAP method is not beneficial at all here.

        // Using an EAP approach here, based on the following best practices:
        // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/event-based-asynchronous-pattern-overview

        event AsyncCompletedEventHandler DownloadAddonsAsyncCompleted;
        event ProgressChangedEventHandler DownloadAddonsAsyncProgressChanged;

        void DownloadAddonsAsync(IEnumerable<string> addonUrls, string downloadFolder);
        void CancelDownloadAddonsAsync();
    }
}
