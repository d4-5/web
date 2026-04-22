namespace Lab1.ViewModels;

public class ChatMessageViewModel
{
    public int Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string? RecipientId { get; set; }
    public string? RecipientName { get; set; }
    public string? Text { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime SentAt { get; set; }
    public string SentAtDisplay { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public string? AttachmentDownloadUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentContentType { get; set; }
    public string? AttachmentSizeDisplay { get; set; }
}
