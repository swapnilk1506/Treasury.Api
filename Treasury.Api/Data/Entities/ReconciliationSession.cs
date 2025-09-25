namespace Treasury.Api.Data.Entities
{
    public class ReconciliationSession
    {
        public int Id { get; set; }
        public int BankAccountId { get; set; }
        public BankAccount? BankAccount { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //Allowed Values: "Open", "Automatched", "Closed"
        public string Status { get; set; } = "Open";


    }
}
