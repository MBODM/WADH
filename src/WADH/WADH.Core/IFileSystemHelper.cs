namespace WADH.Core
{
    public interface IFileSystemHelper
    {
        Task DeleteAllZipFilesInFolderAsync(string folder, CancellationToken cancellationToken = default);
    }
}
