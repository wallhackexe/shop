// Decompiled with JetBrains decompiler
// Type: VesnaStore.Models.CartItem
// Assembly: VesnaStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AA7058AB-B655-449C-936F-A025188CFA05
// Assembly location: C:\Users\foget\Desktop\Новая папка (2)\VesnaStore.dll

using System.ComponentModel.DataAnnotations.Schema;

#nullable enable
namespace VesnaStore.Models;

[Table("CartItems")]
public class CartItem
{
  public int CartItemID { get; set; }

  public int UserID { get; set; }

  public int ProductID { get; set; }

  public int Quantity { get; set; } = 1;

  public virtual Product? Product { get; set; }

  public virtual User? User { get; set; }
}
