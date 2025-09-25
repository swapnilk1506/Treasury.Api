using Microsoft.EntityFrameworkCore;
using Treasury.Api.Data;
using Treasury.Api.Data.Entities;
namespace Treasury.Api.Services
{
    public class AutoMatchService
    {
        private readonly TreasuryDbContext _db;
        public AutoMatchService(TreasuryDbContext db) => _db = db;

        private static bool Similar(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b))
            {
                return true;
            }
            var na = HashService.Normalize(a);
            var nb = HashService.Normalize(b);

            if (na.Length == 0 || nb.Length == 0)
            {
                return true;
            }
            return na.Contains(nb) || nb.Contains(na);
        }

        public async Task<int> RunAsync(int sessionId, int daysTolerance = 3, decimal amountTolerance = 0.01m)
        {
            var session = await _db.ReconciliationSessions.FindAsync(sessionId)
                ?? throw new InvalidOperationException("Session Not Found");
            var bankTxns = await _db.BankTransactions
                .Where(t => t.BankAccountId == session.BankAccountId && t.Date >= session.StartDate && t.Date <= session.EndDate && !t.Matched).ToListAsync();
            var ledgerTxns = await _db.LedgerTransactions
                .Where(t => t.BankAccountId == session.BankAccountId && t.Date >= session.StartDate.AddDays(-daysTolerance) && t.Date <= session.EndDate.AddDays(daysTolerance) && !t.Matched).ToListAsync();

            int matches = 0;
            foreach (var b in bankTxns)
            {
                var candidates = ledgerTxns
                    .Where(l => Math.Abs(l.Amount - b.Amount) <= amountTolerance && Math.Abs((l.Date - b.Date).TotalDays) <= daysTolerance &&
                    Similar(b.Reference, l.Reference)).ToList();
                if (candidates.Count == 0)
                {
                    continue;
                }
                var l = candidates.OrderBy(c => Math.Abs((c.Date - b.Date).TotalDays)).First();
                _db.Matches.Add(new Match
                {
                    SessionId = sessionId,
                    BankTransactionId = b.Id,
                    LedgerTransactionId = l.Id,
                    Strategy = candidates.Count == 1 ? "Exact+Ref" : "Exact+ClosesDate",
                    Confidence = candidates.Count == 1 ? 0.95 : 0.8
                });
                b.Matched = true;
                l.Matched = true;
                matches++;
            }
            session.Status = "AutoMatched";
            await _db.SaveChangesAsync();
            return matches;

        }

    }
}
