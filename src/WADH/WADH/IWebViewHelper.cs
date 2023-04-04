using Microsoft.Web.WebView2.WinForms;

namespace WADH
{
    public interface IWebViewHelper
    {
        Task InitAsync(WebView2 webView);
        void ShowStartPage();
        void DownloadAddons(IEnumerable<string> addonUrls, string downloadFolder);
        Task DownloadAddonsAsync(
            IEnumerable<string> addonUrls,
            string downloadFolder,
            IProgress<string> progress,
            CancellationToken cancellationToken = default);
    }
}
