namespace WADH.Core
{
    public interface IExternalToolsHelper
    {
        bool CanOpenWauz();
        void OpenWauz();
        void OpenExplorer(string arguments = "");
    }
}
