using System;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    /// <summary>
    /// Idempotent: inserts commonly missing systemdictionary rows so status labels and filters work.
    /// </summary>
    public static class DictionarySeedMigration
    {
        public static void EnsureApplied()
        {
            try
            {
                EnsureQuotationStatuses();
                EnsureRefundDictionaries();
                EnsureReplySlipStatuses();
                EnsurePaymentVoucherStatuses();
                EnsureReceiptVoucherStatuses();
            }
            catch
            {
                // Best-effort; startup health check will still warn if gaps remain.
            }
        }

        private static void EnsureQuotationStatuses()
        {
            // Base seed only had 0/1/3; app workflow also uses 2/4/5.
            EnsureEntry(DictionaryService.Categories.Quotation, 2, "Accepted", 4);
            EnsureEntry(DictionaryService.Categories.Quotation, 4, "Converted", 5);
            EnsureEntry(DictionaryService.Categories.Quotation, 5, "Cancelled", 6);
        }

        private static void EnsureReplySlipStatuses()
        {
            EnsureEntry(DictionaryService.Categories.ReplySlip, 0, "Draft", 1);
            EnsureEntry(DictionaryService.Categories.ReplySlip, 1, "Sent", 2);
            EnsureEntry(DictionaryService.Categories.ReplySlip, 2, "Signed", 3);
            EnsureEntry(DictionaryService.Categories.ReplySlip, 3, "Rejected", 4);
        }

        private static void EnsurePaymentVoucherStatuses()
        {
            EnsureEntry(DictionaryService.Categories.PaymentVoucher, 0, "Draft", 1);
            EnsureEntry(DictionaryService.Categories.PaymentVoucher, 1, "Approved", 2);
            EnsureEntry(DictionaryService.Categories.PaymentVoucher, 2, "Paid", 3);
            EnsureEntry(DictionaryService.Categories.PaymentVoucher, 3, "Cancelled", 4);
        }

        private static void EnsureReceiptVoucherStatuses()
        {
            EnsureEntry(DictionaryService.Categories.ReceiptVoucher, 0, "Draft", 1);
            EnsureEntry(DictionaryService.Categories.ReceiptVoucher, 1, "Confirmed", 2);
            EnsureEntry(DictionaryService.Categories.ReceiptVoucher, 2, "Cancelled", 3);
        }

        private static void EnsureRefundDictionaries()
        {
            EnsureEntry(DictionaryService.Categories.RefundStatus, 0, "Draft", 1);
            EnsureEntry(DictionaryService.Categories.RefundStatus, 1, "Approved", 2);
            EnsureEntry(DictionaryService.Categories.RefundStatus, 2, "Paid", 3);
            EnsureEntry(DictionaryService.Categories.RefundStatus, 3, "Rejected", 4);
            EnsureEntry(DictionaryService.Categories.RefundStatus, 4, "Cancelled", 5);

            EnsureEntry(DictionaryService.Categories.RefundMethod, 1, "Bank Transfer", 1);
            EnsureEntry(DictionaryService.Categories.RefundMethod, 2, "FPS", 2);
            EnsureEntry(DictionaryService.Categories.RefundMethod, 3, "Cheque", 3);
            EnsureEntry(DictionaryService.Categories.RefundMethod, 4, "TT", 4);
            EnsureEntry(DictionaryService.Categories.RefundMethod, 5, "PayPal", 5);
            EnsureEntry(DictionaryService.Categories.RefundMethod, 6, "Amazon Pay", 6);
            EnsureEntry(DictionaryService.Categories.RefundMethod, 7, "Taobao Pay", 7);

            EnsureEntry(DictionaryService.Categories.RefundReason, 1, "Damage", 1);
            EnsureEntry(DictionaryService.Categories.RefundReason, 2, "Wrong Shipment", 2);
            EnsureEntry(DictionaryService.Categories.RefundReason, 3, "Sizing Issue", 3);
            EnsureEntry(DictionaryService.Categories.RefundReason, 4, "Order Cancelled", 4);
            EnsureEntry(DictionaryService.Categories.RefundReason, 5, "Customer Dissatisfaction", 5);
        }

        private static void EnsureEntry(string category, int codeValue, string displayName, int sortOrder)
        {
            object exists = DatabaseConnect.ExecuteScalar(
                "SELECT COUNT(*) FROM systemdictionary WHERE category = @cat AND codeValue = @val",
                new[]
                {
                    new MySqlParameter("@cat", category),
                    new MySqlParameter("@val", codeValue)
                });
            if (exists != null && Convert.ToInt32(exists) > 0)
                return;

            object maxId = DatabaseConnect.ExecuteScalar("SELECT COALESCE(MAX(dictionaryID), 0) FROM systemdictionary");
            long nextId = Convert.ToInt64(maxId) + 1;

            DatabaseConnect.ExecuteNonQuery(
                @"INSERT INTO systemdictionary (dictionaryID, category, codeValue, displayNameEnglish, codePrefix, sortOrder)
                  VALUES (@id, @cat, @val, @name, NULL, @sort)",
                new[]
                {
                    new MySqlParameter("@id", nextId),
                    new MySqlParameter("@cat", category),
                    new MySqlParameter("@val", codeValue),
                    new MySqlParameter("@name", displayName),
                    new MySqlParameter("@sort", sortOrder)
                });
        }
    }
}
