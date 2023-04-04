using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using WADH.Core;

namespace WADH
{
    public sealed class WebViewHelper : IWebViewHelper
    {
        private WebView2? webView = null;
        private bool isInitialized = false;
        private Queue<string> urls = null;

        private readonly ICurseHelper curseHelper;

        public WebViewHelper(ICurseHelper curseHelper)
        {
            this.curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));
        }

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

            await webView.EnsureCoreWebView2Async(env);

            webView.CoreWebView2.Settings.IsScriptEnabled = false;

            //webView.NavigationStarting += WebView_NavigationStarting;
            //webView.NavigationCompleted += WebView_NavigationCompleted;
            //webView.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;

            isInitialized = true;
        }

        //private TaskCompletionSource taskCompletionSource;
        //private IProgress<string> progress;
        //private CancellationToken cancellationToken;




        public async Task DownloadAddonsAsync(
            IEnumerable<string> addonUrls,
            string downloadFolder,
            IProgress<string> progress,
            CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource();

            // WebView2.NavigationStarting

            EventHandler<CoreWebView2NavigationStartingEventArgs> navigationStartingHandler = (sender, e) =>
            {
                NavigationStartingDebugPrint(e);
                e.Cancel = NavigationStartingCancel(e.Uri, e.IsRedirected);
            };

            // WebView2.NavigationCompleted

            EventHandler<CoreWebView2NavigationCompletedEventArgs> navigationCompletedHandler = (sender, e) =>
            {
            };

            // WebView2.CoreWebView2.DownloadStarting.DownloadOperation.StateChanged

            EventHandler<object> downloadOperationStateChangedHandler = (sender, e) =>
            {
                if (sender is CoreWebView2DownloadOperation downloadOperation)
                {
                    DownloadOperationStateChangedDebugPrint(e);

                    if (downloadOperation.State == CoreWebView2DownloadState.Completed)
                    {
                        downloadOperation.StateChanged -= downloadOperationStateChangedHandler;

                        if (urls.TryDequeue(out string url))
                        {
                            webView.Source = new Uri(url); // Start with next url
                        }
                        else
                        {
                            tcs.SetResult();
                        }
                    }
                }
            };

            // WebView2.CoreWebView2.DownloadStarting

            EventHandler<CoreWebView2DownloadStartingEventArgs> downloadStartingHandler = (sender, e) =>
            {
                e.DownloadOperation.StateChanged += downloadOperationStateChangedHandler;
                DownloadStartingDebugPrint(e);
                e.Handled = true; // Do not show default download dialog
            };

            try
            {
                webView.NavigationStarting += navigationStartingHandler;
                webView.NavigationCompleted += navigationCompletedHandler;
                webView.CoreWebView2.DownloadStarting += downloadStartingHandler;
                
                var url = addonUrls.First();

                if (webView.Source.ToString() == url)
                {
                    webView.Reload();
                }
                else
                {
                    webView.Source = new Uri(addonUrls.First());
                }

                cancellationToken.Register(() => tcs.SetCanceled());

                await tcs.Task;
            }
            finally
            {
                webView.NavigationStarting -= handlerNavigationStarting;
                webView.NavigationCompleted -= handlerNavigationCompleted;
                webView.CoreWebView2.DownloadStarting -= handlerDownloadStarting;
            }
        }

        private void NavigationStartingDebugPrint(CoreWebView2NavigationStartingEventArgs e)
        {
            DebugPrintHeader();

            DebugPrintLine("e.Uri", e.Uri);
            DebugPrintLine("e.NavigationId", e.NavigationId);
            DebugPrintLine("e.Cancel", e.Cancel);
            DebugPrintLine("e.IsRedirected", e.IsRedirected);
        }

        private bool NavigationStartingCancel(string url, bool redirect)
        {
            var cancel = true;

            if (curseHelper.IsAddonUrl(url) && !redirect) cancel = false;
            // JS is disabled (cause of the Curse 5 sec timer), to navigate to the parsed href manually.
            // Therefore the 1st request, after the Curse addon site url, is not a redirect any longer.
            if (curseHelper.IsRedirect1Url(url) && !redirect) cancel = false;
            if (curseHelper.IsRedirect2Url && redirect) cancel = false;
            if (curseHelper.IsRealDownloadUrl(url) && redirect) cancel = false;

            if (cancel)
            {
                DebugPrintLine("Error: The url is either not a valid Curse url or does not match the expected redirect state.");
            }
            
            DebugPrintLine(cancel ? "Cancel navigation now, for above reasons." : "Proceed with navigation, since url and redirect state are valid.");
            
            return cancel;
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

                DebugPrintLine("(sender as WebView2).Source", url);
                DebugPrintLine("e.NavigationId", e.NavigationId);
                DebugPrintLine("e.IsSuccess", e.IsSuccess);
                DebugPrintLine("e.HttpStatusCode", e.HttpStatusCode);
                DebugPrintLine("e.WebErrorStatus", e.WebErrorStatus);

                if (e.IsSuccess && e.HttpStatusCode == 200 && !string.IsNullOrEmpty(url))
                {
                    if (curseHelper.IsAddonUrl(url))
                    {
                        DebugPrintLine($"Executing script to get 'href' attribute from loaded Curse site ...");

                        await localWebView.ExecuteScriptAsync(curseHelper.AdjustPageAppearanceScript());
                        var href = await localWebView.ExecuteScriptAsync(curseHelper.GrabRedirectDownloadUrlScript());

                        href = href.Trim().Trim('"');
                        DebugPrintLine($"Script returned '{href}' as string.");

                        if (curseHelper.IsRedirect1Url(href))
                        {
                            DebugPrintLine($"Returned 'href' is a valid Curse download/redirect url, so starting download process now ...");

                            localWebView.Stop();
                            localWebView.Source = new Uri(href);
                        }
                    }
                }
            }
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

                switch (downloadOperation.State)
                {
                    case CoreWebView2DownloadState.InProgress:
                        // Not using this state since downloaded bytes progression is actually not supported.
                        DebugPrintLine("Download state changed (state = InProgress).");
                        break;
                    case CoreWebView2DownloadState.Interrupted:
                        throw new InvalidOperationException("Download was interrupted.");
                    case CoreWebView2DownloadState.Completed:
                        downloadOperation.StateChanged -= DownloadOperation_StateChanged;
                        DebugPrintLine("Download completed.");
                        if (urls.TryDequeue(out string url))
                        {
                            DebugPrintLine("Queue of urls is not empty yet, so proceeding with next url in queue.");
                            DebugPrintLine($"Starting new navigation to '{url}' (Curse addon url).");
                            webView.Source = new Uri(url);
                        }
                        else
                        {
                            DebugPrintLine("Queue of urls is empty, so there is nothing else to download.");
                            DebugPrintLine("Means: Download of all addons finished.");
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Given CoreWebView2DownloadOperation.State value is not supported.");
                }
            }
        }

        public void DownloadAddons(IEnumerable<string> addonUrls, string downloadFolder)
        {
            if (addonUrls is null)
            {
                throw new ArgumentNullException(nameof(addonUrls));
            }

            if (!addonUrls.Any())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(downloadFolder))
            {
                throw new ArgumentException($"'{nameof(downloadFolder)}' cannot be null or whitespace.", nameof(downloadFolder));
            }

            if (!isInitialized || webView == null)
            {
                throw new InvalidOperationException("This instance was not initialized. Please call the initialization method first.");
            }

            webView.CoreWebView2.Profile.DefaultDownloadFolderPath = Path.GetFullPath(downloadFolder);

            urls = new Queue<string>(addonUrls);

            webView.Source = new Uri(urls.Dequeue());
        }

        public void ShowStartPage()
        {
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

        private static void DownloadStartingDebugPrint(CoreWebView2DownloadStartingEventArgs e)
        {
            DebugPrintHeader();

            DebugPrintLine("e.DownloadOperation.Uri", e.DownloadOperation.Uri);
            DebugPrintLine("e.DownloadOperation.ResultFilePath", e.DownloadOperation.ResultFilePath);
            DebugPrintLine("e.DownloadOperation.State", e.DownloadOperation.State);
            DebugPrintLine("e.DownloadOperation.TotalBytesToReceive", e.DownloadOperation.TotalBytesToReceive);
            DebugPrintLine("e.Cancel", e.Cancel);
        }

        private static void DebugPrintHeader([CallerMemberName] string caller = "")
        {
            Debug.WriteLine($"--------------------------------------------------------------------------------");
            Debug.WriteLine($"[{nameof(WebViewHelper)}.{caller}]:");
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
