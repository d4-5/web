namespace Lab1.ViewModels;

public class ChatIndexViewModel
{
    public string CurrentUserId { get; set; } = string.Empty;
    public string CurrentUserName { get; set; } = string.Empty;
    public string? SelectedUserId { get; set; }
    public string? SelectedUserName { get; set; }
    public IReadOnlyList<ChatUserViewModel> Users { get; set; } = [];
    public IReadOnlyList<ChatMessageViewModel> GeneralMessages { get; set; } = [];
    public IReadOnlyList<ChatMessageViewModel> PrivateMessages { get; set; } = [];
}
