using System.ComponentModel.DataAnnotations.Schema;

#nullable enable
namespace VesnaStore.Models;

[Table("CartItems")]
public class CartItem
{
  public int CartItemID { get; set; }

  public int UserID { get; set; }

  public int ProductID { get; set; }

  public int Quantity { get; set; } = 1;

  public virtual Product? Product { get; set; }

  public virtual User? User { get; set; }
}
