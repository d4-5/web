using Microsoft.EntityFrameworkCore;
using lab5.Models;

namespace lab5.Data
{
    public class LibraryContext : DbContext
    {
        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Reader> Readers { get; set; }
        public DbSet<BorrowingRecord> BorrowingRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BorrowingRecord>()
                .HasOne(b => b.Book)
                .WithMany(book => book.BorrowingRecords)
                .HasForeignKey(b => b.BookId);

            modelBuilder.Entity<BorrowingRecord>()
                .HasOne(b => b.Reader)
                .WithMany(reader => reader.BorrowingRecords)
                .HasForeignKey(b => b.ReaderId);
        }
    }
}
