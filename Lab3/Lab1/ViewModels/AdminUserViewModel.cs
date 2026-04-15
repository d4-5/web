namespace Lab1.ViewModels;

public class AdminUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
    public string SelectedRole { get; set; } = string.Empty;
    public bool IsCurrentUser { get; set; }
}
