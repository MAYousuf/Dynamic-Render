using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using TechnicalInspection.PoC.Inspections;
using TechnicalInspection.PoC.Requests;

namespace TechnicalInspection.PoC.Web.Binding;

/// <summary>
/// Binds an abstract <see cref="InspectionData"/> property to the concrete subclass indicated by
/// the discriminator posted alongside it.
/// <para>
/// This is what allows a single form post to carry several completely different inspection shapes
/// while the page model still declares ordinary strongly typed properties - no
/// <c>Request.Form</c> walking and no dictionaries.
/// </para>
/// <para>
/// Property binding itself is delegated to the framework's own binder for the resolved type, so
/// type conversion, <c>[Required]</c>, nested prefixes and ModelState keys all behave exactly as
/// they would for a non-polymorphic model.
/// </para>
/// </summary>
public class InspectionDataModelBinder : IModelBinder
{
    /// <summary>
    /// Name of the sibling form field carrying the discriminator, e.g. the field
    /// <c>Inspections[0].Discriminator</c> for the model <c>Inspections[0].Data</c>.
    /// </summary>
    public const string DiscriminatorFieldName = "Discriminator";

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var services = bindingContext.HttpContext.RequestServices;
        var resolver = services.GetRequiredService<IInspectionDataTypeResolver>();

        var discriminatorKey = BuildSiblingKey(bindingContext.ModelName, DiscriminatorFieldName);
        var discriminator = bindingContext.ValueProvider.GetValue(discriminatorKey).FirstValue;

        if (string.IsNullOrWhiteSpace(discriminator))
        {
            bindingContext.ModelState.TryAddModelError(
                discriminatorKey,
                "The inspection kind was not submitted, so its data could not be read.");

            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        if (!resolver.TryResolveByDiscriminator(discriminator, out var definition))
        {
            bindingContext.ModelState.TryAddModelError(
                discriminatorKey,
                $"'{discriminator}' is not a known inspection kind.");

            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        var metadataProvider = services.GetRequiredService<IModelMetadataProvider>();
        var binderFactory = services.GetRequiredService<IModelBinderFactory>();

        var metadata = metadataProvider.GetMetadataForType(definition.DataType);

        var binder = binderFactory.CreateBinder(new ModelBinderFactoryContext
        {
            Metadata = metadata,
            CacheToken = metadata
        });

        var model = Activator.CreateInstance(definition.DataType)!;

        // Re-enter binding with metadata for the concrete type while keeping the same field name
        // and prefix, so the posted names (Inspections[0].Data.Caliber, ...) still line up.
        ModelBindingResult result;

        using (bindingContext.EnterNestedScope(
                   metadata,
                   bindingContext.FieldName,
                   bindingContext.ModelName,
                   model))
        {
            await binder.BindModelAsync(bindingContext);
            result = bindingContext.Result;
        }

        // The nested scope restores the outer result on dispose, so it is assigned explicitly here.
        bindingContext.Result = result.IsModelSet
            ? ModelBindingResult.Success(result.Model)
            : ModelBindingResult.Success(model);
    }

    /// <summary>
    /// Turns <c>Inspections[0].Data</c> into <c>Inspections[0].Discriminator</c>.
    /// </summary>
    private static string BuildSiblingKey(string modelName, string siblingFieldName)
    {
        var lastSeparator = modelName.LastIndexOf('.');

        return lastSeparator < 0
            ? siblingFieldName
            : string.Concat(modelName.AsSpan(0, lastSeparator + 1), siblingFieldName);
    }
}
