namespace WADH.Core
{
    public interface IExternalToolsHelper
    {
        bool CanOpenTool(string exeFileName);
        void OpenTool(string exeFileName);
    }
}
