namespace WADH.Core
{
    public interface ICurseHelper
    {
        string AdjustPageAppearanceScript { get; }
        string GrabJsonFromAddonPageScript { get; }

        bool IsAddonUrl(string url);
        bool IsFetchedDownloadUrl(string url);
        bool IsRedirectUrlWithApiKey(string url);
        bool IsRealDownloadUrl(string url);

        string GetAddonNameFromAddonUrl(string url);
        string GetAddonNameFromRedirect1Url(string url);
        string GetAddonNameFromDownloadUrl(string url);
        string GetFileNameFromDownloadUrl(string url);

        CurseHelperJson SerializeAddonPageJson(string json);
        string BuildDownloadUrl(ulong projectId, ulong fileId);
    }
}
