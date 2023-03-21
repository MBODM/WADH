using System.Runtime.CompilerServices;

namespace WADH.Core
{
    public interface IErrorLogger
    {
        void Log(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void Log(Exception exception, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
    }
}
