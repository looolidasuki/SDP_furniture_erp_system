using System;

namespace Sales_user.Models
{
    public class Customer
    {
        public long CustomerID { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerRefNumber { get; set; }
        public string CustomerName { get; set; }
        public string BillingAddress { get; set; }
        public string PaymentTerm { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastModifyDate { get; set; }
    }
}
