using System.ComponentModel.DataAnnotations;

namespace Lab1.Models;

public class Loan : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    public int ReaderId { get; set; }

    [Required]
    public int BookId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime IssuedAt { get; set; } = DateTime.Today;

    [Required]
    [DataType(DataType.Date)]
    public DateTime DueAt { get; set; } = DateTime.Today.AddDays(14);

    [DataType(DataType.Date)]
    public DateTime? ReturnedAt { get; set; }

    [Required]
    public LoanStatus Status { get; set; } = LoanStatus.Active;

    [StringLength(500)]
    public string? Notes { get; set; }

    public Reader? Reader { get; set; }
    public Book? Book { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DueAt.Date < IssuedAt.Date)
        {
            yield return new ValidationResult("Дата повернення має бути не раніше дати видачі.", new[] { nameof(DueAt) });
        }

        if (ReturnedAt.HasValue && ReturnedAt.Value.Date < IssuedAt.Date)
        {
            yield return new ValidationResult("Дата фактичного повернення не може бути раніше дати видачі.", new[] { nameof(ReturnedAt) });
        }
    }
}
