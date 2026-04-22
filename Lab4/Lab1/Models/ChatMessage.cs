namespace Lab1.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public ApplicationUser Sender { get; set; } = null!;
    public string? RecipientId { get; set; }
    public ApplicationUser? Recipient { get; set; }
    public string? Text { get; set; }
    public string? AttachmentOriginalFileName { get; set; }
    public string? AttachmentStoredFileName { get; set; }
    public string? AttachmentContentType { get; set; }
    public long? AttachmentSize { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
