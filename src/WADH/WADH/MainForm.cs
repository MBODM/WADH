using System.ComponentModel;
using WADH.Core;

namespace WADH
{
    public partial class MainForm : Form
    {
        private readonly IExternalToolsHelper externalToolsHelper;
        private readonly IConfigReader configReader;
        private readonly IErrorLogger errorLogger;
        private readonly IFileSystemHelper fileSystemHelper;
        private readonly IWebViewHelper webViewHelper;

        public MainForm(
            IExternalToolsHelper externalToolsHelper,
            IConfigReader configReader,
            IErrorLogger errorLogger,
            IFileSystemHelper fileSystemHelper,
            IWebViewHelper webViewHelper)
        {
            this.externalToolsHelper = externalToolsHelper ?? throw new ArgumentNullException(nameof(externalToolsHelper));
            this.configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));
            this.errorLogger = errorLogger ?? throw new ArgumentNullException(nameof(errorLogger));
            this.fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            this.webViewHelper = webViewHelper ?? throw new ArgumentNullException(nameof(webViewHelper));

            InitializeComponent();

            Text = $"WADH {GetVersion()}";
            MinimumSize = Size;
            Size = new Size(1280 / 100 * 109, 800 / 100 * 109); // 1280x800 = 16:10 format (9% increased to fit addon page size)
            //panelWebView.Enabled = false; // Prevent user from clicking the web site
            labelWauz.Visible = false;
            Enabled = false;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            await webViewHelper.InitAsync(webView);

            var configFolder = Path.GetDirectoryName(configReader.Storage); // Seems OK to me (since the BL knows the impl type anyway)
            if (!string.IsNullOrEmpty(configFolder))
            {
                labelConfigFolder.ForeColor = new LinkLabel().LinkColor;
                labelConfigFolder.Click += (s, e) => externalToolsHelper.OpenExplorer(configFolder);
            }

            if (externalToolsHelper.CanOpenWauz())
            {
                labelWauz.ForeColor = new LinkLabel().LinkColor;
                labelWauz.Click += (s, e) => externalToolsHelper.OpenWauz();
                labelWauz.Visible = true;
            }

            webViewHelper.ShowStartPage();

            Enabled = true;
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (!LoadConfig())
            {
                Application.Exit();
            }

            labelDownloadFolder.ForeColor = new LinkLabel().LinkColor;
            labelDownloadFolder.Click += (s, e) => externalToolsHelper.OpenExplorer(configReader.DownloadFolder);

