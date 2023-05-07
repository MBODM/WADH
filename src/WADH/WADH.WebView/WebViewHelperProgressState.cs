namespace WADH.WebView
{
    public enum WebViewHelperProgressState
    {
        AddonStarting,
        NavigationToAddonPageStarting,
        ContentOfAddonPageLoaded,
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
