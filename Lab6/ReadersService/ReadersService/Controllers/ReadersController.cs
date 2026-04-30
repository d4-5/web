using Microsoft.AspNetCore.Mvc;
using ReadersService.Models;
using ReadersService.Repositories;
using ReadersService.DTOs;

namespace ReadersService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReadersController : ControllerBase
    {
        private readonly IReaderRepository<Reader> _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ReadersController(
            IReaderRepository<Reader> repository,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReaderDto>>> GetReaders()
        {
            var readers = await _repository.GetAllAsync();
            return Ok(readers.Select(r => new ReaderDto
            {
                Id = r.Id,
                Name = r.Name,
                Email = r.Email
            }));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ReaderDto>> GetReader(int id)
        {
            var reader = await _repository.GetByIdAsync(id);
            if (reader == null) return NotFound();

            return Ok(new ReaderDto
            {
                Id = reader.Id,
                Name = reader.Name,
                Email = reader.Email
            });
        }

        [HttpPost]
        public async Task<ActionResult<ReaderDto>> PostReader(ReaderCreateUpdateDto readerDto)
        {
            var reader = new Reader
            {
                Name = readerDto.Name,
                Email = readerDto.Email
            };

            await _repository.AddAsync(reader);
            await _repository.SaveAsync();

            return CreatedAtAction(nameof(GetReader), new { id = reader.Id }, new ReaderDto
            {
                Id = reader.Id,
                Name = reader.Name,
                Email = reader.Email
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutReader(int id, ReaderCreateUpdateDto readerDto)
        {
            var reader = await _repository.GetByIdAsync(id);
            if (reader == null) return NotFound();

            reader.Name = readerDto.Name;
            reader.Email = readerDto.Email;

            _repository.Update(reader);
            await _repository.SaveAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReader(int id)
        {
            var reader = await _repository.GetByIdAsync(id);
            if (reader == null) return NotFound();

            var borrowingServiceUrl = _configuration["ServiceUrls:BorrowingService"];
            var client = _httpClientFactory.CreateClient();
            await client.DeleteAsync($"{borrowingServiceUrl}/api/BorrowingRecords/reader/{id}");

            _repository.Delete(reader);
            await _repository.SaveAsync();
            return NoContent();
        }
    }
}
