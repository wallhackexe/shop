#nullable enable
namespace VesnaStore.Models;

public class Category
{
  public int CategoryID { get; set; }

  public string Name { get; set; } = string.Empty;

  public virtual ICollection<Product> Products { get; set; } = (ICollection<Product>) new List<Product>();
}
