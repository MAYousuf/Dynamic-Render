using System.Text.Json.Serialization;

namespace TechnicalInspection.PoC.Requests;

/// <summary>
/// Base type for the variable part of an inspection.
/// <para>
/// The derived-type map below is the single source of truth for JSON discriminators. It is read
/// back by <c>InspectionDataRegistry</c> via reflection, so a new inspection kind is registered
/// in exactly one place.
/// </para>
/// <para>
/// Deriving from this type is all that is required for System.Text.Json to round-trip the
/// concrete shape through a single database column.
/// </para>
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BallisticInspectionData), InspectionDataDiscriminators.Ballistic)]
[JsonDerivedType(typeof(FingerprintInspectionData), InspectionDataDiscriminators.Fingerprint)]
[JsonDerivedType(typeof(ChemicalAnalysisInspectionData), InspectionDataDiscriminators.ChemicalAnalysis)]
[JsonDerivedType(typeof(HandwritingInspectionData), InspectionDataDiscriminators.Handwriting)]
public abstract class InspectionData
{
}
