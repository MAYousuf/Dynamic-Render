using System.Collections.Generic;

namespace TechnicalInspection.PoC.Inspections;

/// <summary>
/// The single place where the relationship between fixed master data and concrete inspection
/// models is declared. Master data is seeded from it, the resolver reads from it, and Step 3
/// renders from it.
/// </summary>
public interface IInspectionDataRegistry
{
    IReadOnlyList<InspectionDataDefinition> Definitions { get; }

    IReadOnlyList<EvidenceTypeDescriptor> EvidenceTypes { get; }

    IReadOnlyList<InspectionTypeDescriptor> InspectionTypes { get; }
}
