using System.ComponentModel.DataAnnotations;

#nullable enable
namespace VesnaStore.Models;

public class UserNotification
{
  [Key]
  public int NotificationID { get; set; }

  public int UserID { get; set; }

  public string Title { get; set; }

  public string Message { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.Now;

  public bool IsRead { get; set; }
}
