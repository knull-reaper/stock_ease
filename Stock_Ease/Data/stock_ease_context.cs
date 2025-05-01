using Microsoft.EntityFrameworkCore;
using Stock_Ease.Models;

namespace Stock_Ease.Data
{
    public class Stock_EaseContext : DbContext
    {
        public Stock_EaseContext(DbContextOptions<Stock_EaseContext> options) : base(options) { }

        public DbSet<User> Users
        {
            get;
            set;
        }
        public DbSet<Product> Products
        {
            get;
            set;
        }
        public DbSet<Transaction> Transactions
        {
            get;
            set;
        }
        public DbSet<Report> Reports
        {
            get;
            set;
        }
        public DbSet<Alert> Alerts
        {
            get;
            set;
        }
    }
}