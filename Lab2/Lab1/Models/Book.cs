using System.ComponentModel.DataAnnotations;

namespace Lab1.Models;

public class Book : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Isbn { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Genre { get; set; } = string.Empty;

    [Range(1450, 2100)]
    public int PublishYear { get; set; }

    [Range(1, int.MaxValue)]
    public int TotalCopies { get; set; }

    [Range(0, int.MaxValue)]
    public int AvailableCopies { get; set; }

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AvailableCopies > TotalCopies)
        {
            yield return new ValidationResult(
                "Кількість доступних примірників не може перевищувати загальну кількість.",
                new[] { nameof(AvailableCopies) });
        }
    }
}
