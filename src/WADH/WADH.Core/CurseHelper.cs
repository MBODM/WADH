using System.Reflection.Metadata;

namespace WADH.Core
{
    public sealed class CurseHelper : ICurseHelper
    {
        public string AdjustPageAppearanceScript => GetAdjustPageAppearanceScript();
        public string GrabRedirectDownloadUrlScript => GetGrabRedirectDownloadUrlScript();

        public bool IsAddonUrl(string url)
        {
            // https://www.curseforge.com/wow/addons/coordinates/download
            url = Guard(url);
            return url.StartsWith("https://www.curseforge.com/wow/addons/") && url.EndsWith("/download");
        }

        public bool IsRedirect1Url(string url)
        {
            // https://www.curseforge.com/wow/addons/coordinates/download/4364314/file
            url = Guard(url);
            return url.StartsWith("https://www.curseforge.com/wow/addons/") && url.EndsWith("/file");
        }

        public bool IsRedirect2Url(string url)
        {
            // https://edge.forgecdn.net/files/4364/314/Coordinates-2.4.1.zip?api-key=267C6CA3
            url = Guard(url);
            return url.StartsWith("https://edge.forgecdn.net/files/") && url.Contains("?api-key=");
        }

        public bool IsDownloadUrl(string url)
        {
            // https://mediafilez.forgecdn.net/files/4364/314/Coordinates-2.4.1.zip
            url = Guard(url);
            return url.StartsWith("https://mediafilez.forgecdn.net/files/") && url.EndsWith(".zip");
        }

        public string GetAddonNameFromAddonUrl(string url)
        {
            // https://www.curseforge.com/wow/addons/coordinates/download
            url = Guard(url);
            return IsAddonUrl(url) ? url.Split("addons/").Last().Split("/download").First().ToLower() : string.Empty;
        }

        public string GetAddonNameFromRedirect1Url(string url)
        {
            // https://www.curseforge.com/wow/addons/coordinates/download/4364314/file
            url = Guard(url);
            return IsRedirect1Url(url) ? url.Split("addons/").Last().Split("/download").First().ToLower() : string.Empty;
        }

        public string GetAddonNameFromDownloadUrl(string url)
        {
            // https://mediafilez.forgecdn.net/files/4364/314/Coordinates-2.4.1.zip
            url = Guard(url);
            return IsDownloadUrl(url) ? url.Split('/').Last().Split('-').First().ToLower() : string.Empty;
        }

        public string GetFileNameFromDownloadUrl(string url)
        {
            // https://mediafilez.forgecdn.net/files/4364/314/Coordinates-2.4.1.zip
            url = Guard(url);
            return IsDownloadUrl(url) ? url.Split('/').Last() : string.Empty;
        }

        private static string GetAdjustPageAppearanceScript()
        {
            // The app disables the JS engine, before loading the addon page, to prevent the 5 sec timer (JS) from running.
            // With disabled JS some noscript tags become active and all relevant information is no longer visible at top.
            // Therefore removing the empty img and the "JS is disabled" message, so all relevant stuff moves to top again.
            // And since WebView2 offers no property to hide scrollbars, the last line here does this, as some final step.

            return
            "let img = document.querySelector('body img');" +
            "if (img) { img.style.visibility = 'hidden'; img.style.height = '0px'; }" +
            "let noscripts = document.querySelectorAll('body noscript');" +
            "if (noscripts && noscripts.length >= 2) { noscripts[1].style.visibility = 'hidden'; noscripts[1].style.height = '0px';}" +
            "let containers = document.querySelectorAll('body div.container');" +
            "if (containers && containers.length >= 3) { containers[3].style.visibility = 'hidden'; containers[3].style.height = '50px';}" +
            "document.body.style.overflow = 'hidden'";
        }

        private static string GetGrabRedirectDownloadUrlScript()
        {
            // The app disables the JS engine, loads the addon site, grabs the initial download url from that site (this script)
            // and re-enables the JS engine. Then the app navigates to the grabbed download url (with JS engine enabled), so the
            // 2 following redirects can happen, without Cloudflare problems. The redirects end up at the real download url and
            // the app starts downloading the zip. Otherwise all downloads wait for 5 sec until the page´s JS starts the process.

            return "document.getElementsByClassName(\"alink underline\")[0].href";
        }

        private static string Guard(string url)
        {
            return url?.Trim() ?? string.Empty;
        }
    }
}
