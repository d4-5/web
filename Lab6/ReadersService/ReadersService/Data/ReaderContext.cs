using Microsoft.EntityFrameworkCore;
using ReadersService.Models;

namespace ReadersService.Data
{
    public class ReaderContext : DbContext
    {
        public ReaderContext(DbContextOptions<ReaderContext> options) : base(options)
        {
        }

        public DbSet<Reader> Readers { get; set; }
    }
}
