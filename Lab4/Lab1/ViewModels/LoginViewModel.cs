using System.ComponentModel.DataAnnotations;

namespace Lab1.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Вкажіть email.")]
    [EmailAddress(ErrorMessage = "Невірний формат email.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Вкажіть пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Запам'ятати мене")]
    public bool RememberMe { get; set; }
}
