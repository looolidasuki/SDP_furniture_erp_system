using System;

namespace Sales_user.Models
{
    public class ReplySlip
    {
        public long ReplySlipID { get; set; }
        public string ReplySlipCode { get; set; }
        public long SalesOrderID { get; set; }
        public long CustomerID { get; set; }
        public long StaffID { get; set; }
        public long CurrencyID { get; set; }
        public string SignedBy { get; set; }
        public DateTime? SignedDate { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastModifyDate { get; set; }
    }
}
