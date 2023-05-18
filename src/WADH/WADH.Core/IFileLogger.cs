using System.Runtime.CompilerServices;

namespace WADH.Core
{
    public interface IFileLogger
    {
        string Storage { get; } // Named it like that, since log could be a file, or database, or whatever.

        void Log(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void Log(IEnumerable<string> multiLineMessage, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void Log(Exception exception, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
    }
}
