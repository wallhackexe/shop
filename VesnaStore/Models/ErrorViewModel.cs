// Decompiled with JetBrains decompiler
// Type: VesnaStore.Models.ErrorViewModel
// Assembly: VesnaStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AA7058AB-B655-449C-936F-A025188CFA05
// Assembly location: C:\Users\foget\Desktop\Новая папка (2)\VesnaStore.dll

#nullable enable
namespace VesnaStore.Models;

public class ErrorViewModel
{
  public string? RequestId { get; set; }

  public bool ShowRequestId => !string.IsNullOrEmpty(this.RequestId);
}
