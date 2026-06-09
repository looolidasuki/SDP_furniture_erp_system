using System;
using FurnitureERP.Helpers;

namespace Sales_user.Models
{
    public class RawMaterialRequestNote
    {
        public long RawMaterialRequestNoteID { get; set; }
        public string RawMaterialRequestNoteCode { get; set; }
        public long ProductionOrderID { get; set; }
        public long StaffID { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime RequestDate { get; set; }
        public int Status { get; set; } = RawMaterialRequestNoteConstants.StatusDraft;
        public string Remark { get; set; }
    }
}
