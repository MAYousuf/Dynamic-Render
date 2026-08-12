using System;
using System.Text.Json;
using TechnicalInspection.PoC.Requests;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace TechnicalInspection.PoC.Inspections;

public class InspectionDataSerializer : IInspectionDataSerializer, ISingletonDependency
{
    private const string DiscriminatorProperty = "$type";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions IndentedOptions = new(Options)
    {
        WriteIndented = true
    };

    /// <summary>
    /// Serializing through the declared base type is what makes System.Text.Json emit the
    /// <c>$type</c> discriminator. Serializing the runtime type directly would silently omit it.
    /// </summary>
    public string Serialize(InspectionData data)
    {
        Check.NotNull(data, nameof(data));
        return JsonSerializer.Serialize(data, typeof(InspectionData), Options);
    }

    public InspectionData Deserialize(string json)
    {
        Check.NotNullOrWhiteSpace(json, nameof(json));

        return JsonSerializer.Deserialize<InspectionData>(json, Options)
               ?? throw new BusinessException(PoCDomainErrorCodes.UnknownInspectionDiscriminator)
                   .WithData("Discriminator", ReadDiscriminator(json) ?? "(none)");
    }

    public InspectionData DeserializeAs(string json, Type expectedType)
    {
        Check.NotNullOrWhiteSpace(json, nameof(json));
        Check.NotNull(expectedType, nameof(expectedType));

        var data = Deserialize(json);

        // The row was written by a different model than the current (EvidenceType, InspectionType)
        // combination resolves to - i.e. the stored data is stale, not merely unexpected.
        if (data.GetType() != expectedType)
        {
            throw new BusinessException(PoCDomainErrorCodes.InspectionDataTypeMismatch)
                .WithData("ExpectedType", expectedType.Name)
                .WithData("ActualType", data.GetType().Name)
                .WithData("Discriminator", ReadDiscriminator(json) ?? "(none)");
        }

        return data;
    }

    public string? ReadDiscriminator(string json)
    {
        if (json.IsNullOrWhiteSpace())
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            return document.RootElement.TryGetProperty(DiscriminatorProperty, out var value)
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string Prettify(string json)
    {
        if (json.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement, IndentedOptions);
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
