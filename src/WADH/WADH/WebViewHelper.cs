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
        private bool cancellationFlag = false;

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

            cancellationFlag = false;

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

            cancellationFlag = true;
        }

        private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            DebugPrintHeader();

            DebugPrintValues("e.Uri", e.Uri);
            DebugPrintValues("e.NavigationId", e.NavigationId);
            DebugPrintValues("e.Cancel", e.Cancel);
            DebugPrintValues("e.IsRedirected", e.IsRedirected);

            // JS is disabled (cause of the Curse 5 sec timer), to navigate to the parsed href manually.
            // Therefore the 1st request, after the Curse addon site url, is not a redirect any longer.

            if ((curseHelper.IsAddonUrl(e.Uri) && !e.IsRedirected) ||
                (curseHelper.IsRedirect1Url(e.Uri) && !e.IsRedirected) ||
                (curseHelper.IsRedirect2Url(e.Uri) && e.IsRedirected) ||
                (curseHelper.IsRealDownloadUrl(e.Uri) && e.IsRedirected))
            {
                DebugPrintInfo("Proceed with navigation, since url and redirect-state are both valid.");

                e.Cancel = false;
            }
            else
            {
                DebugPrintInfo("Cancel navigation now, cause url is either none of the allowed Curse-Urls, or does not match the expected redirect-state.");

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
                    DebugPrintValues("sender.Source", url);
                    DebugPrintValues("e.NavigationId", e.NavigationId);
                    DebugPrintValues("e.IsSuccess", e.IsSuccess);
                    DebugPrintValues("e.HttpStatusCode", e.HttpStatusCode);
                    DebugPrintValues("e.WebErrorStatus", e.WebErrorStatus);

                    if (e.IsSuccess && e.HttpStatusCode == 200 && e.WebErrorStatus == CoreWebView2WebErrorStatus.Unknown)
                    {
                        if (!curseHelper.IsAddonUrl(url))
                        {
                            DebugPrintInfo($"Stop the process now, cause only navigations to Curse-Addon-Urls are allowed at this stage.");

                            // Todo: Stop the whole process and make sure Completed handler is called with error.
                        }

                        DebugPrintInfo($"Execute script now, to fetch 'href' attribute from loaded Curse site ...");

                        await localWebView.ExecuteScriptAsync(curseHelper.AdjustPageAppearanceScript());
                        var href = await localWebView.ExecuteScriptAsync(curseHelper.GrabRedirectDownloadUrlScript());

                        href = href.Trim().Trim('"');
                        DebugPrintInfo($"Script returned '{href}' as string.");

                        if (curseHelper.IsRedirect1Url(href))
                        {
                            DebugPrintInfo($"That returned string is a valid Curse-Redirect1-Url -> Manually navigating to that url now ...");

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
                            DebugPrintInfo($"Stop the process now, cause only navigations to Curse-Redirect1-Urls are allowed at this stage.");

                            // Todo: Stop the whole process and make sure Completed handler is called with error.
                        }

                        DebugPrintInfo($"The redirects and the Cloudflare processing has finished. Download should start now ...");
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

            DebugPrintValues("e.DownloadOperation.State", e.DownloadOperation.State);
            DebugPrintValues("e.DownloadOperation.Uri", e.DownloadOperation.Uri);
            DebugPrintValues("e.DownloadOperation.ResultFilePath", e.DownloadOperation.ResultFilePath);
            DebugPrintValues("e.DownloadOperation.TotalBytesToReceive", e.DownloadOperation.TotalBytesToReceive);
            DebugPrintValues("e.Cancel", e.Cancel);

            e.DownloadOperation.BytesReceivedChanged += DownloadOperation_BytesReceivedChanged;
            e.DownloadOperation.StateChanged += DownloadOperation_StateChanged;
            
            DebugPrintInfo("Registered DownloadOperation.BytesReceivedChanged event handler.");
            DebugPrintInfo("Registered DownloadOperation.StateChanged event handler.");

            e.Handled = true; // Do not show default download dialog
        }

        private void DownloadOperation_BytesReceivedChanged(object? sender, object e)
        {
            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                // Only show real chunks and not just the final chunk, when there is only one.
                // This happens sometimes for mid-sized files. The very small ones create no
                // event at all. The very big ones create a bunch of events. But for all the
                // mid-sized files there is only 1 event with i.e. 12345/12345 byte progress.
                // Therefore it seems OK to ignore them, for better debug output readability.

                if ((ulong)downloadOperation.BytesReceived < downloadOperation.TotalBytesToReceive)
                {
                    DebugPrintHeader();

                    DebugPrintValues("sender.State", downloadOperation.State);
                    DebugPrintValues("sender.Uri", downloadOperation.Uri);
                    DebugPrintInfo($"Received {downloadOperation.BytesReceived} / {downloadOperation.TotalBytesToReceive} bytes.");

                    // We do this inside above if clause, to finish the download of smaller files.

                    if (cancellationFlag)
                    {
                        downloadOperation.Cancel();
                    }
                }
            }
        }

        private void DownloadOperation_StateChanged(object? sender, object e)
        {
            DebugPrintHeader();

            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                DebugPrintValues("sender.State", downloadOperation.State);
                DebugPrintValues("sender.Uri", downloadOperation.Uri);
                DebugPrintValues("sender.ResultFilePath", downloadOperation.ResultFilePath);
                DebugPrintValues("sender.DownloadOperation.BytesReceived", downloadOperation.BytesReceived);
                DebugPrintValues("sender.DownloadOperation.TotalBytesToReceive", downloadOperation.TotalBytesToReceive);
                DebugPrintValues("sender.InterruptReason", downloadOperation.InterruptReason);
                
                if (downloadOperation.State == CoreWebView2DownloadState.Completed || downloadOperation.State == CoreWebView2DownloadState.Interrupted)
                {
                    downloadOperation.BytesReceivedChanged -= DownloadOperation_BytesReceivedChanged;
                    downloadOperation.StateChanged -= DownloadOperation_StateChanged;

                    DebugPrintInfo("Unregistered DownloadOperation.BytesReceivedChanged event handler.");
                    DebugPrintInfo("Unregistered DownloadOperation.StateChanged event handler.");

                    if (webView == null) return; // Enforced by NRT

                    if (!addonUrls.Any() || cancellationFlag)
                    {
                        // No more addons in queue to download, or cancellation happened.

                        DebugPrintInfo(cancellationFlag ?
                            $"Queue of urls is not empty yet, but CANCELLATION was detected -> Means: STOP AND NOT PROCEED WITH NEXT URL!" :
                            "Queue of urls is empty, so there is nothing else to download -> Means: ALL DOWNLOADS FINISHED SUCCESSFULLY!");
                        
                        webView.NavigationStarting -= WebView_NavigationStarting;
                        webView.NavigationCompleted -= WebView_NavigationCompleted;
                        webView.CoreWebView2.DownloadStarting -= CoreWebView2_DownloadStarting;
                        
                        DownloadAddonsAsyncCompleted?.Invoke(this, new AsyncCompletedEventArgs(null, cancellationFlag, null));
                    }
                    else
                    {
                        // Still some addons in queue to download, so proceed with next url.
                        
                        var url = addonUrls.Dequeue();
                        DebugPrintInfo($"Queue of urls is not empty yet, so proceed with next url in queue -> {url}");
                        webView.Source = new Uri(url);
                    }
                }
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

        private static void DebugPrintValues<T>(string key, T value)
        {
            Debug.WriteLine($"{key} = {value}");
        }

        private static void DebugPrintInfo(string line)
        {
            Debug.WriteLine($"Info: {line}");
        }
    }
}
