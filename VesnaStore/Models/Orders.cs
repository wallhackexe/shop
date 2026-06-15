using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable enable
namespace VesnaStore.Models;

[Table("Orders")]
public class Order
{
  [Key]
  public int OrderID { get; set; }

  public int UserID { get; set; }

  public DateTime OrderDate { get; set; }

  public Decimal TotalAmount { get; set; }

  public string Status { get; set; } = "Принят";

  public string? Phone { get; set; }

  public string? SocialLink { get; set; }

  public string? Address { get; set; }

  public User? User { get; set; }

  public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

  public string? TrackingNumber { get; set; }
}
