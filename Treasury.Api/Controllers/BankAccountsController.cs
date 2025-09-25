using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Treasury.Api.Data;
using Treasury.Api.Data.Entities;

namespace Treasury.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BankAccountsController : ControllerBase
    {
        private readonly TreasuryDbContext _db;
        public BankAccountsController(TreasuryDbContext db)
        {
            _db = db;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BankAccount>>> Get() 
            => await _db.BankAccounts.AsNoTracking().ToListAsync();
        
    }
}
