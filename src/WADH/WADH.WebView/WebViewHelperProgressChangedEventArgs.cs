using System.ComponentModel;

namespace WADH.WebView
{
    public sealed class WebViewHelperProgressChangedEventArgs : ProgressChangedEventArgs
    {
        public WebViewHelperProgressChangedEventArgs(int progressPercentage, object? userState) : base(progressPercentage, userState)
        {
        }

        public WebViewHelperProgressChangedEventArgs(
            int progressPercentage, object? userState, WebViewHelperProgressData? progressData) : base(progressPercentage, userState)
        {
            ProgressData = progressData;
        }

        public WebViewHelperProgressData? ProgressData { get; }
    }
}
