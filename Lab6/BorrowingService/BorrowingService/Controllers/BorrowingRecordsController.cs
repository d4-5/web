using Microsoft.AspNetCore.Mvc;
using BorrowingService.Models;
using BorrowingService.Repositories;
using BorrowingService.DTOs;
using System.Net;

namespace BorrowingService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BorrowingRecordsController : ControllerBase
    {
        private readonly IBorrowingRepository<BorrowingRecord> _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public BorrowingRecordsController(
            IBorrowingRepository<BorrowingRecord> repository,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BorrowingRecordDto>>> GetBorrowingRecords()
        {
            var records = await _repository.GetAllAsync();
            return Ok(records.Select(r => new BorrowingRecordDto
            {
                Id = r.Id,
                BookId = r.BookId,
                ReaderId = r.ReaderId,
                BorrowDate = r.BorrowDate,
                ReturnDate = r.ReturnDate
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BorrowingRecordDto>> GetBorrowingRecord(int id)
        {
            var record = await _repository.GetByIdAsync(id);
            if (record == null) return NotFound();

            return Ok(new BorrowingRecordDto
            {
                Id = record.Id,
                BookId = record.BookId,
                ReaderId = record.ReaderId,
                BorrowDate = record.BorrowDate,
                ReturnDate = record.ReturnDate
            });
        }

        [HttpPost]
        public async Task<ActionResult<BorrowingRecordDto>> PostBorrowingRecord(BorrowingRecordCreateDto recordDto)
        {
            var bookServiceUrl = _configuration["ServiceUrls:BookService"];
            var client = _httpClientFactory.CreateClient();
            var bookResponse = await client.GetAsync($"{bookServiceUrl}/api/Books/{recordDto.BookId}");
            if (bookResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return BadRequest($"Book with ID {recordDto.BookId} does not exist.");
            }

            var readersServiceUrl = _configuration["ServiceUrls:ReadersService"];
            var readerResponse = await client.GetAsync($"{readersServiceUrl}/api/Readers/{recordDto.ReaderId}");
            if (readerResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return BadRequest($"Reader with ID {recordDto.ReaderId} does not exist.");
            }

            var record = new BorrowingRecord
            {
                BookId = recordDto.BookId,
                ReaderId = recordDto.ReaderId,
                BorrowDate = recordDto.BorrowDate
            };

            await _repository.AddAsync(record);
            await _repository.SaveAsync();

            return CreatedAtAction(nameof(GetBorrowingRecord), new { id = record.Id }, new BorrowingRecordDto
            {
                Id = record.Id,
                BookId = record.BookId,
                ReaderId = record.ReaderId,
                BorrowDate = record.BorrowDate,
                ReturnDate = record.ReturnDate
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutBorrowingRecord(int id, BorrowingRecordUpdateDto recordDto)
        {
            var record = await _repository.GetByIdAsync(id);
            if (record == null) return NotFound();

            record.ReturnDate = recordDto.ReturnDate;

            _repository.Update(record);
            await _repository.SaveAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBorrowingRecord(int id)
        {
            var record = await _repository.GetByIdAsync(id);
            if (record == null) return NotFound();
            _repository.Delete(record);
            await _repository.SaveAsync();
            return NoContent();
        }

        [HttpDelete("book/{bookId}")]
        public async Task<IActionResult> DeleteByBookId(int bookId)
        {
            var records = await _repository.GetAllAsync();
            var toDelete = records.Where(r => r.BookId == bookId).ToList();
            foreach (var r in toDelete)
            {
                _repository.Delete(r);
            }
            await _repository.SaveAsync();
            return NoContent();
        }

        [HttpDelete("reader/{readerId}")]
        public async Task<IActionResult> DeleteByReaderId(int readerId)
        {
            var records = await _repository.GetAllAsync();
            var toDelete = records.Where(r => r.ReaderId == readerId).ToList();
            foreach (var r in toDelete)
            {
                _repository.Delete(r);
            }
            await _repository.SaveAsync();
            return NoContent();
        }
    }
}
