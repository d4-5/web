using Lab1.Data;
using Lab1.Models;
using Lab1.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab1.Controllers;

public class ReadersController : Controller
{
    private readonly LibraryContext _context;

    public ReadersController(LibraryContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Readers.OrderBy(r => r.LastName).ThenBy(r => r.FirstName).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var reader = await _context.Readers
            .Include(r => r.Loans)
            .ThenInclude(l => l.Book)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (reader == null)
        {
            return NotFound();
        }

        return View(reader);
    }

    public IActionResult Create()
    {
        var draft = HttpContext.Session.GetObject<Reader>(SessionKeys.ReaderCreateDraft);
        return View(draft ?? new Reader());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Id,FirstName,LastName,Email,PhoneNumber,RegistrationDate,IsActive")] Reader reader,
        string? submit)
    {
        if (string.Equals(submit, "draft", StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.SetObject(SessionKeys.ReaderCreateDraft, reader);
            TempData["DraftMessage"] = "Чернетку читача збережено у сесії.";
            ModelState.Clear();
            return View(reader);
        }

        if (await _context.Readers.AnyAsync(r => r.Email == reader.Email))
        {
            ModelState.AddModelError(nameof(reader.Email), "Такий email уже існує.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(reader);
            await _context.SaveChangesAsync();
            HttpContext.Session.Remove(SessionKeys.ReaderCreateDraft);
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.SetObject(SessionKeys.ReaderCreateDraft, reader);
        return View(reader);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var draft = HttpContext.Session.GetObject<Reader>(SessionKeys.ReaderEditDraft(id.Value));
        if (draft != null)
        {
            return View(draft);
        }

        var reader = await _context.Readers.FindAsync(id);
        if (reader == null)
        {
            return NotFound();
        }

        HttpContext.Session.SetObject(SessionKeys.ReaderEditDraft(reader.Id), reader);
        return View(reader);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,FirstName,LastName,Email,PhoneNumber,RegistrationDate,IsActive")] Reader reader,
        string? submit)
    {
        if (id != reader.Id)
        {
            return NotFound();
        }

        var sessionKey = SessionKeys.ReaderEditDraft(reader.Id);
        if (string.Equals(submit, "draft", StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.SetObject(sessionKey, reader);
            TempData["DraftMessage"] = "Чернетку змін читача збережено у сесії.";
            ModelState.Clear();
            return View(reader);
        }

        if (await _context.Readers.AnyAsync(r => r.Id != reader.Id && r.Email == reader.Email))
        {
            ModelState.AddModelError(nameof(reader.Email), "Такий email уже існує.");
        }

        if (!ModelState.IsValid)
        {
            HttpContext.Session.SetObject(sessionKey, reader);
            return View(reader);
        }

        try
        {
            _context.Update(reader);
            await _context.SaveChangesAsync();
            HttpContext.Session.Remove(sessionKey);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ReaderExists(reader.Id))
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

        var reader = await _context.Readers
            .FirstOrDefaultAsync(m => m.Id == id);
        if (reader == null)
        {
            return NotFound();
        }

        return View(reader);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var reader = await _context.Readers.FindAsync(id);
        if (reader != null)
        {
            _context.Readers.Remove(reader);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Не можна видалити читача, оскільки є пов'язані записи видачі.";
                return RedirectToAction(nameof(Index));
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private bool ReaderExists(int id)
    {
        return _context.Readers.Any(e => e.Id == id);
    }
}
