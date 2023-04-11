namespace WADH.Core
{
    public interface ICurseHelper
    {
        bool IsAddonUrl(string url);
        bool IsRedirect1Url(string url);
        bool IsRedirect2Url(string url);
        bool IsDownloadUrl(string url);

        string GetAddonNameFromAddonUrl(string url);
        string GetFileNameFromDownloadUrl(string url);
        string GetAddonNameFromDownloadUrl(string url);

        string AdjustPageAppearanceScript();
        string GrabRedirectDownloadUrlScript();
    }
}
