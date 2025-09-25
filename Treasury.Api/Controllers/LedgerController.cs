using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Treasury.Api.Data;
using Treasury.Api.Data.Entities;

namespace Treasury.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LedgerController : ControllerBase
    {
        private readonly TreasuryDbContext _db;
        public LedgerController(TreasuryDbContext db) => _db = db;

        [HttpGet("{bankAccountId}")]
        public async Task<ActionResult<IEnumerable<LedgerTransaction>>> GetByAccount(int bankAccountId) => await _db.LedgerTransactions.Where(x => x.BankAccountId == bankAccountId).OrderByDescending(x => x.Date).ToListAsync();
        public record CreateLedgerDto(int BankAccountId, DateTime Date, decimal Amount, string? Description, string? Reference);

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateLedgerDto dto)
        {
            var lt = new LedgerTransaction
            {
                BankAccountId = dto.BankAccountId,
                Date = dto.Date,
                Amount = dto.Amount,
                Description = dto.Description ?? " ",
                Reference = dto.Reference ?? " "
            };
            _db.LedgerTransactions.Add(lt);
            await _db.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetByAccount), new { bankAccountId = lt.BankAccountId }, lt);

        }
    }
}
