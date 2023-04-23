namespace WADH.Core
{
    public interface ICurseHelper
    {
        string DisableScrollbarScript { get; }
        string HideCookiebarScript { get; }
        string GrabJsonScript { get; }

        bool IsAddonPageUrl(string url);
        bool IsFetchedDownloadUrl(string url);
        bool IsRedirectWithApiKeyUrl(string url);
        bool IsRealDownloadUrl(string url);

        string GetAddonNameFromAddonPageUrl(string url);
        string GetAddonNameFromFetchedDownloadUrl(string url);
        string GetAddonNameFromRealDownloadUrl(string url);
        string GetFileNameFromRealDownloadUrl(string url);

        CurseHelperJson SerializeAddonPageJson(string json);
        string BuildDownloadUrl(ulong projectId, ulong fileId);
    }
}
