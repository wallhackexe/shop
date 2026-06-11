// Decompiled with JetBrains decompiler
// Type: VesnaStore.Models.Product
// Assembly: VesnaStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AA7058AB-B655-449C-936F-A025188CFA05
// Assembly location: C:\Users\foget\Desktop\Новая папка (2)\VesnaStore.dll

using System;
using System.Collections.Generic;

#nullable enable
namespace VesnaStore.Models;

public class Product
{
  public int ProductID { get; set; }

  public string Title { get; set; } = string.Empty;

  public string? Description { get; set; }

  public Decimal Price { get; set; }

  public string? ImageURL { get; set; }

  public string? AdditionalImages { get; set; }

  public int StockQuantity { get; set; }

  public int CategoryID { get; set; }

  public virtual Category? Category { get; set; }

  public int? BrandID { get; set; }

  public virtual Brand? Brand { get; set; }

  public bool IsPublished { get; set; }

  public bool IsNew { get; set; }

  public string? Sizes { get; set; }

  public string? Material { get; set; }

  public string? Color { get; set; }

  public string? ArticleNumber { get; set; }

  public string? Season { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.Now;

  public virtual ICollection<Review> Reviews { get; set; } = (ICollection<Review>) new List<Review>();

  public virtual ICollection<Favorite> Favorites { get; set; } = (ICollection<Favorite>) new List<Favorite>();

  public string? SizeValueA { get; set; }

  public int? StartX_A { get; set; }

  public int? StartY_A { get; set; }

  public int? EndX_A { get; set; }

  public int? EndY_A { get; set; }

  public int? TextX_A { get; set; }

  public int? TextY_A { get; set; }

  public string? SizeValueB { get; set; }

  public int? StartX_B { get; set; }

  public int? StartY_B { get; set; }

  public int? EndX_B { get; set; }

  public int? EndY_B { get; set; }

  public int? TextX_B { get; set; }

  public int? TextY_B { get; set; }

  public string? SizeValueC { get; set; }

  public int? StartX_C { get; set; }

  public int? StartY_C { get; set; }

  public int? EndX_C { get; set; }

  public int? EndY_C { get; set; }

  public int? TextX_C { get; set; }

  public int? TextY_C { get; set; }
}
