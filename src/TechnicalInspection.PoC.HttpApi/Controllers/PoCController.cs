using TechnicalInspection.PoC.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace TechnicalInspection.PoC.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class PoCController : AbpControllerBase
{
    protected PoCController()
    {
        LocalizationResource = typeof(PoCResource);
    }
}
