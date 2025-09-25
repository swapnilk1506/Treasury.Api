using System.ComponentModel.DataAnnotations.Schema;

namespace Treasury.Api.Data.Entities
{
    public class LedgerTransaction
    {
        public int Id { get; set; }

        public int BankAccountId { get; set; }

        public BankAccount? BankAccount { get; set; }

        public DateTime Date { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string Description { get; set; } = " ";
        public string Reference { get; set; } = " ";
        public bool Matched { get; set; }
    }
}
