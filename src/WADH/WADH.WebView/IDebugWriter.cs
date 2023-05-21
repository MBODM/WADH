using System.Runtime.CompilerServices;

namespace WADH.WebView
{
    internal interface IDebugWriter
    {
        void Start();
        void Reached([CallerMemberName] string caller = "");
        void Text(string message, bool showCaller = true, [CallerMemberName] string caller = "");
        void Success(string message = "", [CallerMemberName] string caller = "");
        void Error(string message = "", [CallerMemberName] string caller = "");
        void HandlerAdded(string handler, [CallerMemberName] string caller = "");
        void HandlerRemoved(string handler, [CallerMemberName] string caller = "");
    }
}
