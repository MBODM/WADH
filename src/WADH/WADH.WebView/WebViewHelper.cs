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
        private int addonCount;
        private int finishedDownloads;
        private bool cancellationRequested;
        private string actualAddonName;
        private ulong lastStartingNavigationId;

        private readonly Queue<string> addonUrls = new();
        private readonly ILogHelper logHelper;

        private readonly ICurseHelper curseHelper;
        private readonly IFileLogger fileLogger;

        public WebViewHelper(ICurseHelper curseHelper, IFileLogger fileLogger)
        {
            this.curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));
            this.fileLogger = fileLogger ?? throw new ArgumentNullException(nameof(fileLogger));

            actualAddonName = string.Empty;
            logHelper = new LogHelper(fileLogger);
        }

        public bool IsInitialized { get { return coreWebView != null; } }
        public bool IsBusy { get; private set; }

        public event AsyncCompletedEventHandler? DownloadAddonsAsyncCompleted;
        public event ProgressChangedEventHandler? DownloadAddonsAsyncProgressChanged;

        public Task<CoreWebView2Environment> CreateEnvironmentAsync()
        {
            // The WebView2 user data folder (UDF) has to have write access and the UDF´s default location is the executable´s folder.
            // Therefore some other folder (with write permissions guaranteed) has to be specified here, used as UDF for the WebView2.
            // Just using the temp folder for the UDF here, since this matches the temporary characteristics the UDF has in this case.
            // Also the application, when started or closed, does NOT try to delete that folder. On purpose! Because the UDF contains
            // some .pma files, not accessible directly after the application has closed (Microsoft Edge doing some stuff there). But
            // in my opinion this is totally fine, since it is a user´s temp folder and the UDF will be reused next time again anyway.

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
                this.addonUrls.Clear();
                addonUrls.ToList().ForEach(url => this.addonUrls.Enqueue(url));
                addonCount = addonUrls.Count();

                finishedDownloads = 0;
                cancellationRequested = false;

                coreWebView.Profile.DefaultDownloadFolderPath = Path.GetFullPath(downloadFolder);

                var url = this.addonUrls.Dequeue();

                if (coreWebView.Source.ToString() == url)
                {
                    // If the site has already been loaded then the events are not raised without this.
                    // Happens when there is only 1 URL in queue. Important i.e. for GUI button state.

                    coreWebView.Stop(); // Just to make sure
                    coreWebView.Reload();
                }
                else
                {
                    // Kick off the whole event chain process, by navigating to first URL from queue.

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

        private void NavigationStarting1(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            logHelper.LogEvent();

            if (sender is CoreWebView2 senderCoreWebView)
            {
                senderCoreWebView.NavigationStarting -= NavigationStarting1;
                logHelper.LogNavigationStarting(senderCoreWebView, e);

                if (curseHelper.IsAddonPageUrl(e.Uri) && !e.IsRedirected && e.NavigationId != lastStartingNavigationId)
                {
                    lastStartingNavigationId = e.NavigationId;

                    // senderWebView.Visible = false; // Prevent scrollbar flickering effect here
                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.NavigationToAddonPageStarting, e.Uri,
                        "Starting manual navigation to addon page URL now.", actualAddonName);

                    senderCoreWebView.DOMContentLoaded += DOMContentLoaded;
                }
                else
                {
                    NavigationStartingError(senderCoreWebView, e);
                }
            }
        }

        private async void DOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            logHelper.LogEvent();

            if (sender is CoreWebView2 senderCoreWebView)
            {
                senderCoreWebView.DOMContentLoaded -= DOMContentLoaded;
                logHelper.LogDOMContentLoaded(senderCoreWebView, e);

                logHelper.LogBeforeScriptExecution("disable scrollbar");
                await senderCoreWebView.ExecuteScriptAsync(curseHelper.DisableScrollbarScript);
                logHelper.LogAfterScriptExecution();

                logHelper.LogBeforeScriptExecution("hide cookiebar on load");
                await senderCoreWebView.ExecuteScriptAsync(curseHelper.HideCookiebarScript);
                logHelper.LogAfterScriptExecution();

                // senderWebView.Visible = true; // Prevent scrollbar flickering effect here
                OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.NavigationToAddonPageStarting, senderCoreWebView.Source,
                    "Addon page DOM content loaded.", actualAddonName);

                senderCoreWebView.NavigationCompleted += NavigationCompleted1;
            }
        }

        private async void NavigationCompleted1(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            logHelper.LogEvent();

            if (sender is CoreWebView2 senderCoreWebView)
            {
                senderCoreWebView.NavigationCompleted -= NavigationCompleted1;
                logHelper.LogNavigationCompleted(senderCoreWebView, e);

                // At the time of writing this code the EventArgs e still not including the Uri.
                // Therefore using the WebView´s Source property value as some workaround here.
                // This should be no problem since the navigations are not running concurrently.
                // Have a look at https://github.com/MicrosoftEdge/WebView2Feedback/issues/580

                var uri = senderCoreWebView.Source;

                if (curseHelper.IsAddonPageUrl(uri) && e.NavigationId == lastStartingNavigationId && e.IsSuccess && e.HttpStatusCode == 200 &&
                    e.WebErrorStatus == CoreWebView2WebErrorStatus.Unknown)
                {
                    // The following raised events are a bit "stupid" since there happens nothing in between, but state-wise it makes sense.

                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.NavigationToAddonPageFinished, uri,
                        "Manual navigation to addon page finished.", actualAddonName);

                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.EvaluationOfAddonPageJsonStarting, uri,
                        "Starting evaluation of addon page JSON now.", actualAddonName);

                    logHelper.LogBeforeScriptExecution("grab JSON");
                    var json = await senderCoreWebView.ExecuteScriptAsync(curseHelper.GrabJsonScript);
                    logHelper.LogAfterScriptExecution();

                    var (jsonIsValid, fetchedDownloadUrl, projectName) = ProcessJson(json);
                    if (jsonIsValid)
                    {
                        fileLogger.Log("Addon page JSON is valid.");

                        actualAddonName = projectName;

                        OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.EvaluationOfAddonPageJsonFinished, uri,
                            "Evaluation of addon page JSON successfully finished.", actualAddonName);

                        senderCoreWebView.Stop(); // Just to make sure
                        senderCoreWebView.NavigationStarting += NavigationStarting2;
                        senderCoreWebView.Navigate(fetchedDownloadUrl);

                        return;
                    }
                }

                NavigationCompletedError(senderCoreWebView, e);
            }
        }

        private void NavigationStarting2(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            logHelper.LogEvent();

            if (sender is CoreWebView2 senderCoreWebView)
            {
                senderCoreWebView.NavigationStarting -= NavigationStarting2;
                logHelper.LogNavigationStarting(senderCoreWebView, e);

                if (curseHelper.IsFetchedDownloadUrl(e.Uri) && !e.IsRedirected && e.NavigationId != lastStartingNavigationId)
                {
                    lastStartingNavigationId = e.NavigationId;
                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.NavigationToFetchedDownloadUrlStarting, e.Uri,
                        "Starting manual navigation to fetched download URL now.", actualAddonName);
                }
                else if (curseHelper.IsRedirectWithApiKeyUrl(e.Uri) && e.IsRedirected && e.NavigationId == lastStartingNavigationId)
                {
                    lastStartingNavigationId = e.NavigationId;
                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.RedirectWithApiKeyStarting, e.Uri,
                        "Starting automatic navigation by redirect now.", actualAddonName);
                }
                else if (curseHelper.IsRealDownloadUrl(e.Uri) && e.IsRedirected && e.NavigationId == lastStartingNavigationId)
                {
                    lastStartingNavigationId = e.NavigationId;
                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.RedirectToRealDownloadUrlStarting, e.Uri,
                        "Starting automatic navigation by redirect now.", actualAddonName);
                }
                else
                {
                    NavigationStartingError(senderCoreWebView, e);
                }
            }
        }

        private void NavigationCompleted2(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            logHelper.LogEvent();

            if (sender is CoreWebView2 senderCoreWebView)
            {
                senderCoreWebView.NavigationCompleted -= NavigationCompleted2;
                logHelper.LogNavigationCompleted(senderCoreWebView, e);

                // At the time of writing this code the EventArgs e still not including the Uri.
                // Therefore using the WebView´s Source property value as some workaround here.
                // This should be no problem since the navigations are not running concurrently.
                // Have a look at https://github.com/MicrosoftEdge/WebView2Feedback/issues/580

                var uri = senderCoreWebView.Source;

                // Note: Redirects do not raise this event (in contrast to the starting event).
                // Therefore only the initial URL and the last redirect will raise this event.
                // Also note: WebView2 does not change its Source property value, on redirects.
                // And since there exists no easy way to get the actual redirect location URL,
                // it is not possible to get the URL for the last/2nd occurrence of this event.

                if (curseHelper.IsFetchedDownloadUrl(uri) && e.NavigationId == lastStartingNavigationId && !e.IsSuccess && e.HttpStatusCode == 0 &&
                    e.WebErrorStatus == CoreWebView2WebErrorStatus.ConnectionAborted)
                {
                    // The following raised events are a bit "stupid" since there happens nothing in between, but state-wise it makes sense.

                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.NavigationToFetchedDownloadUrlFinished,
                        "There is no easy way to show the real (redirected) URL here.",
                        "Manual navigation to fetched download URL finished (including all automatic redirects).",
                        actualAddonName);

                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.NavigationAndRedirectsFinished,
                        "There is no easy way to show the real (redirected) URL here.",
                        "All navigations and redirects finished and download should start now.",
                        actualAddonName);

                    senderCoreWebView.DownloadStarting += DownloadStarting;
                }
                else
                {
                    NavigationCompletedError(senderCoreWebView, e);
                }
            }
        }

        private void DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            logHelper.LogEvent();

            if (sender is CoreWebView2 senderCoreWebView)
            {
                senderCoreWebView.DownloadStarting -= DownloadStarting;
                logHelper.LogDownloadStarting(senderCoreWebView, e);

                OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.DownloadStarting, e.DownloadOperation.Uri,
                    "Starting file download.", actualAddonName, Path.GetFileName(e.ResultFilePath), 0, e.DownloadOperation.TotalBytesToReceive ?? 0);

                e.DownloadOperation.BytesReceivedChanged += BytesReceivedChanged;
                e.DownloadOperation.StateChanged += StateChanged;

                e.Handled = true; // Do not show Microsoft Edge´s default download dialog
            }
        }

        private void BytesReceivedChanged(object? sender, object e)
        {
            if (sender is CoreWebView2DownloadOperation senderDownloadOperation)
            {
                var received = (ulong)senderDownloadOperation.BytesReceived;
                var total = senderDownloadOperation.TotalBytesToReceive ?? 0;

                // Only show real chunks and not just the final chunk, when there is only one.
                // This happens sometimes for mid-sized files. The very small ones create no
                // event at all. The very big ones create a bunch of events. But for all the
                // mid-sized files there is only 1 event with i.e. 12345/12345 byte progress.
                // Therefore it seems OK to ignore them, for better readability of log output.

                if (received < total)
                {
                    logHelper.LogEvent();
                    logHelper.LogBytesReceivedChanged(senderDownloadOperation, e);
                    fileLogger.Log($"Received {received} of {total} bytes.");

                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.DownloadProgress, senderDownloadOperation.Uri,
                        "Downloading file...", actualAddonName, Path.GetFileName(senderDownloadOperation.ResultFilePath), received, total);

                    // Doing this inside above if clause, allows small file downloads to finish.

                    if (cancellationRequested)
                    {
                        senderDownloadOperation.Cancel();
                    }
                }
            }
        }

        private void StateChanged(object? sender, object e)
        {
            logHelper.LogEvent();

            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                logHelper.LogStateChanged(downloadOperation, e);

                if (downloadOperation.State == CoreWebView2DownloadState.InProgress)
                {
                    fileLogger.Log("Warning: CoreWebView2DownloadState is 'InProgress' and usually this not happens! Anyway, download will continue.");
                    return;
                }

                downloadOperation.BytesReceivedChanged -= BytesReceivedChanged;
                downloadOperation.StateChanged -= StateChanged;

                // Next block is specific to 'Completed' state (and not 'Interrupted' state):

                if (downloadOperation.State == CoreWebView2DownloadState.Completed)
                {
                    var file = Path.GetFileName(downloadOperation.ResultFilePath);
                    var received = (ulong)downloadOperation.BytesReceived;
                    var total = downloadOperation.TotalBytesToReceive ?? 0;

                    // The following raised events are a bit "stupid" since there happens nothing in between, but state-wise it makes sense.

                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.DownloadFinished, downloadOperation.Uri,
                        "Finished file download.", actualAddonName, file, received, total);

                    finishedDownloads++;

                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.AddonFinished, downloadOperation.Uri,
                        $"Finished processing of addon ({finishedDownloads}/{addonCount}).", actualAddonName, file, received, total);
                }

                // Next block is same for 'Completed' and 'Interrupted' state:

                if (!addonUrls.Any() || cancellationRequested)
                {
                    // No more addons in queue to download or cancellation occurred, so finish the process.

                    fileLogger.Log(cancellationRequested ?
                        "URL-Queue is not empty yet, but cancellation occurred. --> Stop and not proceed with next URL" :
                        "URL-Queue is empty, there is nothing else to download. --> All addons successfully downloaded");

                    OnDownloadAddonsAsyncCompleted(cancellationRequested);
                }
                else
                {
                    // Still some addons to download in queue and no cancellation occurred, so proceed with next URL.

                    var next = addonUrls.Dequeue();
                    fileLogger.Log($"URL-Queue is not empty yet, so proceed with next URL in queue. --> {next}");
                    StartAddonProcessing(next);
                }
            }
        }

        private void StartAddonProcessing(string url)
        {
            actualAddonName = curseHelper.GetAddonSlugNameFromAddonPageUrl(url);

            OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.AddonStarting, url,
                $"Starting processing of addon ({finishedDownloads + 1}/{addonCount}).", actualAddonName);

            if (coreWebView == null) return; // Enforced by NRT

            coreWebView.Stop(); // Just to make sure
            coreWebView.NavigationStarting += NavigationStarting1;
            coreWebView.Navigate(url);
        }

        private void NavigationStartingError(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs e)
        {
            sender.Stop(); // Just to make sure
            e.Cancel = true;
            logHelper.LogNavigationStartingError();
            OnDownloadAddonsAsyncCompleted(false, "Navigation cancelled, cause of unexpected Curse behaviour (see log file for details).");
        }

        private void NavigationCompletedError(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            sender.Stop();
            logHelper.LogNavigationCompletedError();
            OnDownloadAddonsAsyncCompleted(false, "Navigation stopped, cause of unexpected Curse behaviour (see log file for details).");
        }

        private (bool jsonIsValid, string fetchedDownloadUrl, string projectName) ProcessJson(string json)
        {
            json = json.Trim().Trim('"').Trim();
            if (json == "null")
            {
                fileLogger.Log("Script (to grab JSON) returned 'null' as string.");
                return (false, string.Empty, string.Empty);
            }

            json = Regex.Unescape(json);
            var model = curseHelper.SerializeAddonPageJson(json);
            if (!model.IsValid)
            {
                fileLogger.Log("Serialization of JSON string (returned by script) failed.");
                return (false, string.Empty, string.Empty);
            }

            var fetchedDownloadUrl = curseHelper.BuildFetchedDownloadUrl(model.ProjectId, model.FileId);
            if (!curseHelper.IsFetchedDownloadUrl(fetchedDownloadUrl))
            {
                fileLogger.Log("Download URL (fetched from JSON) is not valid.");
                return (false, string.Empty, string.Empty);
            }

            return (true, fetchedDownloadUrl, model.ProjectName);
        }

        private void OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState state, string url, string info, string addon,
            string file = "", ulong received = 0, ulong total = 0)
        {
            var e = new WebViewHelperProgressChangedEventArgs(CalcPercent(), null,
                new WebViewHelperProgressData(state, url, info, addon, file, received, total));

            DownloadAddonsAsyncProgressChanged?.Invoke(this, e);
        }

        private void OnDownloadAddonsAsyncCompleted(bool cancelled = default, string error = "")
        {
            IsBusy = false;

            DownloadAddonsAsyncCompleted?.Invoke(this, new AsyncCompletedEventArgs(
                error != "" ? new InvalidOperationException(error) : null, cancelled, $"Completed {finishedDownloads}/{addonCount} addons."));
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
