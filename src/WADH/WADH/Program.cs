using WADH.Core;
using WADH.WebView;

namespace WADH
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            var fileLogger = new FileLogger();
            var curseHelper = new CurseHelper();
            Application.Run(
                new MainForm(
                    new ExternalToolsHelper(),
                    new ConfigReader(curseHelper),
                    fileLogger,
                    new FileSystemHelper(),
                    new WebViewHelper(curseHelper, fileLogger)));
        }
    }
}
