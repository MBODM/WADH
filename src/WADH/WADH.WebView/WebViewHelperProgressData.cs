namespace WADH.WebView
{
    public sealed record WebViewHelperProgressData(WebViewHelperProgressState State, string Addon, string Message,
        string File, ulong Received, ulong Total);
}
