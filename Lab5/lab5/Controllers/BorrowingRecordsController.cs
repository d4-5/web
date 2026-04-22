using Microsoft.AspNetCore.Mvc;
using lab5.Models;
using lab5.Repositories;
using lab5.DTOs;

namespace lab5.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BorrowingRecordsController : ControllerBase
    {
        private readonly IRepository<BorrowingRecord> _repository;

        public BorrowingRecordsController(IRepository<BorrowingRecord> repository)
        {
            _repository = repository;
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
    }
}
