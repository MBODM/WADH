namespace WADH.Core
{
    public interface IConfigReader
    {
        string DownloadFolder { get; }
        IEnumerable<string> AddonUrls { get; }

        void ReadConfig();
        void ValidateConfig();
    }
}
