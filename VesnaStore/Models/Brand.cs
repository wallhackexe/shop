#nullable enable
namespace VesnaStore.Models;

public class Brand
{
  public int BrandID { get; set; }

  public string BrandName { get; set; } = string.Empty;

  public string? BrandCountry { get; set; }

  public virtual ICollection<Product>? Products { get; set; }
}
