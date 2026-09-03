namespace Whitestone.SegnoSharp.ViewModels.Security;

public sealed class RoleMappingEdit
{
    public int Id { get; init; }
    public string ClaimValue { get; init; } = "";
    public string Description { get; set; }

}