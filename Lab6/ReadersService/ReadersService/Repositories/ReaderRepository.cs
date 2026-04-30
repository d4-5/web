using Microsoft.EntityFrameworkCore;
using ReadersService.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReadersService.Repositories
{
    public class ReaderRepository<T> : IReaderRepository<T> where T : class
    {
        protected readonly ReaderContext _context;
        protected readonly DbSet<T> _dbSet;

        public ReaderRepository(ReaderContext context)
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
