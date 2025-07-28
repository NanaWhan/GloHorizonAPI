using System.ComponentModel.DataAnnotations;

namespace GloHorizonApi.Models.DomainModels;

public class Discount
{
    [Key]
    public int Id { get; set; }
    
    public string Code { get; set; }
    public decimal PercentageOff { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiryDate { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; } = 0;
}