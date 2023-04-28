using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using WADH.Core;

namespace WADH.WebView
{
    public sealed class WebViewHelper : IWebViewHelper
    {
        private const string NotInitializedError = "This instance was not initialized. Please call the initialization method first.";

        private CoreWebView2? coreWebView;
        private bool cancellationRequested;
        private int finishedDownloads;
        private int addonCount;
        private ulong lastNavigationId;
        private string actualAddonName = string.Empty;

        private readonly Queue<string> addonUrls = new();

        private readonly IDebugWriter debugWriter;
        private readonly ICurseHelper curseHelper;
        private readonly IErrorLogger errorLogger;

        public WebViewHelper(IDebugWriter debugWriter, ICurseHelper curseHelper, IErrorLogger errorLogger)
        {
            this.debugWriter = debugWriter ?? throw new ArgumentNullException(nameof(debugWriter));
            this.curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));
            this.errorLogger = errorLogger ?? throw new ArgumentNullException(nameof(errorLogger));
        }

        public bool IsInitialized { get { return coreWebView != null; } }
        public bool IsBusy { get; private set; }

        public event AsyncCompletedEventHandler? DownloadAddonsAsyncCompleted;
        public event ProgressChangedEventHandler? DownloadAddonsAsyncProgressChanged;

        public Task<CoreWebView2Environment> CreateEnvironmentAsync()
        {
            // The WebView2 user data folder (UDF) has to have write access and the default location is the executable´s folder.
            // Therefore some another folder (with write permissions) has to be specified here, used as the UDF for the WebView2.
            // The temp folder is used for the UDF here, since this match the temporary characteristic the UDF has in this case.
            // Also the application, when closed, do NOT try to delete the UDF temp folder, on purpose. Because the UDF contains
            // some .pma files, not accessible directly after the application has closed (Microsoft Edge doing some stuff there).
            // But in my opinion this is totally fine, since it´s the temp folder and this UDF is reused next time again anyway.

            var userDataFolder = Path.Combine(Path.GetFullPath(Path.GetTempPath()), "MBODM-WADH-WebView2-UDF");

            return CoreWebView2Environment.CreateAsync(null, userDataFolder, new CoreWebView2EnvironmentOptions());
        }

        public void Initialize(CoreWebView2 coreWebView)
        {
            if (coreWebView is null)
            {
                throw new ArgumentNullException(nameof(coreWebView));
            }

            if (IsInitialized)
            {
                return;
            }

            this.coreWebView = coreWebView;
        }

        public void ShowStartPage()
        {
            if (coreWebView == null)
            {
                throw new InvalidOperationException(NotInitializedError);
            }

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

            coreWebView.NavigateToString(html);
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

            if (coreWebView == null)
            {
                throw new InvalidOperationException(NotInitializedError);
            }

            if (IsBusy)
            {
                throw new InvalidOperationException("Download is already running.");
            }

            IsBusy = true;

            try
            {
                cancellationRequested = false;
                finishedDownloads = 0;

                this.addonUrls.Clear();
                addonUrls.ToList().ForEach(url => this.addonUrls.Enqueue(url));
                addonCount = addonUrls.Count();

                coreWebView.Profile.DefaultDownloadFolderPath = Path.GetFullPath(downloadFolder);

                coreWebView.NavigationStarting += CoreWebView_NavigationStarting;
                coreWebView.DOMContentLoaded += CoreWebView_DOMContentLoaded;
                coreWebView.NavigationCompleted += CoreWebView_NavigationCompleted;
                coreWebView.DownloadStarting += CoreWebView_DownloadStarting;

                var url = this.addonUrls.Dequeue();

                if (coreWebView.Source.ToString() == url)
                {
                    // If the site has already been loaded then the events are not raised without this.
                    // Happens when there is only 1 URL in queue. Important i.e. for the button state.

                    coreWebView.Stop(); // Just to make sure
                    coreWebView.Reload();
                }
                else
                {
                    // Kick off the whole event chain process, by loading the first URL.

                    StartAddonProcessing(url);
                }
            }
            catch
            {
                IsBusy = false;

                throw;
            }
        }

        public void CancelDownloadAddonsAsync()
        {
            if (coreWebView == null)
            {
                throw new InvalidOperationException(NotInitializedError);
            }

            cancellationRequested = true;
        }

        private void CoreWebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            debugWriter.PrintEventHeader();
            debugWriter.PrintEventNavigationStarting(e);

            if (sender is CoreWebView2 senderCoreWebView)
            {
                if (!HandleNavigationStarting(senderCoreWebView, e))
                {
                    debugWriter.PrintInfo("Unexpected Curse behaviour. --> Cancel navigation now.");
                    errorLogger.Log(new List<string> {
                            "Error in NavigationStarting event occurred, cause of unexpected Curse behaviour. EventArgs:",
                            $"e.{nameof(e.IsRedirected)} = {e.IsRedirected}",
                            $"e.{nameof(e.IsUserInitiated)} = {e.IsUserInitiated}",
                            $"e.{nameof(e.NavigationId)} = {e.NavigationId}",
                            $"e.{nameof(e.Uri)} = {e.Uri}"
                        });

                    senderCoreWebView.Stop(); // Just to make sure
                    e.Cancel = true;

                    OnDownloadAddonsAsyncCompleted(false, "Navigation cancelled, cause of unexpected Curse behaviour (see log for details).");
                }
            }
        }

        private async void CoreWebView_DOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            debugWriter.PrintEventHeader();

            if (sender is CoreWebView2 senderCoreWebView)
            {
                debugWriter.PrintEventDOMContentLoaded(e, senderCoreWebView.Source.ToString());

                debugWriter.PrintInfo("Execute script now, to disable scrollbar...");
                await senderCoreWebView.ExecuteScriptAsync(curseHelper.DisableScrollbarScript);



                // Todo: Hier gehts weiter...

                var s =
                    "console.log('start block...');" +
                    "let cookiebarscript = document.querySelector('script[src*=\"cookiebar\"]');" +
                    "if (cookiebarscript) {" +
                    "console.log('print now script and body');" +
                    "console.log(cookiebarscript);" +
                    "console.log(document.body);" +
                    "console.log('add handler now to script');" +
                    "cookiebarscript.onload = e => {" +
                    "console.log(e);" +
                    "};" +
                    //"console.log(e);" +
                    //"let cookiebar = document.querySelector('div#cookiebar');" +
                    //"if (cookiebar) cookiebar.style.visibility = 'hidden';" +
                    //"};" +
                    "console.log('print now script and body');" +
                    "console.log(cookiebarscript);" +
                    "console.log(document.body);" +
                    "}" +
                    "console.log('block done!');";
                //"cbs.().onload = function() { console.log('fuzzfuzzfuzzfuzzfuzzfuzzfuzzfuzz'); };" +
                //"console.log(cbs);" +
                //"console.log('print body and cbs now');" +
                //"console.log(document.body);" +
                //"console.log(cbs);" +
                //"document.body.removeChild(cbs);" +
                //"console.log('print body and cbs now');" +
                //"console.log(document.body);" +

                //"";
                await senderCoreWebView.ExecuteScriptAsync(s);



                debugWriter.PrintInfo("Script executed.");
            }

            // At the time of writing CoreWebView2 not offering some way to access its controller or parent view.

            if (coreWebView == null) return; // Enforced by NRT

            // Todo: Das hier als Action ?
            // coreWebView.Visible = true; // Prevent scrollbar flickering effect
        }

        private async void CoreWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            debugWriter.PrintEventHeader();

            if (sender is CoreWebView2 senderCoreWebView)
            {
                // At the time of writing this code the EventArgs e still not including the Uri.
                // Therefore using the WebView´s Source property value as some workaround here.
                // This should be no problem since the navigations are not running concurrently.
                // Have a look at https://github.com/MicrosoftEdge/WebView2Feedback/issues/580

                var url = senderCoreWebView.Source.ToString();

                if (!string.IsNullOrEmpty(url))
                {
                    debugWriter.PrintEventNavigationCompleted(e, url);

                    if (!await HandleNavigationCompletedAsync(senderCoreWebView, url, e))
                    {
                        debugWriter.PrintInfo("Unexpected Curse behaviour. --> Stop navigation now.");
                        errorLogger.Log(new List<string> {
                            "Error in NavigationCompleted event occurred, cause of unexpected Curse behaviour. EventArgs:",
                            $"e.{nameof(e.HttpStatusCode)} = {e.HttpStatusCode}",
                            $"e.{nameof(e.IsSuccess)} = {e.IsSuccess}",
                            $"e.{nameof(e.NavigationId)} = {e.NavigationId}",
                            $"e.{nameof(e.WebErrorStatus)} = {e.WebErrorStatus}"
                        });

                        senderCoreWebView.Stop();

                        OnDownloadAddonsAsyncCompleted(false, "Navigation stopped, cause of unexpected Curse behaviour (see log for details).");
                    }
                }
            }
        }

        private void CoreWebView_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            debugWriter.PrintEventHeader();
            debugWriter.PrintEventDownloadStarting(e);

            debugWriter.PrintInfo("Register event handler --> DownloadOperation.BytesReceivedChanged");
            debugWriter.PrintInfo("Register event handler --> DownloadOperation.StateChanged");

            e.DownloadOperation.BytesReceivedChanged += DownloadOperation_BytesReceivedChanged;
            e.DownloadOperation.StateChanged += DownloadOperation_StateChanged;

            var info = "Starting file download.";
            var file = Path.GetFileName(e.ResultFilePath);

            debugWriter.PrintInfo($"{info} --> {file}");
            OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.DownloadStarting, e.DownloadOperation.Uri, info, actualAddonName,
                file, 0, e.DownloadOperation.TotalBytesToReceive ?? 0);

            e.Handled = true; // Do not show Microsoft Edge´s default download dialog
        }

        private void DownloadOperation_BytesReceivedChanged(object? sender, object e)
        {
            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                var received = (ulong)downloadOperation.BytesReceived;
                var total = downloadOperation.TotalBytesToReceive ?? 0;

                // Only show real chunks and not just the final chunk, when there is only one.
                // This happens sometimes for mid-sized files. The very small ones create no
                // event at all. The very big ones create a bunch of events. But for all the
                // mid-sized files there is only 1 event with i.e. 12345/12345 byte progress.
                // Therefore it seems OK to ignore them, for better debug output readability.

                if (received < total)
                {
                    debugWriter.PrintEventHeader();
                    debugWriter.PrintEventReceivedChanged(downloadOperation);
                    debugWriter.PrintInfo($"Received {received} of {total} bytes.");

                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.DownloadProgress, downloadOperation.Uri, "Downloading file...",
                        actualAddonName, Path.GetFileName(downloadOperation.ResultFilePath), received, total);

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

                if (downloadOperation.State == CoreWebView2DownloadState.InProgress)
                {
                    debugWriter.PrintInfo("Warning: State is 'InProgress' and this usually does not happen. --> Anyway, continue with download.");

                    return;
                }

                debugWriter.PrintInfo("Unregister event handler --> DownloadOperation.BytesReceivedChanged");
                debugWriter.PrintInfo("Unregister event handler --> DownloadOperation.StateChanged");

                downloadOperation.BytesReceivedChanged -= DownloadOperation_BytesReceivedChanged;
                downloadOperation.StateChanged -= DownloadOperation_StateChanged;

                if (downloadOperation.State == CoreWebView2DownloadState.Completed)
                {
                    var url = downloadOperation.Uri;
                    var addon = actualAddonName;
                    var file = Path.GetFileName(downloadOperation.ResultFilePath);
                    var received = (ulong)downloadOperation.BytesReceived;
                    var total = downloadOperation.TotalBytesToReceive ?? 0;

                    // The following raised events are a bit "stupid" since there happens nothing in between, but state-wise it makes sense.

                    var info = "Finished file download.";
                    debugWriter.PrintInfo($"{info} --> {file}");
                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.DownloadFinished, url, info, addon, file, received, total);

                    finishedDownloads++;

                    debugWriter.PrintInfo($"Finished processing of addon #{finishedDownloads}. --> Check if there is another addon to process...");
                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.AddonFinished, url,
                        $"Finished processing of addon ({finishedDownloads}/{addonCount}).", addon, file, received, total);
                }

                if (coreWebView == null) return; // Enforced by NRT

                if (!addonUrls.Any() || cancellationRequested)
                {
                    // No more addons in queue to download or cancellation occurred, so finish the process.

                    debugWriter.PrintInfo("Unregister event handler --> NavigationStarting");
                    debugWriter.PrintInfo("Unregister event handler --> DOMContentLoaded");
                    debugWriter.PrintInfo("Unregister event handler --> NavigationCompleted");
                    debugWriter.PrintInfo("Unregister event handler --> DownloadStarting");

                    coreWebView.NavigationStarting -= CoreWebView_NavigationStarting;
                    coreWebView.DOMContentLoaded -= CoreWebView_DOMContentLoaded;
                    coreWebView.NavigationCompleted -= CoreWebView_NavigationCompleted;
                    coreWebView.DownloadStarting -= CoreWebView_DownloadStarting;

                    debugWriter.PrintInfo(cancellationRequested ?
                        "URL-Queue is not empty yet, but cancellation occurred. --> !!! Stop and not proceed with next URL !!!" :
                        "URL-Queue is empty, there is nothing else to download. --> !!! All addons successfully downloaded !!!");

                    OnDownloadAddonsAsyncCompleted(cancellationRequested);

                    return;
                }

                // Still some addons to download in queue and no cancellation occurred, so proceed with next URL.

                var next = addonUrls.Dequeue();
                debugWriter.PrintInfo($"URL-Queue is not empty yet, so proceed with next URL in queue. --> {next}");
                StartAddonProcessing(next);
            }
        }

        private void StartAddonProcessing(string url)
        {
            OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.AddonStarting, url,
                $"Start processing of addon ({finishedDownloads + 1}/{addonCount}).", curseHelper.GetAddonSlugNameFromAddonPageUrl(url));

            if (coreWebView == null) return; // Enforced by NRT

            coreWebView.Stop(); // Just to make sure
            coreWebView.Navigate(url);
        }

        private bool HandleNavigationStarting(CoreWebView2 senderCoreWebView, CoreWebView2NavigationStartingEventArgs e)
        {
            if (curseHelper.IsAddonPageUrl(e.Uri) && !e.IsRedirected && e.NavigationId != lastNavigationId)
            {
                // Todo: Das hier als Action ?
                // senderWebView.Visible = false; // Prevent scrollbar flickering effect

                debugWriter.PrintInfo("NavigationStarting #1 (addon page) successful.");
                OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.NavigationToAddonPageStarting, e.Uri,
                    "Manually navigating to addon page URL now.", curseHelper.GetAddonSlugNameFromAddonPageUrl(e.Uri));
            }
            else if (curseHelper.IsFetchedDownloadUrl(e.Uri) && !e.IsRedirected && e.NavigationId != lastNavigationId)
            {
                debugWriter.PrintInfo("NavigationStarting #2 (fetched download URL) successful.");
                OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.NavigationToFetchedDownloadUrlStarting, e.Uri,
                    "Manually navigating to fetched download URL now.", actualAddonName);
            }
            else if (curseHelper.IsRedirectWithApiKeyUrl(e.Uri) && e.IsRedirected && e.NavigationId == lastNavigationId)
            {
                debugWriter.PrintInfo("NavigationStarting #3 (redirect with API key) successful.");
                OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.RedirectWithApiKeyStarting, e.Uri,
                    "Automatically navigating by redirect now.", actualAddonName);
            }
            else if (curseHelper.IsRealDownloadUrl(e.Uri) && e.IsRedirected && e.NavigationId == lastNavigationId)
            {
                debugWriter.PrintInfo("NavigationStarting #4 (redirect to real download URL) successful.");
                OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.RedirectToRealDownloadUrlStarting, e.Uri,
                    "Automatically navigating by redirect now.", actualAddonName);
            }
            else
            {
                return false;
            }

            lastNavigationId = e.NavigationId;

            return true;
        }

        private async Task<bool> HandleNavigationCompletedAsync(CoreWebView2 senderCoreWebView, string url, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Note: Redirects do not raise this event (in contrast to the starting event).
            // Therefore only the initial URL and the last redirect will raise this event.
            // Also note: WebView2 does not change its Source property value, on redirects.
            // And since there exists no easy way to get the actual redirect location URL,
            // it is not possible to get the URL for the last/2nd occurrence of this event.

            if (curseHelper.IsAddonPageUrl(url) && e.NavigationId == lastNavigationId && e.IsSuccess && e.HttpStatusCode == 200 &&
                e.WebErrorStatus == CoreWebView2WebErrorStatus.Unknown)
            {
                var addon = curseHelper.GetAddonSlugNameFromAddonPageUrl(url);

                // The following raised events are a bit "stupid" since there happens not much in between, but state-wise it makes sense.

                debugWriter.PrintInfo("NavigationCompleted #1 (addon page) successful.");
                OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.NavigationToAddonPageFinished, url,
                    "Manual navigation to addon page finished.", addon);

                debugWriter.PrintInfo("Execute script now, to hide cookiebar...");
                await senderCoreWebView.ExecuteScriptAsync(curseHelper.HideCookiebarScript);
                debugWriter.PrintInfo("Script executed.");

                OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.EvaluationOfAddonPageJsonStarting, url,
                    "Evaluating addon page JSON now.", addon);

                debugWriter.PrintInfo("Execute script now, to grab json...");
                var json = await senderCoreWebView.ExecuteScriptAsync(curseHelper.GrabJsonScript);
                debugWriter.PrintInfo("Script executed.");

                json = json.Trim().Trim('"').Trim();
                if (json == "null")
                {
                    errorLogger.Log("Script (to grab JSON) returned 'null' as string.");

                    return false;
                }

                json = Regex.Unescape(json);
                var model = curseHelper.SerializeAddonPageJson(json);
                if (!model.IsValid)
                {
                    errorLogger.Log("Serialization of JSON string (returned by script) failed.");

                    return false;
                }

                actualAddonName = model.ProjectName;
                debugWriter.PrintInfo("Script returned valid JSON.");

                var fetchedDownloadUrl = curseHelper.BuildFetchedDownloadUrl(model.ProjectId, model.FileId);
                if (!curseHelper.IsFetchedDownloadUrl(fetchedDownloadUrl))
                {
                    errorLogger.Log("Download URL (fetched from JSON) not valid.");

                    return false;
                }

                OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.EvaluationOfAddonPageJsonFinished, url,
                    "Evaluation of addon page JSON finished.", actualAddonName);

                debugWriter.PrintInfo($"Fetched download URL from JSON. --> {fetchedDownloadUrl}");
                debugWriter.PrintInfo($"Manually navigating to that URL now.");

                // Todo: This should not happen here !

                senderCoreWebView.Stop(); // Just to make sure
                senderCoreWebView.Navigate(fetchedDownloadUrl);
            }
            else if (curseHelper.IsFetchedDownloadUrl(url) && e.NavigationId == lastNavigationId && !e.IsSuccess && e.HttpStatusCode == 0 &&
                e.WebErrorStatus == CoreWebView2WebErrorStatus.ConnectionAborted)
            {
                debugWriter.PrintInfo("NavigationCompleted #2 (redirects finished) successful. -- > Download should start now.");

                // The following raised events are a bit "stupid" since there happens nothing in between, but state-wise it makes sense.

                OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.NavigationToFetchedDownloadUrlFinished,
                    "There is no easy way to show the real (redirected) URL here.",
                    "Manual navigation to fetched download URL finished (all automatic redirects included).",
                    actualAddonName);

                OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.NavigationAndRedirectsFinished,
                    "There is no easy way to show the real (redirected) URL here.",
                    "All navigations and redirects finished and download should start now.",
                    actualAddonName);
            }
            else
            {
                return false;
            }

            return true;
        }

        private void OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState state, string url, string info = "", string addon = "",
                    string file = "", ulong received = default, ulong total = default)
        {
            var e = new WebViewHelperProgressChangedEventArgs(CalcPercent(), null,
                new WebViewHelperProgressData(state, url, info, addon, file, received, total));

            DownloadAddonsAsyncProgressChanged?.Invoke(this, e);
        }

        private void OnDownloadAddonsAsyncCompleted(bool cancelled = default, string error = "")
        {
            IsBusy = false;

            DownloadAddonsAsyncCompleted?.Invoke(
                this,
                new AsyncCompletedEventArgs(error != "" ? new InvalidOperationException(error) : null, cancelled, $"Completed {finishedDownloads}/{addonCount} addons."));
        }

        private int CalcPercent()
        {
            // Doing casts inside try/catch block, just to be sure.

            try
            {
                var exact = (double)100 / addonCount * finishedDownloads;
                var rounded = (int)Math.Round(exact);
                var percent = rounded > 100 ? 100 : rounded; // Cap it, just to be sure.

                return percent;
            }
            catch
            {
                return 0;
            }
        }
    }
}
