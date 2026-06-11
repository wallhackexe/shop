// Decompiled with JetBrains decompiler
// Type: VesnaStore.Models.OrderItem
// Assembly: VesnaStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AA7058AB-B655-449C-936F-A025188CFA05
// Assembly location: C:\Users\foget\Desktop\Новая папка (2)\VesnaStore.dll

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable enable
namespace VesnaStore.Models;

[Table("OrderItems")]
public class OrderItem
{
  [Key]
  public int OrderItemID { get; set; }

  public int OrderID { get; set; }

  public int ProductID { get; set; }

  public int Quantity { get; set; }

  [Column(TypeName = "decimal(18,2)")]
  public Decimal Price { get; set; }

  [ForeignKey("OrderID")]
  public Order? Order { get; set; }

  [ForeignKey("ProductID")]
  public Product? Product { get; set; }
}
