using Microsoft.AspNetCore.Mvc.ModelBinding;
using TechnicalInspection.PoC.Requests;

namespace TechnicalInspection.PoC.Web.Binding;

/// <summary>
/// Activates <see cref="InspectionDataModelBinder"/> for properties declared as the abstract
/// <see cref="InspectionData"/> base type.
/// <para>
/// The match is on the abstract type only. Once the binder resolves a concrete subclass, the
/// nested binding request carries that subclass's metadata, so this provider does not match
/// again and the framework's ordinary complex-object binder takes over.
/// </para>
/// </summary>
public class InspectionDataModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        return context.Metadata.ModelType == typeof(InspectionData)
            ? new InspectionDataModelBinder()
            : null;
    }
}
