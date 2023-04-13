namespace WADH.Core
{
    public interface ICurseHelper
    {
        string AdjustPageAppearanceScript { get; }
        string GrabRedirectDownloadUrlScript { get; }

        bool IsAddonUrl(string url);
        bool IsRedirect1Url(string url);
        bool IsRedirect2Url(string url);
        bool IsDownloadUrl(string url);

        string GetAddonNameFromAddonUrl(string url);
        string GetAddonNameFromRedirect1Url(string url);
        string GetAddonNameFromDownloadUrl(string url);
        string GetFileNameFromDownloadUrl(string url);
    }
}
