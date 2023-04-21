namespace WADH.Core
{
    public sealed record CurseHelperJson(
        bool IsValid,
        ulong Id,
        string Name,
        string Slug,
        ulong FileId,
        string FileName,
        ulong FileSize);
}
