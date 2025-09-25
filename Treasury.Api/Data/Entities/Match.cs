namespace Treasury.Api.Data.Entities
{
    public class Match
    {
        public int Id { get; set; }
        public int SessionId { get; set; }

        public ReconciliationSession? Session { get; set; }
        public int BankTransactionId { get; set; }

        public int LedgerTransactionId { get; set; }

        public string Strategy { get; set; } = "Exact";
        public double Confidence { get; set; } = 1.0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;



    }
}
