using System.ComponentModel.DataAnnotations;

#nullable enable
namespace VesnaStore.Models;

public class ProductSizeValue
{
  [Key]
  public int ValueID { get; set; }

  public int ProductID { get; set; }

  public string ValueA { get; set; }

  public string ValueB { get; set; }

  public string ValueC { get; set; }
}
