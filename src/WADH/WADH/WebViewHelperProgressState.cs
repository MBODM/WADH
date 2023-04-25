namespace WADH
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
        NavigationAndRedirectsFinished,
        DownloadStarting,
        DownloadProgress,
        DownloadFinished,
        AddonFinished,
    }
}
