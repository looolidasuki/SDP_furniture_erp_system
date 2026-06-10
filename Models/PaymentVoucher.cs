using System;
using System.Collections.Generic;

namespace Sales_user.Models
{
    public class PaymentVoucher
    {
        // ✨ 以下為用於 UI 顯示核銷採購單資訊的擴充屬性（不對應 paymentvoucher 主表欄位）
        public string PurchaseOrderCode { get; set; }          // 採購單編號 (例如: PO-20260001)
        public decimal PurchaseOrderTotalAmount { get; set; }  // 採購單原本的總金額
        public decimal VoucherPayAmount { get; set; }          // 本次憑證實際核銷/支付的金額（pvpo.payAmount）
        /// <summary>paymentvoucherpurchaseorder.type — 应付对账阶段（FINANCIAL_CLEARING_TYPE）</summary>
        public int ClearingType { get; set; }
        public long PaymentVoucherID { get; set; }
        public string PaymentVoucherCode { get; set; }

        // ✨ 新增：對應資料庫中的 supplierID，記錄付給哪位供應商（不可為空值 long）
        public long SupplierID { get; set; }

        // 💡 註解或刪除：因應資料庫欄位調整，不再查詢與插入 InvoiceID
        // public long? InvoiceID { get; set; }

        // 保留：對應關聯表中的採購單 ID
        public long? PurchaseOrderID { get; set; }
        public long StaffID { get; set; }
        public long CurrencyID { get; set; } = 1;
        public decimal ExchangeRate { get; set; } = 1m;
        public decimal Amount { get; set; }
        public decimal TotalAmountBase { get; set; }

        // ⚠️ 修改：配合資料庫中的類型，將 int 改為 string 
        // 這樣才能正常讀取與寫入資料庫的字串型態
        public string PaymentMethod { get; set; }

        public string PaymentRef { get; set; }
        public string Remark { get; set; }
        public int Status { get; set; }         // 0=Draft, 1=Approved, 2=Paid, 3=Cancelled
        public DateTime CreateDate { get; set; }
        public DateTime? LastModifyDate { get; set; }

        public List<VoucherPurchaseOrderLine> PurchaseOrderLines { get; set; }
    }
}