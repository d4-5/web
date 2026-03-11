using Lab1.Data;
using Lab1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lab1.Controllers;

public class LoansController : Controller
{
    private readonly LibraryContext _context;

    public LoansController(LibraryContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var loans = await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Reader)
            .OrderByDescending(l => l.IssuedAt)
            .ToListAsync();

        return View(loans);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var loan = await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Reader)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (loan == null)
        {
            return NotFound();
        }

        return View(loan);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateDropDowns();
        return View(new Loan { IssuedAt = DateTime.Today, DueAt = DateTime.Today.AddDays(14), Status = LoanStatus.Active });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,ReaderId,BookId,IssuedAt,DueAt,ReturnedAt,Status,Notes")] Loan loan)
    {
        var book = await _context.Books.FindAsync(loan.BookId);
        if (book == null)
        {
            ModelState.AddModelError(nameof(loan.BookId), "Книгу не знайдено.");
        }
        else if (book.AvailableCopies <= 0)
        {
            ModelState.AddModelError(nameof(loan.BookId), "Немає доступних примірників цієї книги.");
        }

        if (loan.ReturnedAt.HasValue)
        {
            ModelState.AddModelError(nameof(loan.ReturnedAt), "Під час створення видачі дата повернення має бути порожньою.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDowns(loan.ReaderId, loan.BookId);
            return View(loan);
        }

        loan.Status = LoanStatus.Active;
        book!.AvailableCopies--;

        _context.Add(loan);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var loan = await _context.Loans.FindAsync(id);
        if (loan == null)
        {
            return NotFound();
        }

        await PopulateDropDowns(loan.ReaderId, loan.BookId);
        return View(loan);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ReaderId,BookId,IssuedAt,DueAt,ReturnedAt,Notes")] Loan editedLoan)
    {
        if (id != editedLoan.Id)
        {
            return NotFound();
        }

        var existingLoan = await _context.Loans.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
        if (existingLoan == null)
        {
            return NotFound();
        }

        var oldBook = await _context.Books.FindAsync(existingLoan.BookId);
        var newBook = await _context.Books.FindAsync(editedLoan.BookId);

        if (oldBook == null || newBook == null)
        {
            ModelState.AddModelError(nameof(editedLoan.BookId), "Книгу не знайдено.");
        }

        if (existingLoan.ReturnedAt.HasValue && editedLoan.ReturnedAt.HasValue && existingLoan.ReturnedAt.Value.Date != editedLoan.ReturnedAt.Value.Date)
        {
            ModelState.AddModelError(nameof(editedLoan.ReturnedAt), "Повторне повернення заборонено: запис уже був повернений.");
        }

        if (existingLoan.ReturnedAt == null && editedLoan.ReturnedAt == null && existingLoan.BookId != editedLoan.BookId)
        {
            if (newBook != null && newBook.AvailableCopies <= 0)
            {
                ModelState.AddModelError(nameof(editedLoan.BookId), "Для нової книги немає доступних примірників.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDowns(editedLoan.ReaderId, editedLoan.BookId);
            return View(editedLoan);
        }

        if (existingLoan.ReturnedAt == null && editedLoan.ReturnedAt.HasValue)
        {
            if (newBook != null && newBook.AvailableCopies < newBook.TotalCopies)
            {
                newBook.AvailableCopies++;
            }
            editedLoan.Status = LoanStatus.Returned;
        }
        else if (existingLoan.ReturnedAt.HasValue && editedLoan.ReturnedAt == null)
        {
            if (newBook != null && newBook.AvailableCopies <= 0)
            {
                ModelState.AddModelError(nameof(editedLoan.BookId), "Немає доступних примірників для повторної активації видачі.");
                await PopulateDropDowns(editedLoan.ReaderId, editedLoan.BookId);
                return View(editedLoan);
            }

            if (newBook != null)
            {
                newBook.AvailableCopies--;
            }
        }
        else if (existingLoan.ReturnedAt == null && editedLoan.ReturnedAt == null && existingLoan.BookId != editedLoan.BookId)
        {
            if (oldBook != null && oldBook.AvailableCopies < oldBook.TotalCopies)
            {
                oldBook.AvailableCopies++;
            }

            if (newBook != null)
            {
                newBook.AvailableCopies--;
            }
        }

        editedLoan.Status = ResolveStatus(editedLoan);

        try
        {
            _context.Update(editedLoan);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!LoanExists(editedLoan.Id))
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

        var loan = await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Reader)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (loan == null)
        {
            return NotFound();
        }

        return View(loan);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var loan = await _context.Loans.FindAsync(id);
        if (loan != null)
        {
            if (loan.ReturnedAt == null)
            {
                var book = await _context.Books.FindAsync(loan.BookId);
                if (book != null && book.AvailableCopies < book.TotalCopies)
                {
                    book.AvailableCopies++;
                }
            }

            _context.Loans.Remove(loan);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private LoanStatus ResolveStatus(Loan loan)
    {
        if (loan.ReturnedAt.HasValue)
        {
            return LoanStatus.Returned;
        }

        return loan.DueAt.Date < DateTime.Today ? LoanStatus.Overdue : LoanStatus.Active;
    }

    private async Task PopulateDropDowns(int? selectedReaderId = null, int? selectedBookId = null)
    {
        var readers = await _context.Readers
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .Select(r => new { r.Id, Name = r.LastName + " " + r.FirstName })
            .ToListAsync();

        var books = await _context.Books
            .OrderBy(b => b.Title)
            .Select(b => new { b.Id, Name = b.Title + " - " + b.Author + " (Доступно: " + b.AvailableCopies + ")" })
            .ToListAsync();

        ViewData["ReaderId"] = new SelectList(readers, "Id", "Name", selectedReaderId);
        ViewData["BookId"] = new SelectList(books, "Id", "Name", selectedBookId);
    }

    private bool LoanExists(int id)
    {
        return _context.Loans.Any(e => e.Id == id);
    }
}
