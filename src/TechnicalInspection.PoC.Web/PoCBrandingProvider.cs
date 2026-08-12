using Microsoft.Extensions.Localization;
using TechnicalInspection.PoC.Localization;
using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace TechnicalInspection.PoC.Web;

[Dependency(ReplaceServices = true)]
public class PoCBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<PoCResource> _localizer;

    public PoCBrandingProvider(IStringLocalizer<PoCResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
