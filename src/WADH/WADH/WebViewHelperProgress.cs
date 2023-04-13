namespace WADH
{
    public sealed record WebViewHelperProgress(
        WebViewHelperProgressState State,
        string Url,
        string Info = "",
        string Addon = "",
        string File = "",
        ulong Received = 0,
        ulong Total = 0);
}
