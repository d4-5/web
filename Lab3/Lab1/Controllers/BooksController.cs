using Lab1.Data;
using Lab1.Extensions;
using Lab1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab1.Controllers;

[Authorize]
public class BooksController : Controller
{
    private readonly LibraryContext _context;

    public BooksController(LibraryContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
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

    [AllowAnonymous]
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
        var draft = HttpContext.Session.GetObject<Book>(SessionKeys.BookCreateDraft);
        return View(draft ?? new Book());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Id,Title,Author,Isbn,Genre,PublishYear,TotalCopies,AvailableCopies")] Book book,
        string? submit)
    {
        if (string.Equals(submit, "draft", StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.SetObject(SessionKeys.BookCreateDraft, book);
            TempData["DraftMessage"] = "Чернетку книги збережено у сесії.";
            ModelState.Clear();
            return View(book);
        }

        if (await _context.Books.AnyAsync(b => b.Isbn == book.Isbn))
        {
            ModelState.AddModelError(nameof(book.Isbn), "Книга з таким ISBN уже існує.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(book);
            await _context.SaveChangesAsync();
            HttpContext.Session.Remove(SessionKeys.BookCreateDraft);
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.SetObject(SessionKeys.BookCreateDraft, book);
        return View(book);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var draft = HttpContext.Session.GetObject<Book>(SessionKeys.BookEditDraft(id.Value));
        if (draft != null)
        {
            return View(draft);
        }

        var book = await _context.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound();
        }

        HttpContext.Session.SetObject(SessionKeys.BookEditDraft(book.Id), book);
        return View(book);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,Title,Author,Isbn,Genre,PublishYear,TotalCopies,AvailableCopies")] Book book,
        string? submit)
    {
        if (id != book.Id)
        {
            return NotFound();
        }

        var sessionKey = SessionKeys.BookEditDraft(book.Id);
        if (string.Equals(submit, "draft", StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.SetObject(sessionKey, book);
            TempData["DraftMessage"] = "Чернетку змін книги збережено у сесії.";
            ModelState.Clear();
            return View(book);
        }

        if (await _context.Books.AnyAsync(b => b.Id != book.Id && b.Isbn == book.Isbn))
        {
            ModelState.AddModelError(nameof(book.Isbn), "Книга з таким ISBN уже існує.");
        }

        if (!ModelState.IsValid)
        {
            HttpContext.Session.SetObject(sessionKey, book);
            return View(book);
        }

        try
        {
            _context.Update(book);
            await _context.SaveChangesAsync();
            HttpContext.Session.Remove(sessionKey);
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
