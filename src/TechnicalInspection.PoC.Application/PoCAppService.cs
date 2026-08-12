using System;
using System.Collections.Generic;
using System.Text;
using TechnicalInspection.PoC.Localization;
using Volo.Abp.Application.Services;

namespace TechnicalInspection.PoC;

/* Inherit your application services from this class.
 */
public abstract class PoCAppService : ApplicationService
{
    protected PoCAppService()
    {
        LocalizationResource = typeof(PoCResource);
    }
}
