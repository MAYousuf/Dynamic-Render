using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TechnicalInspection.PoC.Inspections;

/// <summary>
/// Resolves the concrete inspection model from the business combination, and from the persisted
/// discriminator when reading data back. This is the component section 3 of the design calls for.
/// </summary>
public interface IInspectionDataTypeResolver
{
    /// <summary>Throws <c>BusinessException</c> when the combination is unsupported.</summary>
    InspectionDataDefinition Resolve(string evidenceTypeCode, string inspectionTypeCode);

    bool TryResolve(
        string evidenceTypeCode,
        string inspectionTypeCode,
        [NotNullWhen(true)] out InspectionDataDefinition? definition);

    /// <summary>Throws <c>BusinessException</c> when the discriminator is unknown.</summary>
    InspectionDataDefinition ResolveByDiscriminator(string discriminator);

    bool TryResolveByDiscriminator(
        string discriminator,
        [NotNullWhen(true)] out InspectionDataDefinition? definition);

    /// <summary>The inspection kinds available for an evidence type, for Step 2's dropdowns.</summary>
    IReadOnlyList<InspectionDataDefinition> GetForEvidenceType(string evidenceTypeCode);
}
