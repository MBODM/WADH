namespace WADH.Core
{
    public sealed record CurseHelperJson(
        bool IsValid, ulong ProjectId, string ProjectName, string ProjectSlug, ulong FileId, string FileName, ulong FileSize);
}
