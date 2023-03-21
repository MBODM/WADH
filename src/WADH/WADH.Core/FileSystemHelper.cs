namespace WADH.Core
{
    public sealed class FileSystemHelper : IFileSystemHelper
    {
        public async Task DeleteAllZipFilesInFolderAsync(string folder, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                throw new ArgumentException($"'{nameof(folder)}' cannot be null or whitespace.", nameof(folder));
            }

            if (!Directory.Exists(folder))
            {
                throw new InvalidOperationException("Given folder not exists.");
            }

            folder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));

            var zipFiles = Directory.EnumerateFiles(folder, "*.zip", SearchOption.TopDirectoryOnly);

            if (!zipFiles.Any())
            {
                return;
            }

            // After some measurements this async approach seems to be around 3 times faster than the
            // sync approach. Looks like modern SSD/OS configurations are rather concurrent-friendly.

            var tasks = new List<Task>();

            // No need for a ThrowIfCancellationRequested() here, since Task.Run() cancels on its own (if the
            // task has not already started) and since the sync method one-liner can not be cancelled anyway.

            tasks.AddRange(zipFiles.Select(zipFile => Task.Run(() => File.Delete(zipFile), cancellationToken)));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            // Wait for deletion, as described at:
            // https://stackoverflow.com/questions/34981143/is-directory-delete-create-synchronous

            var counter = 0;

            while (Directory.EnumerateFiles(folder, "*.zip", SearchOption.TopDirectoryOnly).Any())
            {
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);

                // Throw exception after ~500ms to prevent blocking forever.

                counter++;

                if (counter > 10)
                {
                    throw new InvalidOperationException("Could not delete folder content.");
                }
            }
        }
    }
}
