namespace WADH.Core
{
    public interface IConfigReader
    {
        string Storage { get; } // Named it like that, since config could be a file, or database, or whatever.

        string DownloadFolder { get; }
        IEnumerable<string> AddonUrls { get; }

        void ReadConfig();
        void ValidateConfig();
    }
}
