using System;

namespace Sales_user.Models
{
    public class Quotation
    {
        public long QuotationID { get; set; }
        public string QuotationCode { get; set; }
        public int SequenceNumber { get; set; }
        public long StaffID { get; set; }
        public long CustomerID { get; set; }
        public long CurrencyID { get; set; }
        public decimal ExchangeRate { get; set; } = 1m;
        public decimal TotalAmount { get; set; }
        public decimal TotalAmountBase { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastModifyDate { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }
}
