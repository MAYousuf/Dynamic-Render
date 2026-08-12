using System;
using System.Collections.Generic;

namespace TechnicalInspection.PoC.Requests;

/// <summary>
/// The whole request as captured by the single form: basic data plus the entire
/// Exhibit -> Evidence -> Inspection graph, each inspection already carrying its strongly typed
/// model. There is no partial or per-step variant of this DTO by design.
/// </summary>
public class CreateInspectionRequestDto
{
    public string RequestNumber { get; set; } = default!;

    public string Subject { get; set; } = default!;

    public DateTime RequestDate { get; set; }

    public List<CreateExhibitDto> Exhibits { get; set; } = new();
}

public class CreateExhibitDto
{
    public int SequenceNumber { get; set; }

    public string? Description { get; set; }

    public List<CreateEvidenceDto> Evidences { get; set; } = new();
}

public class CreateEvidenceDto
{
    public string EvidenceTypeCode { get; set; } = default!;

    public string? Description { get; set; }

    public List<CreateInspectionDto> Inspections { get; set; } = new();
}

public class CreateInspectionDto
{
    public string InspectionTypeCode { get; set; } = default!;

    /// <summary>
    /// Declared as the abstract base type: the concrete subclass carried here is what the
    /// combination resolves to, and is verified against it before anything is persisted.
    /// </summary>
    public InspectionData Data { get; set; } = default!;
}

public class InspectionRequestListDto
{
    public Guid Id { get; set; }

    public string RequestNumber { get; set; } = default!;

    public string Subject { get; set; } = default!;

    public DateTime RequestDate { get; set; }

    public DateTime CreationTime { get; set; }

    public int InspectionCount { get; set; }
}

public class InspectionRequestDetailDto
{
    public Guid Id { get; set; }

    public string RequestNumber { get; set; } = default!;

    public string Subject { get; set; } = default!;

    public DateTime RequestDate { get; set; }

    public InspectionRequestStatus Status { get; set; }

    public List<ExhibitDto> Exhibits { get; set; } = new();
}

public class ExhibitDto
{
    public Guid Id { get; set; }

    public int SequenceNumber { get; set; }

    public string? Description { get; set; }

    public List<EvidenceDto> Evidences { get; set; } = new();
}

public class EvidenceDto
{
    public Guid Id { get; set; }

    public string EvidenceTypeCode { get; set; } = default!;

    public string EvidenceTypeDisplayName { get; set; } = default!;

    public string? Description { get; set; }

    public List<InspectionDto> Inspections { get; set; } = new();
}

/// <summary>
/// Carries both halves of the round-trip so the review screen can show them side by side: the
/// raw column value, and the strongly typed object rebuilt from it.
/// </summary>
public class InspectionDto
{
    public Guid Id { get; set; }

    public string EvidenceTypeCode { get; set; } = default!;

    public string InspectionTypeCode { get; set; } = default!;

    public string InspectionTypeDisplayName { get; set; } = default!;

    public string DataDiscriminator { get; set; } = default!;

    public InspectionDataStatus DataStatus { get; set; }

    /// <summary>Exactly what is stored in the database column.</summary>
    public string RawJson { get; set; } = string.Empty;

    /// <summary>The same JSON re-indented for display only.</summary>
    public string FormattedJson { get; set; } = string.Empty;

    /// <summary>
    /// The concrete model rebuilt from <see cref="RawJson"/>. Its runtime type differs per row
    /// even though every row came from the same column.
    /// </summary>
    public InspectionData? Data { get; set; }

    /// <summary>Runtime CLR type name, shown to make the polymorphism visible.</summary>
    public string? DataTypeName { get; set; }

    /// <summary>Populated instead of <see cref="Data"/> when a row could not be rebuilt.</summary>
    public string? DeserializationError { get; set; }
}
