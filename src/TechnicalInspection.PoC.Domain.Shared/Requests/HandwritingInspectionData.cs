using System;
using System.ComponentModel.DataAnnotations;

namespace TechnicalInspection.PoC.Requests;

public class HandwritingInspectionData : InspectionData
{
    [Required]
    [StringLength(128)]
    [Display(Name = "Document description")]
    public string? DocumentDescription { get; set; }

    [Required]
    [StringLength(128)]
    [Display(Name = "Reference sample")]
    public string? ReferenceSample { get; set; }

    [Display(Name = "Document date")]
    [DataType(DataType.Date)]
    public DateTime? DocumentDate { get; set; }

    [Range(0, 100)]
    [Display(Name = "Match confidence (%)")]
    public int? MatchConfidence { get; set; }

    [StringLength(2000)]
    [Display(Name = "Conclusion")]
    public string? Conclusion { get; set; }
}
