// Decompiled with JetBrains decompiler
// Type: VesnaStore.Models.Order
// Assembly: VesnaStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AA7058AB-B655-449C-936F-A025188CFA05
// Assembly location: C:\Users\foget\Desktop\Новая папка (2)\VesnaStore.dll

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable enable
namespace VesnaStore.Models;

[Table("Orders")]
public class Order
{
  [Key]
  public int OrderID { get; set; }

  public int UserID { get; set; }

  public DateTime OrderDate { get; set; }

  public Decimal TotalAmount { get; set; }

  public string Status { get; set; } = "Принят";

  public string? Phone { get; set; }

  public string? SocialLink { get; set; }

  public string? Address { get; set; }

  public User? User { get; set; }

  public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

  public string? TrackingNumber { get; set; }
}
