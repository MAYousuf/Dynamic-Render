using System.ComponentModel.DataAnnotations;

namespace TechnicalInspection.PoC.Requests;

public class BallisticInspectionData : InspectionData
{
    [Required]
    [StringLength(64)]
    [Display(Name = "Weapon type")]
    public string? WeaponType { get; set; }

    [Required]
    [StringLength(32)]
    [Display(Name = "Caliber")]
    public string? Caliber { get; set; }

    [StringLength(64)]
    [Display(Name = "Serial number")]
    public string? SerialNumber { get; set; }

    [Range(1, 500)]
    [Display(Name = "Rounds recovered")]
    public int? RoundsRecovered { get; set; }

    [StringLength(2000)]
    [Display(Name = "Findings")]
    public string? Findings { get; set; }
}
