namespace Lab1.ViewModels;

public class AdminIndexViewModel
{
    public IReadOnlyCollection<string> AvailableRoles { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<AdminUserViewModel> Users { get; set; } = Array.Empty<AdminUserViewModel>();
}
