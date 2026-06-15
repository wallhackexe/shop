using System.ComponentModel.DataAnnotations;

#nullable enable
namespace VesnaStore.Models;

public class CategorySizeTemplate
{
  [Key]
  public int TemplateID { get; set; }

  public int CategoryID { get; set; }

  public string LabelA { get; set; } = "Высота / Длина";

  public string LabelB { get; set; } = "Ширина / Обхват пояса";

  public string LabelC { get; set; } = "Обхват бедер / Рукав";

  public string BaseTemplateImagePath { get; set; }
}
