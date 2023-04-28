namespace WADH.WebView
{
    public sealed record WebViewHelperProgressData(
        WebViewHelperProgressState State, string Url, string Info, string Addon, string File, ulong Received, ulong Total);
}
