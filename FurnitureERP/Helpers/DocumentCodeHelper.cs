namespace FurnitureERP.Helpers
{
    public static class DocumentCodeHelper
    {
        public const int DefaultPadWidth = 8;

        public static string Build(string prefix, long id, int padWidth = DefaultPadWidth)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return id.ToString();
            return prefix + "-" + id.ToString("D" + padWidth);
        }

        public static string FormatCustomerCode(long customerId)
        {
            return Build("CU", customerId, 9);
        }

        public static string FormatInvoiceCode(long invoiceId) => Build("INV", invoiceId);

        public static string FormatPaymentVoucherCode(long paymentVoucherId) => Build("PV", paymentVoucherId);

        public static string FormatReceiptVoucherCode(long receiptVoucherId) => Build("RV", receiptVoucherId);

        public static string FormatReplySlipFromDeliveryNote(string deliveryNoteCode)
        {
            if (string.IsNullOrWhiteSpace(deliveryNoteCode)) return null;
            const string prefix = "DN-";
            if (!deliveryNoteCode.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) return null;
            return "RS-" + deliveryNoteCode.Substring(prefix.Length);
        }

        public static string NormalizeScrCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            input = input.Trim();
            int idx = input.IndexOf("SCR-", System.StringComparison.OrdinalIgnoreCase);
            string candidate = idx >= 0 ? input.Substring(idx) : input;
            int end = candidate.IndexOf(' ');
            if (end < 0)
                end = candidate.IndexOf('\u2014');
            if (end < 0)
                end = candidate.IndexOf(" - ", System.StringComparison.Ordinal);
            if (end > 0)
                candidate = candidate.Substring(0, end);
            return candidate.Trim();
        }
    }
}