            labelStatus.Text = $"Ready to download {configReader.AddonUrls.Count()} addons";
        }

        private async void ButtonDownload_Click(object sender, EventArgs e)
        {
            if (buttonDownload.Text == "Cancel")
            {
                buttonDownload.Enabled = false; // Prevent button/logic jitter (button will become active again in EAP Completed event)
                webViewHelper.CancelDownloadAddonsAsync();

                return;
            }

            if (buttonDownload.Text == "Download")
            {
                await InitDownloadFolder(configReader.DownloadFolder);

                buttonDownload.Text = "Cancel";
                SetButtons(false); // Prevent button/logic jitter (button will become active again in EAP Progress event)

                progressBar.Minimum = 0;
                progressBar.Maximum = 100;
                progressBar.Value = progressBar.Minimum;

                webViewHelper.DownloadAddonsAsyncCompleted += WebViewHelper_DownloadAddonsAsyncCompleted;
                webViewHelper.DownloadAddonsAsyncProgressChanged += WebViewHelper_DownloadAddonsAsyncProgressChanged;

                try
                {
                    webViewHelper.DownloadAddonsAsync(configReader.AddonUrls, configReader.DownloadFolder);
                }
                catch (Exception ex)
                {
                    errorLogger.Log(ex);
                    ShowError("Error while starting download (see log for details).");

                    buttonDownload.Text = "Download";
                    SetButtons(true);
                }
            }
        }

        private void ButtonClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void WebViewHelper_DownloadAddonsAsyncProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (e is WebViewHelperProgressChangedEventArgs eventArgs && eventArgs.ProgressData is not null)
            {
                var progressData = eventArgs.ProgressData;

                switch (progressData.State)
                {
                    case WebViewHelperProgressState.AddonStarting:
                        buttonDownload.Enabled = true; // Prevent button/logic jitter (button was set inactive on click)
                        labelStatus.Text = $"Downloading \"{progressData.Addon}\"";
                        break;
                    case WebViewHelperProgressState.NavigationToAddonPageStarting:
                        // State not used at the moment
                        break;
                    case WebViewHelperProgressState.NavigationToAddonPageFinished:
                        // State not used at the moment
                        break;
                    case WebViewHelperProgressState.EvaluationOfAddonPageJsonStarting:
                        // State not used at the moment
                        break;
                    case WebViewHelperProgressState.EvaluationOfAddonPageJsonFinished:
                        labelStatus.Text = $"Downloading \"{progressData.Addon}\"";
                        break;
                    case WebViewHelperProgressState.NavigationToFetchedDownloadUrlStarting:
                        // State not used at the moment
                        break;
                    case WebViewHelperProgressState.RedirectWithApiKeyStarting:
                        // State not used at the moment
                        break;
                    case WebViewHelperProgressState.RedirectToRealDownloadUrlStarting:
                        // State not used at the moment
                        break;
                    case WebViewHelperProgressState.NavigationAndRedirectsFinished:
                        // State not used at the moment
                        break;
                    case WebViewHelperProgressState.DownloadStarting:
                        // State not used at the moment
                        break;
                    case WebViewHelperProgressState.DownloadProgress:
                        // This may not necessary here, since this event/state combination happens for large addons only.
                        // But just relying on some implementation is a bad move, so better make sure it is a large addon.
                        if (progressData.Total > 1024 * 1024) // 1024 * 1024 = 1 MB
                        {
                            labelStatus.Text = $"Downloading \"{progressData.Addon}\"";
                            var received = (double)(progressData.Received / 1024) / 1024;
                            var total = (double)(progressData.Total / 1024) / 1024;
                            labelStatus.Text += $" ({received:0.00} MB / {total:0.00} MB)".Replace(',', '.');
                        }
                        break;
                    case WebViewHelperProgressState.DownloadFinished:
                        // State not used at the moment
                        break;
                    case WebViewHelperProgressState.AddonFinished:
                        progressBar.Value = e.ProgressPercentage;
                        break;
                    default:
                        throw new InvalidOperationException("WebViewHelperProgressState value not supported.");
                }
            }
        }

        private async void WebViewHelper_DownloadAddonsAsyncCompleted(object? sender, AsyncCompletedEventArgs e)
        {
            // Even with a typical semaphore-blocking-mechanism it is impossible to prevent a Windows.Forms
            // ProgressBar control from reaching its maximum shortly after the last async progress happened.
            // The control is painted natively by the WinApi/OS itself. Therefore also no event-based tricks
            // will solve the problem. I just added a short async wait delay instead, to keep things simple.
            // This means the ProgressBar now has X ms, to be painted by Windows, to reach the maximum first.

            await Task.Delay(1250);

            if (e.Cancelled)
            {
                labelStatus.Text = "Cancelled";
            }
            else if (e.Error != null)
            {
                labelStatus.Text = $"Error: {e.Error.Message}";
                errorLogger.Log(e.Error);
            }
            else
            {
                labelStatus.Text = $"Download of {configReader.AddonUrls.Count()} addons successfully finished";
            }

            buttonDownload.Text = "Download";
            SetButtons(true);
        }

        private bool LoadConfig()
        {
            try
            {
                configReader.ReadConfig();
                configReader.ValidateConfig();

                return true;
            }
            catch (Exception ex)
            {
                errorLogger.Log(ex);
                ShowError("Error while loading config file (see log for details).");

                return false;
            }
        }

        private async Task InitDownloadFolder(string folder)
        {
            folder = Path.GetFullPath(folder);

            if (Directory.Exists(folder))
            {
                await fileSystemHelper.DeleteAllZipFilesInFolderAsync(folder);
            }
            else
            {
                Directory.CreateDirectory(folder);
            }
        }

        private void SetButtons(bool enabled)
        {
            labelWauz.Enabled = enabled;
            labelDownloadFolder.Enabled = enabled;
            labelConfigFolder.Enabled = enabled;
            buttonDownload.Enabled = enabled;
            buttonClose.Enabled = enabled;
        }

        private static string GetVersion()
        {
            // Seems to be the most simple way to get the product version (semantic versioning) for .NET5/6 onwards.
            // Application.ProductVersion.ToString() is the counterpart of the "Version" entry in the .csproj file.

            return Application.ProductVersion.ToString();
        }

        private static void ShowError(string errorMessage)
        {
            MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
