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
        private ulong addonPageUrlNavigationId;
        private ulong fetchedDownloadUrlNavigationId;
        private string actualAddonName;
        private string fetchedDownloadUrl;

        private readonly IDebugWriter debugWriter;
        private readonly ILogHelper logHelper;
        private readonly Queue<string> addonUrls;

        private readonly ICurseHelper curseHelper;
        private readonly IFileLogger fileLogger;

        public WebViewHelper(ICurseHelper curseHelper, IFileLogger fileLogger)
        {
            this.curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));
            this.fileLogger = fileLogger ?? throw new ArgumentNullException(nameof(fileLogger));

            debugWriter = new DebugWriter();
            logHelper = new LogHelper(fileLogger, debugWriter);
            addonUrls = new();

            actualAddonName = string.Empty;
            fetchedDownloadUrl = string.Empty;
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
                addonPageUrlNavigationId = 0;
                fetchedDownloadUrlNavigationId = 0;
                fetchedDownloadUrl = string.Empty;
                actualAddonName = string.Empty;

                coreWebView.Profile.DefaultDownloadFolderPath = Path.GetFullPath(downloadFolder);

                AddHandlers();
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

        private void NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            logHelper.LogEvent();

            if (sender is CoreWebView2 senderCoreWebView)
            {
                logHelper.LogNavigationStarting(senderCoreWebView, e);

                if (curseHelper.IsAddonPageUrl(e.Uri) && !e.IsRedirected)
                {
                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.AddonStarted, actualAddonName,
                        "Starting manual navigation to addon page URL");

                    // Prevent flickering effects
                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.ShouldHideWebView, actualAddonName,
                        "Please hide the WebView2 component now");

                    addonPageUrlNavigationId = e.NavigationId;
                    debugWriter.Success($"starting with '{e.Uri}'");
                }
                else if (curseHelper.IsFetchedDownloadUrl(e.Uri) && !e.IsRedirected)
                {
                    fetchedDownloadUrlNavigationId = e.NavigationId;
                    debugWriter.Success($"starting with '{e.Uri}'");
                }
                else if (curseHelper.IsRedirectWithApiKeyUrl(e.Uri) && e.IsRedirected && e.NavigationId == fetchedDownloadUrlNavigationId)
                {
                    debugWriter.Success($"redirecting to '{e.Uri}'");
                }
                else if (curseHelper.IsRealDownloadUrl(e.Uri) && e.IsRedirected && e.NavigationId == fetchedDownloadUrlNavigationId)
                {
                    debugWriter.Success($"redirecting to '{e.Uri}'");
                }
                else
                {
                    senderCoreWebView.Stop(); // Just to make sure (maybe not necessary with e.Cancel = true)
                    e.Cancel = true;
                    RemoveHandlers();
                    debugWriter.Error();
                    logHelper.LogNavigationStartingError();
                    OnDownloadAddonsAsyncCompleted(false, "Navigation cancelled, cause of unexpected Curse behaviour (see log file for details).");
                }
            }
        }

        private async void DOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            logHelper.LogEvent();

            if (sender is CoreWebView2 senderCoreWebView)
            {
                logHelper.LogDOMContentLoaded(senderCoreWebView, e);

                if (e.NavigationId == addonPageUrlNavigationId)
                {
                    logHelper.LogBeforeScriptExecution("disable scrollbar");
                    await senderCoreWebView.ExecuteScriptAsync(curseHelper.DisableScrollbarScript);
                    logHelper.LogAfterScriptExecution();

                    logHelper.LogBeforeScriptExecution("hide cookiebar on load");
                    await senderCoreWebView.ExecuteScriptAsync(curseHelper.HideCookiebarScript);
                    logHelper.LogAfterScriptExecution();

                    // Prevent flickering effects
                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.ShouldShowWebView, actualAddonName,
                        "Please show the WebView2 component now");

                    debugWriter.Success();
                }
            }
        }

        private async void NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            logHelper.LogEvent();

            if (sender is CoreWebView2 senderCoreWebView)
            {
                logHelper.LogNavigationCompleted(senderCoreWebView, e);

                // At the time of writing this code, the EventArgs e still not including the Uri.
                // Therefore relying on NavigationId here, and caching the fetched download URL.
                // This should be no problem, since the navigations are not running concurrently.
                // Have a look at https://github.com/MicrosoftEdge/WebView2Feedback/issues/580

                // I also tried using the CoreWebView2.Source property, instead of caching the
                // fetched download URL in class. But this also failed, cause of another issue.
                // Have a look at https://github.com/MicrosoftEdge/WebView2Feedback/issues/3461

                if (e.NavigationId == addonPageUrlNavigationId && e.IsSuccess && e.HttpStatusCode == 200)
                {
                    logHelper.LogBeforeScriptExecution("grab JSON");
                    var json = await senderCoreWebView.ExecuteScriptAsync(curseHelper.GrabJsonScript);
                    logHelper.LogAfterScriptExecution();

                    var (isValid, downloadUrl, projectName) = ProcessJson(json);
                    if (isValid)
                    {
                        debugWriter.Text("JSON validated");
                        fileLogger.Log("Addon page JSON is valid.");

                        actualAddonName = projectName;
                        debugWriter.Text($"Addon name is '{actualAddonName}'");

                        fetchedDownloadUrl = downloadUrl;
                        debugWriter.Text($"Fetched download URL is '{fetchedDownloadUrl}'");

                        OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.JsonEvaluationFinished, actualAddonName,
                            "Evaluation of addon page JSON successfully finished");

                        senderCoreWebView.Stop(); // Just to make sure
                        debugWriter.Success($"next line calls Navigate() with fetched download URL");
                        senderCoreWebView.Navigate(fetchedDownloadUrl);

                        return;
                    }
                }

                // Note: Redirects do not raise this event (in contrast to the Starting event).
                // Therefore only the addon page and the final redirect will raise this event.
                // Also note: WebView2 does not change its Source property value, on redirects.

                if (e.NavigationId == fetchedDownloadUrlNavigationId)
                {
                    debugWriter.Success("redirects finished and download should automatically start now");

                    return;
                }

                // Todo: Sometimes the error shows up. Why ?

                debugWriter.Error();

                //senderCoreWebView.Stop();
                //RemoveHandlers();
                //debugWriter.Error();
                //logHelper.LogNavigationCompletedError();
                //OnDownloadAddonsAsyncCompleted(false, "Navigation stopped, cause of unexpected Curse behaviour (see log file for details).");
            }
        }

        private void DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            logHelper.LogEvent();

            e.DownloadOperation.StateChanged += StateChanged;
            debugWriter.HandlerAdded("StateChanged");
            e.DownloadOperation.BytesReceivedChanged += BytesReceivedChanged;
            debugWriter.HandlerAdded("BytesReceivedChanged");

            debugWriter.Success();

            e.Handled = true; // Do not show Microsoft Edge´s default download dialog
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

                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.DownloadProgress, actualAddonName,
                        "Downloading file...", Path.GetFileName(senderDownloadOperation.ResultFilePath), received, total);

                    debugWriter.Success($"{received} / {total} bytes");

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

                if (downloadOperation.State == CoreWebView2DownloadState.Completed)
                {
                    finishedDownloads++;

                    OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState.AddonFinished, actualAddonName,
                        $"Finished processing of addon ({finishedDownloads}/{addonCount})",
                        Path.GetFileName(downloadOperation.ResultFilePath),
                        (ulong)downloadOperation.BytesReceived,
                        downloadOperation.TotalBytesToReceive ?? 0);
                }

                if (downloadOperation.State == CoreWebView2DownloadState.Completed || downloadOperation.State == CoreWebView2DownloadState.Interrupted)
                {
                    downloadOperation.StateChanged -= StateChanged;
                    debugWriter.HandlerRemoved(nameof(StateChanged));
                    downloadOperation.BytesReceivedChanged -= BytesReceivedChanged;
                    debugWriter.HandlerRemoved(nameof(BytesReceivedChanged));

                    if (!addonUrls.Any() || cancellationRequested)
                    {
                        // No more addons in queue to download or cancellation occurred, so finish the process.

                        debugWriter.Success(cancellationRequested ? "canceled by user" : "all addons finished");
                        fileLogger.Log(cancellationRequested ?
                            "URL-Queue is not empty yet, but cancellation occurred. --> Stop and not proceed with next URL" :
                            "URL-Queue is empty, there is nothing else to download. --> All addons successfully downloaded");

                        OnDownloadAddonsAsyncCompleted(cancellationRequested);
                    }
                    else
                    {
                        // Still some addons to download in queue and no cancellation occurred, so proceed with next URL.

                        debugWriter.Success("but queue is not empty yet");
                        var next = addonUrls.Dequeue();
                        fileLogger.Log($"URL-Queue is not empty yet, so proceed with next URL in queue. --> {next}");

                        StartAddonProcessing(next);
                    }
                }
            }
        }

        private void StartAddonProcessing(string url)
        {
            debugWriter.Start();

            actualAddonName = curseHelper.GetAddonSlugNameFromAddonPageUrl(url);

            if (coreWebView == null) // Enforced by NRT
            {
                throw new InvalidOperationException(NotInitializedError);
            }

            coreWebView.Stop(); // Just to make sure
            debugWriter.Text($"Curse addon page URL is '{url}'", false);
            debugWriter.Text($"Next line calls Navigate() with Curse addon page URL", false);
            coreWebView.Navigate(url);
        }

        private void AddHandlers()
        {
            if (coreWebView == null) // Enforced by NRT
            {
                throw new InvalidOperationException(NotInitializedError);
            }

            coreWebView.NavigationStarting += NavigationStarting;
            coreWebView.DOMContentLoaded += DOMContentLoaded;
            coreWebView.NavigationCompleted += NavigationCompleted;
            coreWebView.DownloadStarting += DownloadStarting;
        }

        private void RemoveHandlers()
        {
            if (coreWebView == null) // Enforced by NRT
            {
                throw new InvalidOperationException(NotInitializedError);
            }

            coreWebView.DownloadStarting -= DownloadStarting;
            coreWebView.NavigationCompleted -= NavigationCompleted;
            coreWebView.DOMContentLoaded -= DOMContentLoaded;
            coreWebView.NavigationStarting -= NavigationStarting;
        }

        private (bool isValid, string downloadUrl, string projectName) ProcessJson(string json)
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

            var downloadUrl = curseHelper.BuildFetchedDownloadUrl(model.ProjectId, model.FileId);
            if (!curseHelper.IsFetchedDownloadUrl(downloadUrl))
            {
                fileLogger.Log("Download URL (fetched from JSON) is not valid.");
                return (false, string.Empty, string.Empty);
            }

            return (true, downloadUrl, model.ProjectName);
        }

        private void OnDownloadAddonsAsyncProgressChanged(WebViewHelperProgressState state, string addon, string message,
            string file = "", ulong received = 0, ulong total = 0)
        {
            DownloadAddonsAsyncProgressChanged?.Invoke(this, new WebViewHelperProgressChangedEventArgs(
                CalcPercent(),
                null,
                new WebViewHelperProgressData(state, addon, message, file, received, total)));
        }

        private void OnDownloadAddonsAsyncCompleted(bool cancelled = false, string error = "")
        {
            IsBusy = false;

            DownloadAddonsAsyncCompleted?.Invoke(this, new AsyncCompletedEventArgs(
                error != string.Empty ? new InvalidOperationException(error) : null,
                cancelled,
                $"Finished {finishedDownloads}/{addonCount} addons"));
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
