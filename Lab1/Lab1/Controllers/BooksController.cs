using Lab1.Data;
using Lab1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab1.Controllers;

public class BooksController : Controller
{
    private readonly LibraryContext _context;

    public BooksController(LibraryContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(b => b.Title.Contains(search) || b.Author.Contains(search));
        }

        ViewData["Search"] = search;
        return View(await query.OrderBy(b => b.Title).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books
            .Include(b => b.Loans)
            .ThenInclude(l => l.Reader)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }

    public IActionResult Create()
    {
        return View(new Book());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Title,Author,Isbn,Genre,PublishYear,TotalCopies,AvailableCopies")] Book book)
    {
        if (book.AvailableCopies > book.TotalCopies)
        {
            ModelState.AddModelError(nameof(book.AvailableCopies), "Кількість доступних примірників не може перевищувати загальну кількість.");
        }

        if (await _context.Books.AnyAsync(b => b.Isbn == book.Isbn))
        {
            ModelState.AddModelError(nameof(book.Isbn), "Книга з таким ISBN уже існує.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(book);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(book);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Author,Isbn,Genre,PublishYear,TotalCopies,AvailableCopies")] Book book)
    {
        if (id != book.Id)
        {
            return NotFound();
        }

        if (book.AvailableCopies > book.TotalCopies)
        {
            ModelState.AddModelError(nameof(book.AvailableCopies), "Кількість доступних примірників не може перевищувати загальну кількість.");
        }

        if (await _context.Books.AnyAsync(b => b.Id != book.Id && b.Isbn == book.Isbn))
        {
            ModelState.AddModelError(nameof(book.Isbn), "Книга з таким ISBN уже існує.");
        }

        if (!ModelState.IsValid)
        {
            return View(book);
        }

        try
        {
            _context.Update(book);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!BookExists(book.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var book = await _context.Books
            .FirstOrDefaultAsync(m => m.Id == id);
        if (book == null)
        {
            return NotFound();
        }

        return View(book);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book != null)
        {
            _context.Books.Remove(book);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Не можна видалити книгу, оскільки є пов'язані записи видачі.";
                return RedirectToAction(nameof(Index));
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private bool BookExists(int id)
    {
        return _context.Books.Any(e => e.Id == id);
    }
}
