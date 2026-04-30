namespace BorrowingService.DTOs
{
    public class BorrowingRecordDto
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int ReaderId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
    }

    public class BorrowingRecordCreateDto
    {
        public int BookId { get; set; }
        public int ReaderId { get; set; }
        public DateTime BorrowDate { get; set; } = DateTime.Now;
    }

    public class BorrowingRecordUpdateDto
    {
        public DateTime? ReturnDate { get; set; }
    }
}
