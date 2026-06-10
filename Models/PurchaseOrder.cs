using System;

namespace Sales_user.Models
{
    public class PurchaseOrder
    {
        public long PurchaseOrderID { get; set; }
        public string PurchaseOrderCode { get; set; }
        public long SupplierID { get; set; }
        public long StaffID { get; set; }
        public long WarehouseID { get; set; }
        public long? RelatedShortageReport { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastModifyDate { get; set; }
        public DateTime RequestDeliveryDate { get; set; }
        public long CurrencyID { get; set; } = 1;
        public decimal ExchangeRate { get; set; } = 1m;
        public decimal TotalAmount { get; set; }
        public decimal TotalAmountBase { get; set; }
        public int Status { get; set; }
        public string Remark { get; set; }
    }
}
