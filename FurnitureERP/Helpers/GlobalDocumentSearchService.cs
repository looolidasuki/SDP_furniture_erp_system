using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public sealed class DocumentSearchResult
    {
        public string DocumentType { get; set; }
        public string Code { get; set; }
        public long Id { get; set; }
        public string Module { get; set; }
        public string Summary { get; set; }
    }

    public static class GlobalDocumentSearchService
    {
        private static readonly Regex CodePattern = new Regex(
            @"^(SO|PO|PV|RV|INV|RF|QU|CU|GRN|DN|PTO|RS)-[\w-]+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool LooksLikeDocumentCode(string text)
        {
            text = (text ?? "").Trim();
            return text.Length >= 4 && CodePattern.IsMatch(text);
        }

        public static IList<DocumentSearchResult> Search(string query, int maxResults = 20)
        {
            var results = new List<DocumentSearchResult>();
            if (string.IsNullOrWhiteSpace(query))
                return results;

            query = query.Trim();
            if (!LooksLikeDocumentCode(query))
                return results;

            TryAdd(results, () => SearchSalesOrder(query));
            TryAdd(results, () => SearchPurchaseOrder(query));
            TryAdd(results, () => SearchPaymentVoucher(query));
            TryAdd(results, () => SearchReceiptVoucher(query));
            TryAdd(results, () => SearchInvoice(query));
            TryAdd(results, () => SearchRefund(query));

            if (results.Count > maxResults)
                return results.GetRange(0, maxResults);
            return results;
        }

        private static void TryAdd(List<DocumentSearchResult> results, Func<DocumentSearchResult> factory)
        {
            if (results.Count > 0) return;
            try
            {
                var hit = factory();
                if (hit != null) results.Add(hit);
            }
            catch { }
        }

        private static DocumentSearchResult SearchSalesOrder(string query)
        {
            var so = new SalesOrderController().GetByCode(query);
            if (so == null) return null;
            return new DocumentSearchResult
            {
                DocumentType = "Sales Order",
                Code = so.SalesOrderCode,
                Id = so.SalesOrderID,
                Module = "Sales Orders",
                Summary = $"Customer ID {so.CustomerID} | Status {so.Status}"
            };
        }

        private static DocumentSearchResult SearchPurchaseOrder(string query)
        {
            var po = new PurchaseOrderController().GetByCode(query);
            if (po == null) return null;
            return new DocumentSearchResult
            {
                DocumentType = "Purchase Order",
                Code = po.PurchaseOrderCode,
                Id = po.PurchaseOrderID,
                Module = "Purchase Orders",
                Summary = $"Supplier ID {po.SupplierID} | Status {po.Status}"
            };
        }

        private static DocumentSearchResult SearchPaymentVoucher(string query)
        {
            var pv = new PaymentVoucherController().GetByCode(query);
            if (pv == null) return null;
            return new DocumentSearchResult
            {
                DocumentType = "Payment Voucher",
                Code = pv.PaymentVoucherCode,
                Id = pv.PaymentVoucherID,
                Module = "Finance Dept",
                Summary = $"Supplier ID {pv.SupplierID} | {pv.Amount:N2}"
            };
        }

        private static DocumentSearchResult SearchReceiptVoucher(string query)
        {
            var rv = new ReceiptVoucherController().GetByCode(query);
            if (rv == null) return null;
            return new DocumentSearchResult
            {
                DocumentType = "Receipt Voucher",
                Code = rv.ReceiptVoucherCode,
                Id = rv.ReceiptVoucherID,
                Module = "Finance Dept",
                Summary = $"Customer ID {rv.CusomerID} | {rv.PaymentAmount:N2}"
            };
        }

        private static DocumentSearchResult SearchInvoice(string query)
        {
            var inv = new InvoiceController().GetByCode(query);
            if (inv == null) return null;
            return new DocumentSearchResult
            {
                DocumentType = "Invoice",
                Code = inv.InvoiceCode,
                Id = inv.InvoiceID,
                Module = "Invoices",
                Summary = $"Customer ID {inv.CustomerID}"
            };
        }

        private static DocumentSearchResult SearchRefund(string query)
        {
            var rf = new RefundRequestController().GetByCode(query);
            if (rf == null) return null;
            return new DocumentSearchResult
            {
                DocumentType = "Refund",
                Code = rf.RefundRequestCode,
                Id = rf.RefundRequestID,
                Module = "Refunds",
                Summary = $"Invoice ID {rf.InvoiceID} | {rf.RefundAmount:N2}"
            };
        }
    }
}
