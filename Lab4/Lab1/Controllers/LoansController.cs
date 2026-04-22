using Lab1.Data;
using Lab1.Models;
using Lab1.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lab1.Controllers;

[Authorize]
public class LoansController : Controller
{
    private readonly LibraryContext _context;

    public LoansController(LibraryContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var loans = await _context.Loans
            .Include(l => l.Book)
            .Include(l => l.Reader)
            .OrderByDescending(l => l.IssuedAt)
            .ToListAsync();

        return View(loans);
    }

    [AllowAnonymous]
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
        var draft = HttpContext.Session.GetObject<Loan>(SessionKeys.LoanCreateDraft);
        var model = draft ?? new Loan { IssuedAt = DateTime.Today, DueAt = DateTime.Today.AddDays(14), Status = LoanStatus.Active };
        await PopulateDropDowns(model.ReaderId, model.BookId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Id,ReaderId,BookId,IssuedAt,DueAt,ReturnedAt,Status,Notes")] Loan loan,
        string? submit)
    {
        if (string.Equals(submit, "draft", StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.SetObject(SessionKeys.LoanCreateDraft, loan);
            TempData["DraftMessage"] = "Чернетку видачі збережено у сесії.";
            ModelState.Clear();
            await PopulateDropDowns(loan.ReaderId, loan.BookId);
            return View(loan);
        }

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
            HttpContext.Session.SetObject(SessionKeys.LoanCreateDraft, loan);
            await PopulateDropDowns(loan.ReaderId, loan.BookId);
            return View(loan);
        }

        loan.Status = LoanStatus.Active;
        book!.AvailableCopies--;

        _context.Add(loan);
        await _context.SaveChangesAsync();
        HttpContext.Session.Remove(SessionKeys.LoanCreateDraft);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var draft = HttpContext.Session.GetObject<Loan>(SessionKeys.LoanEditDraft(id.Value));
        if (draft != null)
        {
            await PopulateDropDowns(draft.ReaderId, draft.BookId);
            return View(draft);
        }

        var loan = await _context.Loans.FindAsync(id);
        if (loan == null)
        {
            return NotFound();
        }

        HttpContext.Session.SetObject(SessionKeys.LoanEditDraft(loan.Id), loan);
        await PopulateDropDowns(loan.ReaderId, loan.BookId);
        return View(loan);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,ReaderId,BookId,IssuedAt,DueAt,ReturnedAt,Notes")] Loan editedLoan,
        string? submit)
    {
        if (id != editedLoan.Id)
        {
            return NotFound();
        }

        var sessionKey = SessionKeys.LoanEditDraft(editedLoan.Id);
        if (string.Equals(submit, "draft", StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.SetObject(sessionKey, editedLoan);
            TempData["DraftMessage"] = "Чернетку змін видачі збережено у сесії.";
            ModelState.Clear();
            await PopulateDropDowns(editedLoan.ReaderId, editedLoan.BookId);
            return View(editedLoan);
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
            HttpContext.Session.SetObject(sessionKey, editedLoan);
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
                HttpContext.Session.SetObject(sessionKey, editedLoan);
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
            HttpContext.Session.Remove(sessionKey);
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
