// Decompiled with JetBrains decompiler
// Type: VesnaStore.Models.Favorite
// Assembly: VesnaStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AA7058AB-B655-449C-936F-A025188CFA05
// Assembly location: C:\Users\foget\Desktop\Новая папка (2)\VesnaStore.dll

#nullable enable
namespace VesnaStore.Models;

public class Favorite
{
  public int FavoriteID { get; set; }

  public int UserID { get; set; }

  public int ProductID { get; set; }

  public virtual User? User { get; set; }

  public virtual Product? Product { get; set; }
}
