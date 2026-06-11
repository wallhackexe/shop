// Decompiled with JetBrains decompiler
// Type: VesnaStore.Models.UserNotification
// Assembly: VesnaStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AA7058AB-B655-449C-936F-A025188CFA05
// Assembly location: C:\Users\foget\Desktop\Новая папка (2)\VesnaStore.dll

using System;
using System.ComponentModel.DataAnnotations;

#nullable enable
namespace VesnaStore.Models;

public class UserNotification
{
  [Key]
  public int NotificationID { get; set; }

  public int UserID { get; set; }

  public string Title { get; set; }

  public string Message { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.Now;

  public bool IsRead { get; set; }
}
