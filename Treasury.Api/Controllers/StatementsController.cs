using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Treasury.Api.Data;
using Treasury.Api.Data.Entities;
using Treasury.Api.Services;

namespace Treasury.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatementsController : ControllerBase
{
    private readonly TreasuryDbContext _db;
    private readonly HashService _hash;

    public StatementsController(TreasuryDbContext db, HashService hash)
    {
        _db = db; _hash = hash;
    }

    // CSV row model (headers in your CSV must match these names)
    public class BankCsvRow
    {
        public string Date { get; set; } = "";
        public string Amount { get; set; } = "";
        public string Description { get; set; } = "";
        public string Reference { get; set; } = "";
        public string ExternalId { get; set; } = "";
    }

    // POST /api/statements/upload
    [HttpPost("upload")]
    public async Task<ActionResult> Upload([FromForm] IFormFile file, [FromForm] int bankAccountId)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var accountExists = await _db.BankAccounts.AnyAsync(x => x.Id == bankAccountId);
        if (!accountExists)
            return NotFound("Bank account not found");

        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null,
            MissingFieldFound = null
        };

        int imported = 0, skipped = 0;

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, cfg);

        var records = csv.GetRecords<BankCsvRow>();
        foreach (var row in records)
        {
            if (!TryParseDate(row.Date, out var dt)) { skipped++; continue; }
            if (!TryParseAmount(row.Amount, out var amt)) { skipped++; continue; }

            var refNorm = HashService.Normalize(row.Reference ?? "");
            var external = row.ExternalId?.Trim() ?? "";

            var dedup = _hash.Compute($"{bankAccountId}|{dt:yyyy-MM-dd}|{amt:F2}|{refNorm}|{external}");

            var already = await _db.BankTransactions.AnyAsync(t => t.DedupHash == dedup);
            if (already) { skipped++; continue; }

            var bt = new BankTransaction
            {
                BankAccountId = bankAccountId,
                Date = dt,
                Amount = amt,
                Description = row.Description ?? "",
                Reference = row.Reference ?? "",
                ExternalId = external,
                DedupHash = dedup
            };

            _db.BankTransactions.Add(bt);
            imported++;
        }

        await _db.SaveChangesAsync();
        return Ok(new { imported, skipped });
    }

    // Helpers inside the controller (so they are always in scope)
    private static bool TryParseDate(string input, out DateTime date)
    {
        var formats = new[]
        {
            "dd/MM/yy","dd/MM/yyyy","d/M/yyyy",
            "dd-MM-yyyy","d-M-yyyy",
            "yyyy-MM-dd"
        };

        if (DateTime.TryParseExact(input.Trim(), formats, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal, out date))
            return true;

        return DateTime.TryParse(input, new CultureInfo("en-IN"),
            DateTimeStyles.AssumeLocal, out date);
    }

    private static bool TryParseAmount(string input, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;

        // Handle ₹ symbol, Indian commas, and parentheses for negatives
        var cleaned = input.Trim()
            .Replace("₹", "", StringComparison.Ordinal)
            .Replace(",", "", StringComparison.Ordinal)
            .Replace("(", "-", StringComparison.Ordinal)
            .Replace(")", "", StringComparison.Ordinal);

        return decimal.TryParse(
            cleaned,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out amount
        );
    }
}
