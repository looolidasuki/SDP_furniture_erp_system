using System;

namespace Sales_user.Models
{
    public class RawMaterialSupplierLine
    {
        public long SupplierId { get; set; }
        public string SupplierStyleNumber { get; set; }
        public decimal BasePrice { get; set; }
        public long CurrencyId { get; set; } = 1;
        public string Unit { get; set; }
        public int MinimumOrderQuantity { get; set; } = 1;
        public DateTime? QuoteDate { get; set; }
        public int Status { get; set; } = 1;
    }
}
