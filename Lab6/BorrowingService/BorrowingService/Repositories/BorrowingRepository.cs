using Microsoft.EntityFrameworkCore;
using BorrowingService.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BorrowingService.Repositories
{
    public class BorrowingRepository<T> : IBorrowingRepository<T> where T : class
    {
        protected readonly BorrowingContext _context;
        protected readonly DbSet<T> _dbSet;

        public BorrowingRepository(BorrowingContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
