using System.Diagnostics;

namespace WADH.Core
{
    public sealed class ExternalToolsHelper : IExternalToolsHelper
    {
        public bool CanOpenTool(string exeFileName)
        {
            if (string.IsNullOrWhiteSpace(exeFileName))
            {
                throw new ArgumentException($"'{nameof(exeFileName)}' cannot be null or whitespace.", nameof(exeFileName));
            }

            return File.Exists(Path.Combine(GetAppFolder(), exeFileName));
        }

        public void OpenTool(string exeFileName)
        {
            Process.Start(Path.Combine(GetAppFolder(), exeFileName));
        }

        private static string GetAppFolder()
        {
            // Since .NET Core AppContext.BaseDirectory is the preferred method

            return Path.GetFullPath(AppContext.BaseDirectory);
        }
    }
}
