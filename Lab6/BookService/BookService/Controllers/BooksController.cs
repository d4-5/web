using Microsoft.AspNetCore.Mvc;
using BookService.Models;
using BookService.Repositories;
using BookService.DTOs;

namespace BookService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository<Book> _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public BooksController(
            IBookRepository<Book> repository,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookDto>>> GetBooks()
        {
            var books = await _repository.GetAllAsync();
            return Ok(books.Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                ISBN = b.ISBN
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookDto>> GetBook(int id)
        {
            var book = await _repository.GetByIdAsync(id);
            if (book == null) return NotFound();

            return Ok(new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                ISBN = book.ISBN
            });
        }

        [HttpPost]
        public async Task<ActionResult<BookDto>> PostBook(BookCreateUpdateDto bookDto)
        {
            var book = new Book
            {
                Title = bookDto.Title,
                Author = bookDto.Author,
                ISBN = bookDto.ISBN
            };

            await _repository.AddAsync(book);
            await _repository.SaveAsync();

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, new BookDto
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                ISBN = book.ISBN
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, BookCreateUpdateDto bookDto)
        {
            var book = await _repository.GetByIdAsync(id);
            if (book == null) return NotFound();

            book.Title = bookDto.Title;
            book.Author = bookDto.Author;
            book.ISBN = bookDto.ISBN;

            _repository.Update(book);
            await _repository.SaveAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _repository.GetByIdAsync(id);
            if (book == null) return NotFound();

            var borrowingServiceUrl = _configuration["ServiceUrls:BorrowingService"];
            var client = _httpClientFactory.CreateClient();
            await client.DeleteAsync($"{borrowingServiceUrl}/api/BorrowingRecords/book/{id}");

            _repository.Delete(book);
            await _repository.SaveAsync();
            return NoContent();
        }
    }
}
