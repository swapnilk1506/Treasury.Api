using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Treasury.Api.Data.Entities
{
    public class BankAccount
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = " ";

        public string BankName { get; set; } = " ";

        [Required]
        public string AccountNumber { get; set; } = " ";

        public string Currency { get; set; } = "INR";

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningBalance { get; set; }


    }
}
