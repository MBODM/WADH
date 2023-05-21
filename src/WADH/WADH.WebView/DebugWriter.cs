using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WADH.WebView
{
    internal sealed class DebugWriter : IDebugWriter
    {
        public void Start()
        {
            Debug.WriteLine("================================================================================");
            Debug.WriteLine("Start next addon");
        }

        public void Reached([CallerMemberName] string caller = "")
        {
            Write(caller, "Reached");
        }

        public void Text(string message, bool showCaller = true, [CallerMemberName] string caller = "")
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException($"'{nameof(message)}' cannot be null or whitespace.", nameof(message));
            }

            if (showCaller)
            {
                Write(caller, message);
            }
            else
            {
                Debug.WriteLine(message);
            }
        }

        public void Success(string message = "", [CallerMemberName] string caller = "")
        {
            Write(caller, string.IsNullOrEmpty(message) ? "Success" : $"Success ({message})");
        }

        public void Error(string message = "", [CallerMemberName] string caller = "")
        {
            Write(caller, string.IsNullOrEmpty(message) ? "Error" : $"Error ({message})");
        }

        public void HandlerAdded(string handler, [CallerMemberName] string caller = "")
        {
            if (string.IsNullOrWhiteSpace(handler))
            {
                throw new ArgumentException($"'{nameof(handler)}' cannot be null or whitespace.", nameof(handler));
            }

            Write(caller, $"Handler added ({handler})");
        }

        public void HandlerRemoved(string handler, [CallerMemberName] string caller = "")
        {
            if (string.IsNullOrWhiteSpace(handler))
            {
                throw new ArgumentException($"'{nameof(handler)}' cannot be null or whitespace.", nameof(handler));
            }

            Write(caller, $"Handler removed ({handler})");
        }

        private static void Write(string caller, string s)
        {
            Debug.WriteLine($"[{caller}] {s}");
        }
    }
}
