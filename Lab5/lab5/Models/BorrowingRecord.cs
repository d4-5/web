namespace lab5.Models
{
    public class BorrowingRecord
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int ReaderId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public Book? Book { get; set; }
        public Reader? Reader { get; set; }
    }
}
