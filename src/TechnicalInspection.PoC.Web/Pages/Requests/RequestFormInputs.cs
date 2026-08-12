using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TechnicalInspection.PoC.Requests;

namespace TechnicalInspection.PoC.Web.Pages.Requests;

/// <summary>
/// The bound shape of the request form. Top-level types rather than nested page-model classes,
/// because the same graph is bound from the final form post and from the JSON body the browser
/// sends when it asks for step 3.
/// </summary>
public class BasicDataInput
{
    [Required]
    [StringLength(32)]
    [Display(Name = "Request number")]
    public string? RequestNumber { get; set; }

    [Required]
    [StringLength(256)]
    [Display(Name = "Subject")]
    public string? Subject { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Request date")]
    public DateTime RequestDate { get; set; } = DateTime.Today;
}

public class ExhibitInput
{
    public int SequenceNumber { get; set; }

    public string? Description { get; set; }

    public List<EvidenceInput> Evidences { get; set; } = new();
}

public class EvidenceInput
{
    public string? EvidenceTypeCode { get; set; }

    public string? Description { get; set; }

    public List<InspectionInput> Inspections { get; set; } = new();
}

public class InspectionInput
{
    /// <summary>
    /// Generated in the browser when the row is added, and the only link between a row here and its
    /// data card in step 3. Nothing server-side depends on it after the post.
    /// </summary>
    public Guid Id { get; set; }

    public string? InspectionTypeCode { get; set; }
}

/// <summary>
/// The JSON body of the step-3 render request: the structure the browser holds, and nothing else.
/// Which model and which partial each inspection gets is the server's decision.
/// </summary>
public class DataPaneInput
{
    public List<ExhibitInput> Exhibits { get; set; } = new();
}

/// <summary>
/// One step-3 data card. Unchanged in spirit from the old Step 3 page: <see cref="Data"/> is
/// declared as the abstract base type and filled with the right concrete subclass by
/// <c>InspectionDataModelBinder</c>, using <see cref="Discriminator"/> as the hint.
/// </summary>
public class InspectionEntryInput
{
    public Guid InspectionId { get; set; }

    public string Discriminator { get; set; } = default!;

    public InspectionData? Data { get; set; }
}

/// <summary>
/// Everything a data card needs to render itself. The partial view name in particular is always
/// resolved server-side and never read from the request.
/// </summary>
public class InspectionCardContext
{
    public string Discriminator { get; set; } = default!;

    public string PartialViewName { get; set; } = default!;

    public string DataTypeName { get; set; } = default!;

    public int? ExhibitSequenceNumber { get; set; }

    public string? EvidenceTypeDisplayName { get; set; }

    public string? InspectionTypeDisplayName { get; set; }

    public string? EvidenceDescription { get; set; }
}
