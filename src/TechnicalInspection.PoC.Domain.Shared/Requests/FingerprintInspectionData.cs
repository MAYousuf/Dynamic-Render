using System.ComponentModel.DataAnnotations;

namespace TechnicalInspection.PoC.Requests;

public class FingerprintInspectionData : InspectionData
{
    [Required]
    [StringLength(64)]
    [Display(Name = "Fingerprint classification")]
    public string? FingerprintClassification { get; set; }

    [Required]
    [Range(1, 20)]
    [Display(Name = "Number of prints")]
    public int? NumberOfPrints { get; set; }

    [Display(Name = "Lifted successfully")]
    public bool LiftedSuccessfully { get; set; }

    [StringLength(2000)]
    [Display(Name = "Findings")]
    public string? Findings { get; set; }
}
