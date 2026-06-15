#nullable enable
namespace VesnaStore.Models;


public class MediaFile
{
  public int Id { get; set; }

  public string Url { get; set; }

  public DateTime UploadedAt { get; set; } = DateTime.Now;
}
