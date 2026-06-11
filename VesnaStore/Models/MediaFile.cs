// Decompiled with JetBrains decompiler
// Type: VesnaStore.Models.MediaFile
// Assembly: VesnaStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AA7058AB-B655-449C-936F-A025188CFA05
// Assembly location: C:\Users\foget\Desktop\Новая папка (2)\VesnaStore.dll

using System;

#nullable enable
namespace VesnaStore.Models;

public class MediaFile
{
  public int Id { get; set; }

  public string Url { get; set; }

  public DateTime UploadedAt { get; set; } = DateTime.Now;
}
