using System;

namespace TechnicalInspection.PoC.Inspections;

/// <summary>
/// One row of the deterministic mapping recommended by section 18 of the design:
/// business combination -> C# type -> Razor partial -> persisted discriminator.
/// </summary>
/// <param name="EvidenceTypeCode">Master-data code of the evidence type.</param>
/// <param name="InspectionTypeCode">Master-data code of the inspection type.</param>
/// <param name="Discriminator">Value written into the persisted JSON as <c>$type</c>.</param>
/// <param name="DataType">Concrete <c>InspectionData</c> subclass bound and serialized.</param>
/// <param name="PartialViewName">Strongly typed Razor partial that renders the form.</param>
public record InspectionDataDefinition(
    string EvidenceTypeCode,
    string InspectionTypeCode,
    string Discriminator,
    Type DataType,
    string PartialViewName);

public record EvidenceTypeDescriptor(string Code, string DisplayName, int DisplayOrder);

public record InspectionTypeDescriptor(string Code, string DisplayName, int DisplayOrder);
