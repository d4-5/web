namespace lab5.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        
        // Navigation property
        public ICollection<BorrowingRecord> BorrowingRecords { get; set; } = new List<BorrowingRecord>();
    }
}
