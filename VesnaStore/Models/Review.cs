#nullable enable
namespace VesnaStore.Models;

public class Review
{
  public int ReviewID { get; set; }

  public int ProductID { get; set; }

  public int UserID { get; set; }

  public string UserName { get; set; }

  public string Comment { get; set; }

  public int Rating { get; set; }

  public DateTime CreatedAt { get; set; }

  public virtual Product? Product { get; set; }

  public string? ReviewImageUrl { get; set; }

  public bool IsApproved { get; set; } = true;
}
