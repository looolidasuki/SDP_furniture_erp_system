using System;

namespace Sales_user.Models
{
    public class DeliveryNote
    {
        public long DeliveryNoteID { get; set; }
        public string DeliveryNoteCode { get; set; }
        public string ReplySlipCode { get; set; }
        public long CustomerID { get; set; }
        public long SalesOrderID { get; set; }
        public long StaffID { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? LastModifyDate { get; set; }
        public long WarehouseID { get; set; }
        public string ShipMethod { get; set; }
        public string TrackingNumber { get; set; }
        public string Remark { get; set; }
        public string SignedBy { get; set; }
        public DateTime? SignedDate { get; set; }
        public int Status { get; set; }
    }
}
