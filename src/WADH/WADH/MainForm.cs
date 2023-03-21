using Microsoft.Web.WebView2.Core;
using WADH.Core;

namespace WADH
{
    public partial class MainForm : Form
    {
        private readonly List<string> urls = new();

        private readonly IFileSystemHelper fileSystemHelper;

        public MainForm(IFileSystemHelper fileSystemHelper)
        {
            this.fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));

            InitializeComponent();

            MinimumSize = Size;
            Text = $"WADH {GetVersion()}";

            buttonStart.Enabled = false;
            labelStatus.Enabled = false;
            progressBar.Enabled = false;

            urls.AddRange(new string[] {
                "https://www.curseforge.com/wow/addons/details/download",
                "https://www.curseforge.com/wow/addons/deadly-boss-mods/download",
                "https://www.curseforge.com/wow/addons/recount/download",
            });
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Profile.DefaultDownloadFolderPath = GetDownloadFolder();
            webView.NavigationCompleted += (s, e) =>
            {
                buttonStart.Enabled = true;
                labelStatus.Enabled = true;
                progressBar.Enabled = true;
            };

            var msg1 = "The addon download sites are loaded and rendered inside this web control, using Microsoft Edge.";
            var msg2 = "The app needs to do this, since https://www.curseforge.com is strictly protected by Cloudflare.";

            webView.NavigateToString(
                "<html>" +
                    "<body style=\"margin: 0; padding: 0; background-color: lightskyblue; font-family: Verdana; font-size: small;\">" +
                        "<div style=\"background-color: steelblue; position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); white-space: nowrap;\">" +
                            $"<div style =\"color: white; margin: 10px;\">{msg1}</div>" +
                            $"<div style =\"color: white; margin: 10px;\">{msg2}</div>" +
                        "</div>" +
                        "</body>" +
                "</html>"
            );

            webView.CoreWebView2.DownloadStarting += (s, e) => e.DownloadOperation.StateChanged += DownloadOperation_StateChanged;
        }

        private async void ButtonStart_Click(object sender, EventArgs e)
        {
            await ClearDownloadFolder();

            await webView.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.DownloadHistory);
            webView.CoreWebView2.OpenDefaultDownloadDialog();

            buttonStart.Enabled = false;
            progressBar.Minimum = 0;
            progressBar.Maximum = urls.Count;
            progressBar.Value = progressBar.Minimum;

            StartNextDownload();
        }

        private void DownloadOperation_StateChanged(object? sender, object e)
        {
            if (sender is CoreWebView2DownloadOperation downloadOperation)
            {
                if (downloadOperation.State == CoreWebView2DownloadState.Completed)
                {
                    progressBar.Value++;

                    if (progressBar.Value >= progressBar.Maximum)
                    {
                        labelStatus.Text = $"Download of {progressBar.Maximum} addons successfully finished.";
                        buttonStart.Enabled = true;

                        return;
                    }

                    StartNextDownload();
                }
            }
        }

        private void StartNextDownload()
        {
            var url = urls.ElementAt(progressBar.Value);

            labelStatus.Text = $"Downloading {url} ...";
            webView.Source = new Uri(url);
        }

        private static string GetVersion()
        {
            // Seems to be the most simple way to get the product version (semantic versioning) for .NET5/6 onwards.
            // Application.ProductVersion.ToString() is the counterpart of the "Version" entry in the .csproj file.

            return Application.ProductVersion.ToString();
        }

        private static string GetDownloadFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "My_WoW_Addons");
        }

        private Task ClearDownloadFolder()
        {
            return fileSystemHelper.DeleteAllZipFilesInFolderAsync(GetDownloadFolder());
        }
    }
}
