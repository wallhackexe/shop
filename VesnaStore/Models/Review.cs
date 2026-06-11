// Decompiled with JetBrains decompiler
// Type: VesnaStore.Models.Review
// Assembly: VesnaStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AA7058AB-B655-449C-936F-A025188CFA05
// Assembly location: C:\Users\foget\Desktop\Новая папка (2)\VesnaStore.dll

using System;

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
