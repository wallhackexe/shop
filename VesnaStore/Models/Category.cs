// Decompiled with JetBrains decompiler
// Type: VesnaStore.Models.Category
// Assembly: VesnaStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AA7058AB-B655-449C-936F-A025188CFA05
// Assembly location: C:\Users\foget\Desktop\Новая папка (2)\VesnaStore.dll

using System.Collections.Generic;

#nullable enable
namespace VesnaStore.Models;

public class Category
{
  public int CategoryID { get; set; }

  public string Name { get; set; } = string.Empty;

  public virtual ICollection<Product> Products { get; set; } = (ICollection<Product>) new List<Product>();
}
