using Lab1.Data;
using Lab1.Models;
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
        return View(new Reader());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,Email,PhoneNumber,RegistrationDate,IsActive")] Reader reader)
    {
        if (await _context.Readers.AnyAsync(r => r.Email == reader.Email))
        {
            ModelState.AddModelError(nameof(reader.Email), "Такий email уже існує.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(reader);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(reader);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var reader = await _context.Readers.FindAsync(id);
        if (reader == null)
        {
            return NotFound();
        }

        return View(reader);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Email,PhoneNumber,RegistrationDate,IsActive")] Reader reader)
    {
        if (id != reader.Id)
        {
            return NotFound();
        }

        if (await _context.Readers.AnyAsync(r => r.Id != reader.Id && r.Email == reader.Email))
        {
            ModelState.AddModelError(nameof(reader.Email), "Такий email уже існує.");
        }

        if (!ModelState.IsValid)
        {
            return View(reader);
        }

        try
        {
            _context.Update(reader);
            await _context.SaveChangesAsync();
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
