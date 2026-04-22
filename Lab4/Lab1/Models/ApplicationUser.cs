using Microsoft.AspNetCore.Identity;

namespace Lab1.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public ICollection<ChatMessage> SentMessages { get; set; } = [];
    public ICollection<ChatMessage> ReceivedPrivateMessages { get; set; } = [];
}
