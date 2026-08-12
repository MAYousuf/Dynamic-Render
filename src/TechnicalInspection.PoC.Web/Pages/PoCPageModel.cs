using TechnicalInspection.PoC.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace TechnicalInspection.PoC.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class PoCPageModel : AbpPageModel
{
    protected PoCPageModel()
    {
        LocalizationResourceType = typeof(PoCResource);
    }
}
