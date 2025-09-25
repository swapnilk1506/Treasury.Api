using Microsoft.EntityFrameworkCore;
using Treasury.Api.Data.Entities;

namespace Treasury.Api.Data
{
    public class TreasuryDbContext : DbContext { public TreasuryDbContext(DbContextOptions<TreasuryDbContext> options) : base(options){ }
        public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
        public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
        public DbSet<LedgerTransaction> LedgerTransactions => Set<LedgerTransaction>();

        public DbSet<ReconciliationSession> ReconciliationSessions => Set<ReconciliationSession>();

        public DbSet<Match> Matches => Set<Match>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Prevent Duplicate Bank lines by unique hash
            modelBuilder.Entity<BankTransaction>().HasIndex(b=>b.DedupHash).IsUnique();

            //Seed one demo bank account to prevent null refs
            modelBuilder.Entity<BankAccount>().HasData(new BankAccount
            {
                Id = 1,
                Name = "Operations Account",
                BankName = "Demo Bank",
                AccountNumber = "000123456789",
                Currency = "INR",
                OpeningBalance = 1000.00M

            });
        }

    }
}
