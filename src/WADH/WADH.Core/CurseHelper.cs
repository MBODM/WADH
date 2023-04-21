using System.Text.Json;

namespace WADH.Core
{
    public sealed class CurseHelper : ICurseHelper
    {
        public string AdjustPageAppearanceScript =>
            "/*let cookiebar = document.querySelector('div#cookiebar');*/" +
            "/*if (cookiebar) cookiebar.style.visibility = 'hidden';*/" +
            "document.body.style.overflow = 'hidden';";

        public string GrabJsonFromAddonPageScript =>
            "let script = document.querySelector('script#__NEXT_DATA__');" +
            "let json = script?.innerHTML ?? '';" +
            "json;";

        public bool IsAddonUrl(string url)
        {
            // https://www.curseforge.com/wow/addons/coordinates
            url = Guard(url);
            return url.StartsWith("https://www.curseforge.com/wow/addons/") && !url.EndsWith("/addons");
        }

        public bool IsFetchedDownloadUrl(string url)
        {
            // https://www.curseforge.com/api/v1/mods/298607/files/4364314/download
            url = Guard(url);
            return url.StartsWith("https://www.curseforge.com/api/v1/mods/") && url.Contains("/files/") && url.EndsWith("/download");
        }

        public bool IsRedirectUrlWithApiKey(string url)
        {
            // https://edge.forgecdn.net/files/4364/314/Coordinates-2.4.1.zip?api-key=267C6CA3
            url = Guard(url);
            return url.StartsWith("https://edge.forgecdn.net/files/") && url.Contains("?api-key=");
        }

        public bool IsRealDownloadUrl(string url)
        {
            // https://mediafilez.forgecdn.net/files/4364/314/Coordinates-2.4.1.zip
            url = Guard(url);
            return url.StartsWith("https://mediafilez.forgecdn.net/files/") && url.EndsWith(".zip");
        }

        public string GetAddonNameFromAddonUrl(string url)
        {
            // https://www.curseforge.com/wow/addons/coordinates
            url = Guard(url);
            return IsAddonUrl(url) ? url.Split("https://www.curseforge.com/wow/addons/").Last().ToLower() : string.Empty;
        }

        public string GetAddonNameFromRedirect1Url(string url)
        {
            // https://www.curseforge.com/wow/addons/coordinates/download/4364314/file
            url = Guard(url);
            return IsFetchedDownloadUrl(url) ? url.Split("addons/").Last().Split("/download").First().ToLower() : string.Empty;
        }

        public string GetAddonNameFromDownloadUrl(string url)
        {
            // https://mediafilez.forgecdn.net/files/4364/314/Coordinates-2.4.1.zip
            url = Guard(url);
            return IsRealDownloadUrl(url) ? url.Split('/').Last().Split('-').First().ToLower() : string.Empty;
        }

        public string GetFileNameFromDownloadUrl(string url)
        {
            // https://mediafilez.forgecdn.net/files/4364/314/Coordinates-2.4.1.zip
            url = Guard(url);
            return IsRealDownloadUrl(url) ? url.Split('/').Last() : string.Empty;
        }

        public CurseHelperJson SerializeAddonPageJson(string json)
        {
            var invalid = new CurseHelperJson(false, 0, string.Empty, string.Empty, 0, string.Empty, 0);

            if (string.IsNullOrWhiteSpace(json))
            {
                return invalid;
            }

            // Curse addon page JSON format:
            // props
            //   pageProps
            //     project
            //       id             --> Short number for download url       Example --> 3358
            //       mainFile
            //         fileName     --> The name of the zip file            Example --> "DBM-10.0.35.zip"
            //         fileSize     --> The size of the zip file            Example --> 123456789
            //         id           --> Long number for download url        Example --> 4485146
            //       name           --> Useful name of the addon            Example --> "Deadly Boss Mods (DBM)"
            //       slug           --> Slug name of the addon              Example --> "deadly-boss-mods"

            try
            {
                var doc = JsonDocument.Parse(json);

                var project = doc.RootElement.GetProperty("props").GetProperty("pageProps").GetProperty("project");
                var projectId = project.GetProperty("id").GetUInt64();
                var name = project.GetProperty("name").GetString() ?? string.Empty;
                var slug = project.GetProperty("slug").GetString() ?? string.Empty;
                var mainFile = project.GetProperty("mainFile");
                var mainFileId = mainFile.GetProperty("id").GetUInt64();
                var fileName = mainFile.GetProperty("fileName").GetString() ?? string.Empty;
                var fileSize = mainFile.GetProperty("fileLength").GetUInt64();

                return new CurseHelperJson(true, projectId, name, slug, mainFileId, fileName, fileSize);
            }
            catch
            {
                return invalid;
            }
        }

        public string BuildDownloadUrl(ulong projectId, ulong fileId)
        {
            // https://www.curseforge.com/api/v1/mods/3358/files/4485146/download

            return $"https://www.curseforge.com/api/v1/mods/{projectId}/files/{fileId}/download";
        }

        private static string Guard(string url)
        {
            return url?.Trim().TrimEnd('/') ?? string.Empty;
        }
    }
}
