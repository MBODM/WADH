using System.Diagnostics;

namespace WADH.Core
{
    public sealed class ExternalToolsHelper : IExternalToolsHelper
    {
        public bool CanOpenWauz()
        {
            return File.Exists(WauzFilePath());
        }

        public void OpenWauz()
        {
            Process.Start(WauzFilePath());
        }

        public void OpenExplorer(string arguments = "")
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                Process.Start("Explorer.exe");
            }
            else
            {
                Process.Start("Explorer.exe", arguments);
            }
        }

        private static string WauzFilePath()
        {
            return Path.Combine(GetAppFolder(), "WAUZ.exe");
        }

        private static string GetAppFolder()
        {
            // Since .NET Core AppContext.BaseDirectory is the preferred method

            return Path.GetFullPath(AppContext.BaseDirectory);
        }
    }
}
