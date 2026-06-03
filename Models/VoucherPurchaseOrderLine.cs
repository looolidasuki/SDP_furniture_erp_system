using System;

namespace Sales_user.Models
{
    public class VoucherPurchaseOrderLine
    {
        public long PurchaseOrderID { get; set; }
        public string PurchaseOrderCode { get; set; }
        public DateTime? RequestDeliveryDate { get; set; }
        public int ClearingType { get; set; }
        public decimal PayAmount { get; set; }
    }
}
