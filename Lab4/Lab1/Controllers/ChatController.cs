using Lab1.Data;
using Lab1.Hubs;
using Lab1.Models;
using Lab1.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Lab1.Controllers;

[Authorize]
public class ChatController : Controller
{
    private const int MessagePageSize = 50;
    private const long MaxAttachmentSize = 10 * 1024 * 1024;

    private readonly LibraryContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IWebHostEnvironment _environment;

    public ChatController(
        LibraryContext context,
        UserManager<ApplicationUser> userManager,
        IHubContext<ChatHub> hubContext,
        IWebHostEnvironment environment)
    {
        _context = context;
        _userManager = userManager;
        _hubContext = hubContext;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? selectedUserId = null)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        var users = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.Id != currentUser.Id)
            .OrderBy(u => u.Email)
            .Select(u => new ChatUserViewModel
            {
                UserId = u.Id,
                DisplayName = u.Email ?? u.UserName ?? "User"
            })
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(selectedUserId) && users.All(u => u.UserId != selectedUserId))
        {
            selectedUserId = null;
        }

        var generalMessages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.RecipientId == null)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.SentAt)
            .Take(MessagePageSize)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        var privateMessages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(selectedUserId))
        {
            privateMessages = await _context.ChatMessages
                .AsNoTracking()
                .Where(m =>
                    m.RecipientId != null &&
                    ((m.SenderId == currentUser.Id && m.RecipientId == selectedUserId) ||
                     (m.SenderId == selectedUserId && m.RecipientId == currentUser.Id)))
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .OrderByDescending(m => m.SentAt)
                .Take(MessagePageSize)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        var selectedUserName = users.FirstOrDefault(u => u.UserId == selectedUserId)?.DisplayName;

        var viewModel = new ChatIndexViewModel
        {
            CurrentUserId = currentUser.Id,
            CurrentUserName = GetDisplayName(currentUser),
            SelectedUserId = selectedUserId,
            SelectedUserName = selectedUserName,
            Users = users,
            GeneralMessages = generalMessages.Select(MapMessage).ToList(),
            PrivateMessages = privateMessages.Select(MapMessage).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxAttachmentSize)]
    public async Task<IActionResult> SendMessage([FromForm] SendChatMessageViewModel model)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        var text = string.IsNullOrWhiteSpace(model.Text) ? null : model.Text.Trim();
        var attachment = model.Attachment;

        if (string.IsNullOrWhiteSpace(text) && (attachment == null || attachment.Length == 0))
        {
            return BadRequest(new { error = "Введіть текст повідомлення або додайте файл." });
        }

        ApplicationUser? recipient = null;
        if (!string.IsNullOrWhiteSpace(model.RecipientUserId))
        {
            if (model.RecipientUserId == currentUser.Id)
            {
                return BadRequest(new { error = "Не можна надсилати приватні повідомлення самому собі." });
            }

            recipient = await _userManager.Users
                .FirstOrDefaultAsync(u => u.Id == model.RecipientUserId);

            if (recipient == null)
            {
                return NotFound(new { error = "Одержувача не знайдено." });
            }
        }

        var message = new ChatMessage
        {
            SenderId = currentUser.Id,
            Text = text,
            RecipientId = recipient?.Id,
            SentAt = DateTime.UtcNow
        };

        string? savedFilePath = null;
        if (attachment is { Length: > 0 })
        {
            if (attachment.Length > MaxAttachmentSize)
            {
                return BadRequest(new { error = "Максимальний розмір файлу становить 10 МБ." });
            }

            var uploadsRoot = Path.Combine(_environment.ContentRootPath, "ChatUploads");
            Directory.CreateDirectory(uploadsRoot);

            var originalFileName = Path.GetFileName(attachment.FileName);
            var extension = Path.GetExtension(originalFileName);
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            savedFilePath = Path.Combine(uploadsRoot, storedFileName);

            await using var stream = new FileStream(savedFilePath, FileMode.Create);
            await attachment.CopyToAsync(stream);

            message.AttachmentOriginalFileName = originalFileName;
            message.AttachmentStoredFileName = storedFileName;
            message.AttachmentContentType = string.IsNullOrWhiteSpace(attachment.ContentType)
                ? "application/octet-stream"
                : attachment.ContentType;
            message.AttachmentSize = attachment.Length;
        }

        try
        {
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(savedFilePath) && System.IO.File.Exists(savedFilePath))
            {
                System.IO.File.Delete(savedFilePath);
            }

            throw;
        }

        message.Sender = currentUser;
        message.Recipient = recipient;

        var responseMessage = MapMessage(message);

        if (message.RecipientId == null)
        {
            await _hubContext.Clients.All.SendAsync("ReceivePublicMessage", responseMessage);
        }
        else
        {
            await _hubContext.Clients.Users(currentUser.Id, message.RecipientId)
                .SendAsync("ReceivePrivateMessage", responseMessage);
        }

        return Ok(new { message = responseMessage });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(int id, bool download = false)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        var message = await _context.ChatMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (message == null || string.IsNullOrWhiteSpace(message.AttachmentStoredFileName))
        {
            return NotFound();
        }

        if (message.RecipientId != null &&
            message.SenderId != currentUser.Id &&
            message.RecipientId != currentUser.Id)
        {
            return Forbid();
        }

        var filePath = Path.Combine(_environment.ContentRootPath, "ChatUploads", message.AttachmentStoredFileName);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var contentType = string.IsNullOrWhiteSpace(message.AttachmentContentType)
            ? "application/octet-stream"
            : message.AttachmentContentType;

        if (download)
        {
            return PhysicalFile(filePath, contentType, message.AttachmentOriginalFileName, enableRangeProcessing: true);
        }

        return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
    }

    private ChatMessageViewModel MapMessage(ChatMessage message)
    {
        var senderName = GetDisplayName(message.Sender);
        var recipientName = message.Recipient == null ? null : GetDisplayName(message.Recipient);
        var attachmentUrl = string.IsNullOrWhiteSpace(message.AttachmentStoredFileName)
            ? null
            : Url.Action(nameof(DownloadAttachment), new { id = message.Id });

        return new ChatMessageViewModel
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderName = senderName,
            RecipientId = message.RecipientId,
            RecipientName = recipientName,
            Text = message.Text,
            IsPrivate = !string.IsNullOrWhiteSpace(message.RecipientId),
            SentAt = message.SentAt,
            SentAtDisplay = message.SentAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
            AttachmentUrl = attachmentUrl,
            AttachmentDownloadUrl = attachmentUrl == null ? null : Url.Action(nameof(DownloadAttachment), new { id = message.Id, download = true }),
            AttachmentFileName = message.AttachmentOriginalFileName,
            AttachmentContentType = message.AttachmentContentType,
            AttachmentSizeDisplay = FormatFileSize(message.AttachmentSize)
        };
    }

    private static string GetDisplayName(ApplicationUser user)
    {
        return user.Email ?? user.UserName ?? "User";
    }

    private static string? FormatFileSize(long? bytes)
    {
        if (bytes is null or <= 0)
        {
            return null;
        }

        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes.Value;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.#} {units[unitIndex]}";
    }
}
