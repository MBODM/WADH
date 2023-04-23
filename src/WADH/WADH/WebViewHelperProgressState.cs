namespace WADH
{
    public enum WebViewHelperProgressState
    {
        AddonStarting,
        NavigatingToAddonPage,
        NavigatingToAddonPageFinished,
        NavigatingToFetchedDownloadUrl,
        RedirectWithApiKey,
        RedirectToRealDownloadUrl,
        RedirectsFinished,
        DownloadStarting,
        DownloadProgress,
        DownloadFinished,
        AddonFinished,
    }
}
