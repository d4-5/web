using Microsoft.EntityFrameworkCore;
using BorrowingService.Models;

namespace BorrowingService.Data
{
    public class BorrowingContext : DbContext
    {
        public BorrowingContext(DbContextOptions<BorrowingContext> options) : base(options)
        {
        }

        public DbSet<BorrowingRecord> BorrowingRecords { get; set; }
    }
}
