// Decompiled with JetBrains decompiler
// Type: VesnaStore.Models.PromoCode
// Assembly: VesnaStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AA7058AB-B655-449C-936F-A025188CFA05
// Assembly location: C:\Users\foget\Desktop\Новая папка (2)\VesnaStore.dll

using System;
using System.ComponentModel.DataAnnotations;

#nullable enable
namespace VesnaStore.Models;

public class PromoCode
{
  [Key]
  public int PromoCodeID { get; set; }

  [Required]
  [StringLength(50)]
  public string Code { get; set; }

  [Required]
  public Decimal DiscountAmount { get; set; }

  [Required]
  public Decimal MinOrderAmount { get; set; }

  [Required]
  public int MaxUses { get; set; }

  public int UsedCount { get; set; }

  public bool IsActive { get; set; } = true;
}
