// Decompiled with JetBrains decompiler
// Type: VesnaStore.Models.User
// Assembly: VesnaStore, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: AA7058AB-B655-449C-936F-A025188CFA05
// Assembly location: C:\Users\foget\Desktop\Новая папка (2)\VesnaStore.dll

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable enable
namespace VesnaStore.Models;

public class User
{
  [Key]
  public int UserID { get; set; }

  [Required(ErrorMessage = "Введите Email")]
  [EmailAddress(ErrorMessage = "Некорректный адрес")]
  public string Email { get; set; } = string.Empty;

  public string PasswordHash { get; set; } = string.Empty;

  [NotMapped]
  [Required(ErrorMessage = "Введите пароль")]
  [DataType(DataType.Password)]
  public string Password { get; set; } = string.Empty;

  [NotMapped]
  [DataType(DataType.Password)]
  [Compare("Password", ErrorMessage = "Пароли не совпадают")]
  public string ConfirmPassword { get; set; } = string.Empty;

  [Required(ErrorMessage = "Введите ваше имя")]
  public string FullName { get; set; } = string.Empty;

  public string UserRole { get; set; } = nameof (User);

  public List<Order> Orders { get; set; } = new List<Order>();

  [NotMapped]
  [Range(typeof (bool), "true", "true", ErrorMessage = "Необходимо ваше согласие на обработку данных")]
  public bool AcceptTerms { get; set; }
}
