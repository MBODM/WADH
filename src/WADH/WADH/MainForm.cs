using Microsoft.Web.WebView2.Core;
using WADH.Core;

namespace WADH
{
    public partial class MainForm : Form
    {
        private readonly IConfigReader configReader;
        private readonly IErrorLogger errorLogger;
        private readonly IFileSystemHelper fileSystemHelper;

        private readonly WebViewHelper webViewHelper;

        public MainForm(IConfigReader configReader, IErrorLogger errorLogger, IFileSystemHelper fileSystemHelper)
        {
            this.configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));
            this.errorLogger = errorLogger ?? throw new ArgumentNullException(nameof(errorLogger));
            this.fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));

            InitializeComponent();

            Text = $"WADH {GetVersion()}";
            MinimumSize = Size;
            Size = new Size(1280, 800); /* 16:10 */

            webViewHelper = new WebViewHelper(webView);
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            await webViewHelper.ConfigureWebView();
            webViewHelper.ShowStartPage();
            webView.CoreWebView2.DownloadStarting += (s, e) => e.DownloadOperation.StateChanged += DownloadOperation_StateChanged;
        }

        private async void ButtonStart_Click(object sender, EventArgs e)
        {
            try
            {
                configReader.ReadConfig();
            }
            catch (Exception ex)
            {
                errorLogger.Log(ex);
                ShowError("Error while loading config file (see log file for details).");

                return;
            }

            try
            {
                configReader.ValidateConfig();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);

                return;
            }

            await InitDownloadFolder(configReader.DownloadFolder);
            webViewHelper.SetDownloadFolder(configReader.DownloadFolder);
            await webViewHelper.ClearDownloadHistory();
            webViewHelper.ShowDownloadHistoryDialog();

            buttonStart.Enabled = false;
            progressBar.Minimum = 0;
            progressBar.Maximum = configReader.AddonUrls.Count();
            progressBar.Value = progressBar.Minimum;

            StartNextDownload();
        }

        private async void DownloadOperation_StateChanged(object? sender, object e)
        {
            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                if (downloadOperation.State == CoreWebView2DownloadState.Completed)
                {
                    progressBar.Value++;

                    if (progressBar.Value >= progressBar.Maximum)
                    {
                        await Task.Delay(1500);

                        labelStatus.Text = $"Download of {progressBar.Maximum} addons successfully finished.";
                        buttonStart.Enabled = true;

                        return;
                    }

                    StartNextDownload();
                }
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

        private void StartNextDownload()
        {
            var url = configReader.AddonUrls.ElementAt(progressBar.Value);
            var name = url.Split("https://www.curseforge.com/wow/addons/").Last().Split("/download").First();

            labelStatus.Text = $"Downloading {name} ...";
            webViewHelper.NavigateToUrl(url);
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
