using System;

namespace Sales_user.Models
{
    public class ReceiptVoucher
    {
        public long ReceiptVoucherID { get; set; }
        public string ReceiptVoucherCode { get; set; }

        // 💡 配合 SQL 改為少一個 t 的 cusomerID
        public long CusomerID { get; set; }

        public long StaffID { get; set; }

        // 💡 配合 SQL 欄位名 paymentAmount
        public decimal PaymentAmount { get; set; }

        /// <summary>付款方式（对应 paymentMethod varchar）</summary>
        public string PaymentMethodName { get; set; }
        public string PaymentMethodRef { get; set; }
        public string Remark { get; set; }
        public int Status { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? LastModifyDate { get; set; }

        // 💡 配合 SQL 的到帳日期與幣種
        public DateTime PaymentReceivedDate { get; set; }
        public long CurrencyID { get; set; }
    }
}