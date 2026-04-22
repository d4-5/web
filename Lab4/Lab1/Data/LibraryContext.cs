using Lab1.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Lab1.Data;

public class LibraryContext : IdentityDbContext<ApplicationUser>
{
    public LibraryContext(DbContextOptions<LibraryContext> options) : base(options)
    {
    }

    public DbSet<Reader> Readers => Set<Reader>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Reader>()
            .HasIndex(r => r.Email)
            .IsUnique();

        modelBuilder.Entity<Book>()
            .HasIndex(b => b.Isbn)
            .IsUnique();

        modelBuilder.Entity<Loan>()
            .HasOne(l => l.Reader)
            .WithMany(r => r.Loans)
            .HasForeignKey(l => l.ReaderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Loan>()
            .HasOne(l => l.Book)
            .WithMany(b => b.Loans)
            .HasForeignKey(l => l.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChatMessage>()
            .Property(m => m.Text)
            .HasMaxLength(4000);

        modelBuilder.Entity<ChatMessage>()
            .Property(m => m.AttachmentOriginalFileName)
            .HasMaxLength(255);

        modelBuilder.Entity<ChatMessage>()
            .Property(m => m.AttachmentStoredFileName)
            .HasMaxLength(260);

        modelBuilder.Entity<ChatMessage>()
            .Property(m => m.AttachmentContentType)
            .HasMaxLength(255);

        modelBuilder.Entity<ChatMessage>()
            .HasIndex(m => m.SentAt);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.Recipient)
            .WithMany(u => u.ReceivedPrivateMessages)
            .HasForeignKey(m => m.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
