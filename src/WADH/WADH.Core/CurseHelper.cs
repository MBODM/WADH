﻿namespace WADH.Core
{
    public sealed class CurseHelper : ICurseHelper
    {
        public bool IsAddonUrl(string url)
        {
            url = Guard(url);
            // https://www.curseforge.com/wow/addons/coordinates/download
            return url.StartsWith("https://www.curseforge.com/wow/addons/") && url.EndsWith("/download");
        }

        public bool IsRedirect1Url(string url)
        {
            url = Guard(url);
            // https://www.curseforge.com/wow/addons/coordinates/download/4364314/file
            return url.StartsWith("https://www.curseforge.com/wow/addons/") && url.Contains("/download/") && url.EndsWith("/file");
        }

        public bool IsRedirect2Url(string url)
        {
            url = Guard(url);
            // https://edge.forgecdn.net/files/4364/314/Coordinates-2.4.1.zip?api-key=267C6CA3
            return url.StartsWith("https://edge.forgecdn.net/files/") && url.Contains("?api-key=");
        }

        public bool IsRealDownloadUrl(string url)
        {
            url = Guard(url);
            // https://mediafilez.forgecdn.net/files/4364/314/Coordinates-2.4.1.zip
            return url.StartsWith("https://mediafilez.forgecdn.net/files/") && url.EndsWith(".zip");
        }

        public string GetAddonNameFromAddonUrl(string url)
        {
            url = Guard(url);

            return IsAddonUrl(url) ? url.Split("https://www.curseforge.com/wow/addons/").Last().Split("/download").First() : string.Empty;
        }

        public string AdjustPageAppearanceScript()
        {
            // The app disables the JS engine, before loading the addon page, to prevent the 5 sec timer (JS) from running.
            // With disabled JS some noscript tags become active and all relevant information is no longer visible at top.
            // Therefore removing the empty img and the "JS is disabled" message, so all relevant stuff moves to top again.

            return
                "let img = document.querySelector('body img');" +
                "if (img) { img.remove(); }" +
                "let noscripts = document.querySelectorAll('body noscript');" +
                "if (noscripts.length >= 2) noscripts[1].remove();";
        }

        public string GrabRedirectDownloadUrlScript()
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
