using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Lab1.ViewModels;

public class SendChatMessageViewModel
{
    [StringLength(4000)]
    public string? Text { get; set; }
    public string? RecipientUserId { get; set; }
    public IFormFile? Attachment { get; set; }
}
