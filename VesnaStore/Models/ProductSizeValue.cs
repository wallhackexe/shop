// Decompiled with JetBrains decompiler
// Type: VesnaStore.Models.ProductSizeValue
// Assembly: VesnaStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AA7058AB-B655-449C-936F-A025188CFA05
// Assembly location: C:\Users\foget\Desktop\Новая папка (2)\VesnaStore.dll

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
