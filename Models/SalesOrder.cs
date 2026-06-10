using System;

namespace Sales_user.Models
{
    public class SalesOrder
    {
        public long SalesOrderID { get; set; }
        public string SalesOrderCode { get; set; }
        public long CustomerID { get; set; }
        public long StaffID { get; set; }
        public long CurrencyCurrencyID { get; set; }
        public decimal ExchangeRate { get; set; } = 1m;
        public decimal TotalAmount { get; set; }
        public decimal TotalAmountBase { get; set; }
        public string DeliveryAddress { get; set; }
        public DateTime? RequestedDeliveryDate { get; set; }
        public string CustomerRefNumber { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastModifyDate { get; set; }
        public string DiscountType { get; set; }
        public decimal Discount { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }
}
