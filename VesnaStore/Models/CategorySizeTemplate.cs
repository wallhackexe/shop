// Decompiled with JetBrains decompiler
// Type: VesnaStore.Models.CategorySizeTemplate
// Assembly: VesnaStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AA7058AB-B655-449C-936F-A025188CFA05
// Assembly location: C:\Users\foget\Desktop\Новая папка (2)\VesnaStore.dll

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
