using System.Xml.Linq;

namespace WADH.Core
{
    public sealed class ConfigReader : IConfigReader
    {
        private readonly string xmlFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WADH.xml");

        private readonly ICurseHelper curseHelper;

        public ConfigReader(ICurseHelper curseHelper)
        {
            this.curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));
        }

        public string Storage => xmlFile;

        public string DownloadFolder { get; private set; } = string.Empty;

        public IEnumerable<string> AddonUrls { get; private set; } = Enumerable.Empty<string>();

        public void ReadConfig()
        {
            if (!File.Exists(xmlFile))
            {
                throw new InvalidOperationException("Config file not exists.");
            }

            var document = XDocument.Load(xmlFile);

            var message = "Config file format is invalid.";

            DownloadFolder = document?.
                Element("root")?.
                Element("folder")?.
                Value?.
                ToString() ?? throw new InvalidOperationException(message);

            AddonUrls = document?.
                Element("root")?.
                Element("addons")?.
                Elements()?.
                Where(e => e.Name == "url" && IsValidCurseUrl(e.Value))?.
                Select(e => e.Value.Trim().ToLower())?.
                Distinct() ?? throw new InvalidOperationException(message);
        }

        public void ValidateConfig()
        {
            if (DownloadFolder == string.Empty)
            {
                throw new InvalidOperationException("The config file contains no folder, to download the zip files into.");
            }

            if (!AddonUrls.Any())
            {
                throw new InvalidOperationException("The config file contains no valid url, so there is nothing to download.");
            }
        }

        private bool IsValidCurseUrl(string url)
        {
            return curseHelper.IsAddonUrl(url);
        }
    }
}
