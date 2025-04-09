using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stock_Ease.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        public string Name { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string Email { get; set; } = null!;
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    }

    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public string? Barcode { get; set; }
        public int Quantity { get; set; }
        public double CurrentWeight { get; set; }
        public int MinimumThreshold { get; set; } = 0;
        public string ThresholdType { get; set; } = "Quantity"; // "Quantity" or "Weight"
        public DateTime? ExpiryDate { get; set; }
        public string? SensorId { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }

    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime TransactionDate { get; set; }
        public virtual User? User { get; set; }
        public virtual Product? Product { get; set; }
    }

    public class Report
    {
        [Key]
        public int ReportId { get; set; }
        public int UserId { get; set; }
        public string ReportType { get; set; } = null!;
        public DateTime GeneratedDate { get; set; }
        public virtual User User { get; set; } = null!;
    }

    public class Alert
    {
        [Key]
        public int AlertId { get; set; }
        public int ProductId { get; set; }
        public string Message { get; set; } = null!;
        public DateTime AlertDate { get; set; }
        public bool IsRead { get; set; }
        public virtual Product Product { get; set; } = null!;
    }
}
