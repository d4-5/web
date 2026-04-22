namespace lab5.Models
{
    public class Reader
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Navigation property
        public ICollection<BorrowingRecord> BorrowingRecords { get; set; } = new List<BorrowingRecord>();
    }
}
