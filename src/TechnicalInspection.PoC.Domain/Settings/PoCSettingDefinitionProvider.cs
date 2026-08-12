using Volo.Abp.Settings;

namespace TechnicalInspection.PoC.Settings;

public class PoCSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(PoCSettings.MySetting1));
    }
}
