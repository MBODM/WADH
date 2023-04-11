using System.ComponentModel;
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
        private bool cancellationRequested = false;
        private int addonCount = 0;
        private int finishedCounter = 0;
        private bool isRunning = false;

        private readonly Queue<string> addonUrls = new();

        private readonly IDebugWriter debugWriter;
        private readonly ICurseHelper curseHelper;

        public WebViewHelper(IDebugWriter debugWriter, ICurseHelper curseHelper)
        {
            this.debugWriter = debugWriter ?? throw new ArgumentNullException(nameof(debugWriter));
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

            if (!addonUrls.Any())
            {
                throw new ArgumentException("Enumerable is empty.", nameof(addonUrls));
            }

            if (string.IsNullOrWhiteSpace(downloadFolder))
            {
                throw new ArgumentException($"'{nameof(downloadFolder)}' cannot be null or whitespace.", nameof(downloadFolder));
            }

            if (!isInitialized || webView == null)
            {
                throw new InvalidOperationException(NotInitializedError);
            }

            if (isRunning)
            {
                throw new InvalidOperationException("Download is already running.");
            }

            isRunning = true;

            this.addonUrls.Clear();
            addonUrls.ToList().ForEach(url => this.addonUrls.Enqueue(url));
            addonCount = addonUrls.Count();

            cancellationRequested = false;

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

            cancellationRequested = true;
        }

        private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            debugWriter.PrintEventHeader();
            debugWriter.PrintEventNavigationStarting(e);

            // JS is disabled (cause of the Curse 5 sec timer), to navigate to the parsed href manually.
            // Therefore the 1st request, after the Curse addon site url, is not a redirect any longer.

            if ((curseHelper.IsAddonUrl(e.Uri) && !e.IsRedirected) ||
                (curseHelper.IsRedirect1Url(e.Uri) && !e.IsRedirected) ||
                (curseHelper.IsRedirect2Url(e.Uri) && e.IsRedirected) ||
                (curseHelper.IsDownloadUrl(e.Uri) && e.IsRedirected))
            {
                var message = "Is allowed Curse-Url, with valid redirect-state.";
                debugWriter.PrintInfo($"{message} --> Proceed with navigation.");

                if (curseHelper.IsAddonUrl(e.Uri))
                {
                    var addon = curseHelper.GetAddonNameFromAddonUrl(e.Uri);
                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.CurseAddonSiteLoaded, e.Uri, message, addon);
                }
                else
                {
                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.CursePreludeProgress, e.Uri, message);
                }

                e.Cancel = false;
            }
            else
            {
                var message = "Cancel navigation now, cause url is either none of the allowed Curse-Urls, or does not match the expected redirect-state.";
                debugWriter.PrintInfo(message);
                OnDownloadAddonsAsyncCompleted(e.Uri, false, new InvalidOperationException(message));

                e.Cancel = true; // Todo: Do we need webView.Stop() here, when e.Cancel = true ???
            }
        }

        private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            debugWriter.PrintEventHeader();

            if (sender is WebView2 localWebView)
            {
                // At the time of writing this code the EventArgs e still not including the Uri.
                // Therefore using the WebView´s Source property value as some workaround here.
                // This should be no problem since the navigations are not running concurrently.
                // Have a look at https://github.com/MicrosoftEdge/WebView2Feedback/issues/580

                var url = localWebView.Source.ToString();

                if (!string.IsNullOrEmpty(url))
                {
                    debugWriter.PrintEventNavigationCompleted(e, url);

                    if (e.IsSuccess && e.HttpStatusCode == 200 && e.WebErrorStatus == CoreWebView2WebErrorStatus.Unknown)
                    {
                        debugWriter.PrintInfo("Navigation to Curse addon site successfully completed.");
                        debugWriter.PrintInfo("Execute script now, to fetch 'href' attribute from loaded Curse addon site ...");

                        await localWebView.ExecuteScriptAsync(curseHelper.AdjustPageAppearanceScript());
                        var href = await localWebView.ExecuteScriptAsync(curseHelper.GrabRedirectDownloadUrlScript());

                        href = href.Trim().Trim('"');
                        debugWriter.PrintInfo($"Script returned '{href}' as string.");

                        if (curseHelper.IsRedirect1Url(href))
                        {
                            debugWriter.PrintInfo("That returned string is a valid Curse-Redirect1-Url. --> Manually navigate to that url now ...");
                            OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.CurseAddonSiteLoaded, url, $"Fetched {href} from Curse addon site.");

                            // This is the central logic part of the "prevent the 5 sec JS timer" concept.
                            // By disabling JS, fetching the 'href' manually and loading that url manually.

                            localWebView.Stop();
                            localWebView.Source = new Uri(href);
                        }
                    }
                    else if (!e.IsSuccess && e.HttpStatusCode == 0 && e.WebErrorStatus == CoreWebView2WebErrorStatus.ConnectionAborted)
                    {
                        var message = "Redirects and Cloudflare processing finished.";
                        debugWriter.PrintInfo($"{message} --> Download should start now ...");
                        OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.CursePreludeProgress, url, message);
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
            debugWriter.PrintEventHeader();
            debugWriter.PrintEventDownloadStarting(e);

            debugWriter.PrintInfo("Register event handler --> DownloadOperation.BytesReceivedChanged");
            debugWriter.PrintInfo("Register event handler --> DownloadOperation.StateChanged");

            e.DownloadOperation.BytesReceivedChanged += DownloadOperation_BytesReceivedChanged;
            e.DownloadOperation.StateChanged += DownloadOperation_StateChanged;

            var url = e.DownloadOperation.Uri;

            OnDownloadAddonsAsyncProgressChanged(
                WebViewHelperProgressState.DownloadStarting,
                url,
                curseHelper.GetFileNameFromDownloadUrl(url),
                curseHelper.GetAddonNameFromDownloadUrl(url),
                0,
                e.DownloadOperation.TotalBytesToReceive ?? 0);

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
                    debugWriter.PrintEventHeader();
                    debugWriter.PrintEventReceivedChanged(downloadOperation);
                    debugWriter.PrintInfo($"Received {downloadOperation.BytesReceived} / {downloadOperation.TotalBytesToReceive} bytes.");

                    var url = downloadOperation.Uri;

                    OnDownloadAddonsAsyncProgressChanged(
                        WebViewHelperProgressState.DownloadProgress,
                        url,
                        curseHelper.GetFileNameFromDownloadUrl(url),
                        curseHelper.GetAddonNameFromDownloadUrl(url),
                        (ulong)downloadOperation.BytesReceived,
                        downloadOperation.TotalBytesToReceive ?? 0,
                        666); // Todo ???

                    // Doing this inside above if clause, allows small file downloads to finish.

                    if (cancellationRequested)
                    {
                        downloadOperation.Cancel();
                    }
                }
            }
        }

        private void DownloadOperation_StateChanged(object? sender, object e)
        {
            debugWriter.PrintEventHeader();

            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                debugWriter.PrintEventStateChanged(downloadOperation);

                if (downloadOperation.State != CoreWebView2DownloadState.Completed && downloadOperation.State != CoreWebView2DownloadState.Interrupted)
                {
                    // Todo ???

                    return;
                }
                var url = downloadOperation.Uri;
                var file = Path.GetFileName(downloadOperation.ResultFilePath);

                if (downloadOperation.State == CoreWebView2DownloadState.Completed)
                {
                    finishedCounter++;
                    var percent = (double)(100 / addonCount) * finishedCounter;

                    OnDownloadAddonsAsyncProgressChanged(
                        WebViewHelperProgressState.AddonFinished,
                        url,
                        file,
                        curseHelper.GetAddonNameFromDownloadUrl(url),
                        (ulong)downloadOperation.BytesReceived,
                        downloadOperation.TotalBytesToReceive ?? 0,
                        (uint)percent);
                }

                debugWriter.PrintInfo("Unregister event handler --> DownloadOperation.BytesReceivedChanged");
                debugWriter.PrintInfo("Unregister event handler --> DownloadOperation.StateChanged");

                downloadOperation.BytesReceivedChanged -= DownloadOperation_BytesReceivedChanged;
                downloadOperation.StateChanged -= DownloadOperation_StateChanged;

                if (webView == null) return; // Enforced by NRT

                if (!addonUrls.Any() || cancellationRequested)
                {
                    // No more addons in queue to download, or cancellation happened.

                    debugWriter.PrintInfo(cancellationRequested ?
                        $"Queue of urls is not empty yet, but CANCELLATION was detected. --> STOP AND NOT PROCEED WITH NEXT URL!" :
                        "Queue of urls is empty, so there is nothing else to download. --> ALL DOWNLOADS FINISHED SUCCESSFULLY!");

                    webView.NavigationStarting -= WebView_NavigationStarting;
                    webView.NavigationCompleted -= WebView_NavigationCompleted;
                    webView.CoreWebView2.DownloadStarting -= CoreWebView2_DownloadStarting;

                    OnDownloadAddonsAsyncCompleted(url, cancellationRequested, null, "", curseHelper.)
                    }
                else
                {
                    // Still some addons in queue to download, so proceed with next url.

                    var url = addonUrls.Dequeue();
                    debugWriter.PrintInfo($"Queue of urls is not empty yet, so proceed with next url in queue. --> {url}");
                    webView.Source = new Uri(url);
                }
            }
        }

        private void OnDownloadAddonsAsyncCompleted(
            string url,
            bool cancelled = default,
            Exception? error = default,
            string info = "",
            string addon = "",
            ulong received = default,
            ulong total = default)
        {
            DownloadAddonsAsyncCompleted?.Invoke(this, new AsyncCompletedEventArgs(
                    error,
                    cancelled,
                    new WebViewHelperProgress(WebViewHelperProgressState.Finished, url, info, addon, received, total)));
        }

        private void OnDownloadAddonsAsyncProgressChanged(
            WebViewHelperProgressState state,
            string url,
            string info = "",
            string addon = "",
            ulong received = default,
            ulong total = default,
            uint percent = default)
        {
            DownloadAddonsAsyncProgressChanged?.Invoke(this, new ProgressChangedEventArgs(
                (int)percent,
                new WebViewHelperProgress(state, url, info, addon, received, total)));
        }
    }
}
