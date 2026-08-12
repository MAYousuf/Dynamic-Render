using TechnicalInspection.PoC.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace TechnicalInspection.PoC.Permissions;

public class PoCPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(PoCPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(PoCPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<PoCResource>(name);
    }
}
