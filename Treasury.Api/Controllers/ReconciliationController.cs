using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // for AnyAsync, ToListAsync
using Treasury.Api.Data;
using Treasury.Api.Data.Entities;
using Treasury.Api.Services;

namespace Treasury.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReconciliationController : ControllerBase
{
    private readonly TreasuryDbContext _db;
    private readonly AutoMatchService _auto;

    // DI constructor (assigns both non-nullable fields)
    public ReconciliationController(TreasuryDbContext db, AutoMatchService auto)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _auto = auto ?? throw new ArgumentNullException(nameof(auto));
    }

    // DTO MUST contain EndDate
    public record CreateSessionDto(int BankAccountId, DateTime StartDate, DateTime EndDate);

    // Create a new reconciliation session
    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionDto dto)
    {
        // AnyAsync requires: using Microsoft.EntityFrameworkCore;
        var exists = await _db.BankAccounts.AnyAsync(x => x.Id == dto.BankAccountId);
        if (!exists)
            return NotFound("Bank account not found");

        var session = new ReconciliationSession
        {
            BankAccountId = dto.BankAccountId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = "Open"
        };

        // Use the correct DbSet name from your DbContext: ReconciliationSessions
        _db.ReconciliationSessions.Add(session);
        await _db.SaveChangesAsync();

        // IActionResult avoids nullable generic warnings
        return Ok(session);
    }

    // Run auto-match for a session
    [HttpPost("sessions/{sessionId:int}/automatch")]
    public async Task<IActionResult> AutoMatch(int sessionId)
    {
        var s = await _db.ReconciliationSessions.FindAsync(sessionId);
        if (s is null) return NotFound("Session not found");

        var matched = await _auto.RunAsync(sessionId);
        return Ok(new { matched });
    }

    // Get unmatched bank and ledger lines for the session window
    [HttpGet("sessions/{sessionId:int}/unmatched")]
    public async Task<IActionResult> GetUnmatched(int sessionId)
    {
        var s = await _db.ReconciliationSessions.FindAsync(sessionId);
        if (s is null) return NotFound("Session not found");

        var bank = await _db.BankTransactions
            .Where(t => t.BankAccountId == s.BankAccountId
                        && t.Date >= s.StartDate && t.Date <= s.EndDate
                        && !t.Matched)
            .OrderBy(t => t.Date)
            .ToListAsync();

        var ledger = await _db.LedgerTransactions
            .Where(t => t.BankAccountId == s.BankAccountId
                        && t.Date >= s.StartDate && t.Date <= s.EndDate
                        && !t.Matched)
            .OrderBy(t => t.Date)
            .ToListAsync();

        return Ok(new { bank, ledger });
    }

    // List matches for a session
    [HttpGet("sessions/{sessionId:int}/matches")]
    public async Task<IActionResult> GetMatches(int sessionId)
    {
        var exists = await _db.ReconciliationSessions.AnyAsync(x => x.Id == sessionId);
        if (!exists) return NotFound("Session not found");

        var matches = await _db.Matches
            .Where(m => m.SessionId == sessionId)
            .ToListAsync();

        return Ok(matches);
    }

    // Manual match
    public record ManualMatchDto(int SessionId, int BankTransactionId, int LedgerTransactionId);

    [HttpPost("matches/manual")]
    public async Task<IActionResult> ManualMatch([FromBody] ManualMatchDto dto)
    {
        var s = await _db.ReconciliationSessions.FindAsync(dto.SessionId);
        if (s is null) return NotFound("Session not found");

        var b = await _db.BankTransactions.FindAsync(dto.BankTransactionId);
        var l = await _db.LedgerTransactions.FindAsync(dto.LedgerTransactionId);
        if (b is null || l is null) return NotFound("Transaction not found");
        if (b.Matched || l.Matched) return BadRequest("One of the transactions is already matched");

        _db.Matches.Add(new Match
        {
            SessionId = dto.SessionId,
            BankTransactionId = dto.BankTransactionId,
            LedgerTransactionId = dto.LedgerTransactionId,
            Strategy = "Manual",
            Confidence = 1.0
        });

        b.Matched = true;
        l.Matched = true;

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // Unmatch (delete a match)
    [HttpDelete("matches/{matchId:int}")]
    public async Task<IActionResult> Unmatch(int matchId)
    {
        var m = await _db.Matches.FindAsync(matchId);
        if (m is null) return NotFound();

        var b = await _db.BankTransactions.FindAsync(m.BankTransactionId);
        var l = await _db.LedgerTransactions.FindAsync(m.LedgerTransactionId);

        if (b is not null) b.Matched = false;
        if (l is not null) l.Matched = false;

        _db.Matches.Remove(m);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}