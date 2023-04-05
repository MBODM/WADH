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
            Size = new Size(1280, 800); // 16:10 format.
            panelWebView.Enabled = false; // Prevents user from clicking the web site.
            labelWauz.Visible = false;

            Enabled = false;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            await webViewHelper.InitAsync(webView);

            var configFolder = Path.GetDirectoryName(configReader.Storage); // Seems OK to me, since the BL knows the impl type anyway.
            if (!string.IsNullOrEmpty(configFolder))
            {
                labelConfig.ForeColor = new LinkLabel().LinkColor;
                labelConfig.Click += (s, e) => externalToolsHelper.OpenExplorer(configFolder);
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

        private async void ButtonStart_Click(object sender, EventArgs e)
        {
            if (buttonStart.Text == "Start")
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

                buttonStart.Text = "Cancel";
                buttonClose.Enabled = false;
                progressBar.Minimum = 0;
                progressBar.Maximum = configReader.AddonUrls.Count();
                progressBar.Value = progressBar.Minimum;

                //labelStatus.Text = $"Downloading {name} ...";

                //var tempForDebug = new List<string>() { configReader.AddonUrls.Where(url => url.Contains("/raiderio/")).First() };
                //tempForDebug.Clear();
                //tempForDebug.Add("attps://www.curseforge.com/wow/addons/coordinates/downloadz");

                webViewHelper.DownloadAddonsAsyncCompleted += (s, e) =>
                {
                    buttonStart.Text = "Start";

                    if (e.Cancelled)
                    {
                        MessageBox.Show("Was cancelled");
                    }
                    else if (e.Error != null)
                    {
                        MessageBox.Show("Had Error: " + e.Error.Message);
                    }
                    else
                    {
                        MessageBox.Show("Finished successfully.");
                    }
                };

                webViewHelper.DownloadAddonsAsyncProgressChanged += (s, e) =>
                {
                    progressBar.Value = e.ProgressPercentage;
                };

                webViewHelper.DownloadAddonsAsync(configReader.AddonUrls, configReader.DownloadFolder);

                // Todo: ExceptionHandling ???
            }
            else
            {
                webViewHelper.CancelDownloadAddonsAsync();
            }
        }

        private void ButtonClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
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
