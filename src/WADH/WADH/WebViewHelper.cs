using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using WADH.Core;

namespace WADH
{
    public sealed class WebViewHelper : IWebViewHelper
    {
        private const string NotInitializedError = "This instance was not initialized. Please call the initialization method first.";

        private WebView2? webView = null;
        private bool isInitialized = false;
        private readonly Queue<string> addonUrls = new();

        private readonly ICurseHelper curseHelper;

        public WebViewHelper(ICurseHelper curseHelper)
        {
            this.curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));
        }

        public event AsyncCompletedEventHandler? DownloadAddonsAsyncCompleted;
        public event ProgressChangedEventHandler? DownloadAddonsAsyncProgressChanged;

        public async Task InitAsync(WebView2 webView)
        {
            if (isInitialized)
            {
                return;
            }

            this.webView = webView;

            // The WebView2 user data folder (UDF) has to have write access and the default location is the executable´s folder.
            // Therefore some another folder (with write permissions) has to be specified here, used as the UDF for the WebView2.
            // The temp folder is used for the UDF here, since this match the temporary characteristic the UDF has in this case.
            // Also the application, when closed, do NOT try to delete the UDF temp folder, on purpose. Because the UDF contains
            // some .pma files, not accessible directly after the application has closed (Microsoft Edge doing some stuff there).
            // But in my opinion this is totally fine, since it´s the temp folder and this UDF is reused next time again anyway.

            var userDataFolder = Path.Combine(Path.GetFullPath(Path.GetTempPath()), "MBODM-WADH-WebView2-UDF");

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, new CoreWebView2EnvironmentOptions());

            // The Microsoft WebView2 docs say: The CoreWebView2InitializationCompleted event is fired even before
            // the EnsureCoreWebView2Async() method ends. Therefore just awaiting that method is all we need here.

            await webView.EnsureCoreWebView2Async(env);

            // Completely disable JS execution, cause of our "prevent the Curse 5 sec JS timer" concept.

            webView.CoreWebView2.Settings.IsScriptEnabled = false;

            isInitialized = true;
        }

        public void ShowStartPage()
        {
            if (webView == null) return; // Enforced by NRT

            var msg1 = "The addon download sites are loaded and rendered inside this web control, using Microsoft Edge.";
            var msg2 = "The app needs to do this, since https://www.curseforge.com is strictly protected by Cloudflare.";

            var html =
                "<html>" +
                    "<body style=\"" +
                        "margin: 0;" +
                        "padding: 0;" +
                        "font-family: verdana;" +
                        "font-size: small;" +
                        "color: white;" +
                        "background-color: lightskyblue;" +
                    "\">" +
                        "<div style=\"" +
                            "position: absolute;" +
                            "top: 50%;" +
                            "left: 50%;" +
                            "transform: translate(-50%, -50%);" +
                            "white-space: nowrap;" +
                            "background-color: steelblue;" +
                        "\">" +
                            $"<div style =\"margin: 10px;\">{msg1}</div>" +
                            $"<div style =\"margin: 10px;\">{msg2}</div>" +
                        "</div>" +
                    "</body>" +
                "</html>";

            webView.NavigateToString(html);
        }

        public void DownloadAddonsAsync(IEnumerable<string> addonUrls, string downloadFolder)
        {
            if (addonUrls is null)
            {
                throw new ArgumentNullException(nameof(addonUrls));
            }

            if (string.IsNullOrWhiteSpace(downloadFolder))
            {
                throw new ArgumentException($"'{nameof(downloadFolder)}' cannot be null or whitespace.", nameof(downloadFolder));
            }

            if (!addonUrls.Any())
            {
                throw new ArgumentException("Enumerable is empty.", nameof(addonUrls));
            }

            addonUrls.ToList().ForEach(url => this.addonUrls.Enqueue(url));

            if (!isInitialized || webView == null)
            {
                throw new InvalidOperationException(NotInitializedError);
            }

            webView.CoreWebView2.Profile.DefaultDownloadFolderPath = Path.GetFullPath(downloadFolder);

            webView.NavigationStarting += WebView_NavigationStarting;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;

            var url = this.addonUrls.Dequeue();

            if (webView.Source.ToString() == url)
            {
                // If the site has already been loaded then the events are not raised without this.
                // Happens when there is only 1 url in queue. Important i.e. for Start button state.

                webView.Reload();
            }
            else
            {
                // Kick off the whole event chain process, by loading the first url.

                webView.Source = new Uri(addonUrls.First());
            }
        }

        public void CancelDownloadAddonsAsync()
        {
            if (!isInitialized || webView == null)
            {
                throw new InvalidOperationException(NotInitializedError);
            }

            webView.Stop();
        }

        private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            DebugPrintHeader();

            DebugPrintLine("e.Uri", e.Uri);
            DebugPrintLine("e.NavigationId", e.NavigationId);
            DebugPrintLine("e.Cancel", e.Cancel);
            DebugPrintLine("e.IsRedirected", e.IsRedirected);

            // JS is disabled (cause of the Curse 5 sec timer), to navigate to the parsed href manually.
            // Therefore the 1st request, after the Curse addon site url, is not a redirect any longer.

            if ((curseHelper.IsAddonUrl(e.Uri) && !e.IsRedirected) ||
                (curseHelper.IsRedirect1Url(e.Uri) && !e.IsRedirected) ||
                (curseHelper.IsRedirect2Url(e.Uri) && e.IsRedirected) ||
                (curseHelper.IsRealDownloadUrl(e.Uri) && e.IsRedirected))
            {
                DebugPrintLine("Proceed with navigation, since url and redirect-state are both valid.");

                e.Cancel = false;
            }
            else
            {
                DebugPrintLine("Cancel navigation now, cause url is either none of the allowed Curse-Urls, or does not match the expected redirect-state.");

                e.Cancel = true;

                // Todo: Stop the whole process and make sure Completed handler is called with error.
                // Todo: Do we need webView.Stop() here, when e.Cancel = true ???
            }
        }

        private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            DebugPrintHeader();

            if (sender is WebView2 localWebView)
            {
                // At the time of writing this code the EventArgs e still not including the Uri.
                // Therefore using the WebView´s Source property value as some workaround here.
                // This should be no problem since the navigations are not running concurrently.
                // Have a look at https://github.com/MicrosoftEdge/WebView2Feedback/issues/580

                var url = localWebView.Source.ToString();

                if (!string.IsNullOrEmpty(url))
                {
                    DebugPrintLine("(sender as WebView2).Source", url);
                    DebugPrintLine("e.NavigationId", e.NavigationId);
                    DebugPrintLine("e.IsSuccess", e.IsSuccess);
                    DebugPrintLine("e.HttpStatusCode", e.HttpStatusCode);
                    DebugPrintLine("e.WebErrorStatus", e.WebErrorStatus);

                    if (e.IsSuccess && e.HttpStatusCode == 200 && e.WebErrorStatus == CoreWebView2WebErrorStatus.Unknown)
                    {
                        if (!curseHelper.IsAddonUrl(url))
                        {
                            DebugPrintLine($"Stop the process now, cause only navigations to Curse-Addon-Urls are allowed at this stage.");

                            // Todo: Stop the whole process and make sure Completed handler is called with error.
                        }

                        DebugPrintLine($"Executing script to fetch 'href' attribute from loaded Curse site ...");

                        await localWebView.ExecuteScriptAsync(curseHelper.AdjustPageAppearanceScript());
                        var href = await localWebView.ExecuteScriptAsync(curseHelper.GrabRedirectDownloadUrlScript());

                        href = href.Trim().Trim('"');
                        DebugPrintLine($"Script returned '{href}' as string.");

                        if (curseHelper.IsRedirect1Url(href))
                        {
                            DebugPrintLine($"The returned 'href' is a valid Curse-Redirect1-Url -> Manually navigating to that 'href' url now ...");

                            // This reflects the central logic of the "prevent the 5 sec JS timer" concept.
                            // By disabling JS, fetching the 'href' manually and loading that url manually.

                            localWebView.Stop();
                            localWebView.Source = new Uri(href);
                        }
                    }
                    else if (!e.IsSuccess && e.HttpStatusCode == 0 && e.WebErrorStatus == CoreWebView2WebErrorStatus.ConnectionAborted)
                    {
                        if (!curseHelper.IsRedirect1Url(url))
                        {
                            DebugPrintLine($"Stop the process now, cause only navigations to Curse-Redirect1-Urls are allowed at this stage.");

                            // Todo: Stop the whole process and make sure Completed handler is called with error.
                        }

                        DebugPrintLine($"The redirects and the Cloudflare processing has finished. Download should start now ...");
                    }
                    else
                    {
                        // Todo: Error Handling.
                    }
                }
            }
        }

        private void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            DebugPrintHeader();

            DebugPrintLine("e.DownloadOperation.Uri", e.DownloadOperation.Uri);
            DebugPrintLine("e.DownloadOperation.ResultFilePath", e.DownloadOperation.ResultFilePath);
            DebugPrintLine("e.DownloadOperation.State", e.DownloadOperation.State);
            DebugPrintLine("e.DownloadOperation.TotalBytesToReceive", e.DownloadOperation.TotalBytesToReceive);
            DebugPrintLine("e.Cancel", e.Cancel);

            e.DownloadOperation.StateChanged += DownloadOperation_StateChanged;

            DebugPrintLine("Registered e.DownloadOperation.StateChanged event handler.");

            e.Handled = true; // Do not show default download dialog
        }

        private void DownloadOperation_StateChanged(object? sender, object e)
        {
            DebugPrintHeader();

            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                DebugPrintLine("(sender as CoreWebView2DownloadOperation).State", downloadOperation.State);
                DebugPrintLine("(sender as CoreWebView2DownloadOperation).Uri", downloadOperation.Uri);
                DebugPrintLine("(sender as CoreWebView2DownloadOperation).ResultFilePath", downloadOperation.ResultFilePath);
                DebugPrintLine("(sender as CoreWebView2DownloadOperation).DownloadOperation.TotalBytesToReceive", downloadOperation.TotalBytesToReceive);
                DebugPrintLine("(sender as CoreWebView2DownloadOperation).DownloadOperation.BytesReceived", downloadOperation.BytesReceived);

                DebugPrintLine($"DownloadOperation.State changed to '{downloadOperation.State}' value.");

                switch (downloadOperation.State)
                {
                    case CoreWebView2DownloadState.InProgress:
                        // Download bytes progression is actually not supported.
                        break;
                    case CoreWebView2DownloadState.Interrupted:
                        // Todo: Stop the whole process and make sure Completed handler is called with error.
                        break;
                    case CoreWebView2DownloadState.Completed:
                        downloadOperation.StateChanged -= DownloadOperation_StateChanged;
                        HandleOneFinishedDownload();
                        break;
                    default:
                        throw new InvalidOperationException("Given value of CoreWebView2DownloadOperation.State is not supported.");
                }
            }
        }

        private void HandleOneFinishedDownload()
        {
            if (webView == null) return; // Enforced by NRT

            if (addonUrls.Any())
            {
                var url = addonUrls.Dequeue();

                DebugPrintLine($"Queue of urls is not empty yet, so proceeding with next url in queue -> '{url}'");

                webView.Source = new Uri(url); // Proceed with next url
            }
            else
            {
                DebugPrintLine("Queue of urls is empty, so there is nothing else to download -> Means: ALL DOWNLOADS FINISHED SUCCESSFULLY!");

                webView.NavigationStarting -= WebView_NavigationStarting;
                webView.NavigationCompleted -= WebView_NavigationCompleted;
                webView.CoreWebView2.DownloadStarting -= CoreWebView2_DownloadStarting;

                // Todo: Make sure Completed handler is called with success.
            }
        }

        private static void DebugPrintHeader([CallerMemberName] string caller = "")
        {
            Debug.WriteLine($"--------------------------------------------------------------------------------");

            if (Debugger.IsAttached)
            {
                var name = $"{nameof(WebViewHelper)}.{caller}";

                if (name.Contains("_"))
                {
                    // At the moment printing happens only inside of event handlers.
                    // Showing only the event name increase debug output readability.

                    name = name.Split('_').Last();
                }

                Debug.WriteLine($"[{name}]");
            }
        }

        private static void DebugPrintLine<T>(string key, T value)
        {
            Debug.WriteLine($"{key} = {value}");
        }

        private static void DebugPrintLine(string line)
        {
            Debug.WriteLine(line);
        }
    }
}
