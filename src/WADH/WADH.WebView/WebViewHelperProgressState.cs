namespace WADH.WebView
{
    public enum WebViewHelperProgressState
    {
        AddonStarting,
        NavigationToAddonPageStarting,
        NavigationToAddonPageFinished,
        EvaluationOfAddonPageJsonStarting,
        EvaluationOfAddonPageJsonFinished,
        NavigationToFetchedDownloadUrlStarting,
        RedirectWithApiKeyStarting,
        RedirectToRealDownloadUrlStarting,
        NavigationToFetchedDownloadUrlFinished,
        NavigationAndRedirectsFinished,
        DownloadStarting,
        DownloadProgress,
        DownloadFinished,
        AddonFinished,
    }
}
