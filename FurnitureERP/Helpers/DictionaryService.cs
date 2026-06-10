using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class DictionaryService
    {
        public static class Categories
        {
            public const string SalesOrder = "SALES_ORDER_STATUS";
            public const string Production = "PRODUCTION_STATUS";
            public const string PurchaseOrder = "PURCHASE_ORDER_STATUS";
            public const string Delivery = "DELIVERY_STATUS";
            public const string Invoice = "INVOICE_STATUS";
            public const string ReceiptVoucher = "RECEIPT_VOUCHER_STATUS";
            public const string PaymentVoucher = "PAYMENT_VOUCHER_STATUS";
            public const string Supplier = "SUPPLIER_STATUS";
            public const string Product = "PRODUCT_STATUS";
            public const string RawMaterial = "RAW_MATERIAL_STATUS";
            public const string Staff = "STAFF_STATUS";
            public const string Department = "DEPARTMENT";
            public const string StaffTitle = "STAFF_TITLE";
            public const string Quotation = "QUOTATION_STATUS";
            public const string ReplySlip = "REPLY_SLIP_STATUS";
            public const string PoPaymentType = "FINANCIAL_CLEARING_TYPE";
            public const string InvoiceType = "INVOICE_TYPE";
            public const string RefundMethod = "REFUND_METHOD";
            public const string RefundReason = "REFUND_REASON";
            public const string RefundStatus = "REFUND_STATUS";
            public const string PaymentTerm = "PAYMENT_TERM";
            public const string ShipMethod = "SHIP_METHOD";
        }

        private static readonly Dictionary<string, Dictionary<int, string>> _cache =
            new Dictionary<string, Dictionary<int, string>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, int[][]> _transitions =
            new Dictionary<string, int[][]>(StringComparer.OrdinalIgnoreCase)
            {
                [Categories.SalesOrder] = new[]
                {
                    new[] { 0, 1, 5 },
                    new[] { 1, 2, 5 },
                    new[] { 2, 3 },
                    new[] { 3, 4 },
                    new[] { 4 },
                    new[] { 5 }
                },
                [Categories.Production] = new[]
                {
                    new[] { 0, 1, 4 },
                    new[] { 1, 2, 4 },
                    new[] { 2, 3 },
                    new[] { 3 },
                    new[] { 4, 1 }
                },
                [Categories.PurchaseOrder] = new[]
                {
                    new[] { 0, 1, 7 },
                    new[] { 1, 2, 3, 7 },
                    new[] { 2, 4, 7 },
                    new[] { 3, 0 },
                    new[] { 4, 5, 7 },
                    new[] { 5, 6, 7 },
                    new[] { 6 },
                    new[] { 7 }
                },
                [Categories.Delivery] = new[]
                {
                    new[] { 0, 1, 4 },
                    new[] { 1, 2, 4 },
                    new[] { 2, 3, 4 },
                    new[] { 3 },
                    new[] { 4, 0 }
                },
                [Categories.Invoice] = new[]
                {
                    new[] { 0, 1, 2, 4 },
                    new[] { 1, 2, 4 },
                    new[] { 2 },
                    new[] { 3, 1, 2 },
                    new[] { 4 }
                },
                [Categories.ReceiptVoucher] = new[]
                {
                    new[] { 0, 1, 2 },
                    new[] { 1 },
                    new[] { 2, 0 }
                },
                [Categories.Quotation] = new[]
                {
                    new[] { 0, 1, 3, 5 },
                    new[] { 1, 2, 3, 4, 5 },
                    new[] { 2, 4 },
                    new[] { 3 },
                    new[] { 4 },
                    new[] { 5 }
                },
                [Categories.ReplySlip] = new[]
                {
                    new[] { 0, 1, 3 },
                    new[] { 1, 2, 3 },
                    new[] { 2 },
                    new[] { 3 }
                }
            };

        private static readonly Dictionary<int, string> _quotationFallback = new Dictionary<int, string>
        {
            [0] = "Draft",
            [1] = "Sent",
            [2] = "Accepted",
            [3] = "Rejected",
            [4] = "Converted",
            [5] = "Cancelled"
        };
        private static readonly Dictionary<int, string> _replySlipFallback = new Dictionary<int, string>
        {
            [0] = "Draft",
            [1] = "Sent",
            [2] = "Signed",
            [3] = "Rejected"
        };
        private static readonly Dictionary<int, string> _invoiceTypeFallback = new Dictionary<int, string>
        {
            [1] = "Deposit",
            [2] = "Normal"
        };
        private static readonly Dictionary<int, string> _invoiceStatusFallback = new Dictionary<int, string>
        {
            [0] = "Unpaid",
            [1] = "Partially Paid",
            [2] = "Fully Paid",
            [3] = "Overdue",
            [4] = "Voided"
        };
        private static readonly Dictionary<int, string> _refundMethodFallback = new Dictionary<int, string>
        {
            [1] = "Bank Transfer",
            [2] = "FPS",
            [3] = "Cheque",
            [4] = "TT",
            [5] = "PayPal",
            [6] = "Amazon Pay",
            [7] = "Taobao Pay"
        };
        private static readonly Dictionary<int, string> _refundReasonFallback = new Dictionary<int, string>
        {
            [1] = "Damage",
            [2] = "Wrong Shipment",
            [3] = "Sizing Issue",
            [4] = "Order Cancelled",
            [5] = "Customer Dissatisfaction"
        };
        private static readonly Dictionary<int, string> _refundReasonStorageKeys = new Dictionary<int, string>
        {
            [1] = "damage",
            [2] = "wrong_shipment",
            [3] = "sizing_issue",
            [4] = "order_cancelled",
            [5] = "customer_dissatisfaction"
        };
        private static readonly Dictionary<string, int> _refundReasonLegacyLookup =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["damage"] = 1,
                ["wrong shipment"] = 2,
                ["wrong_shipment"] = 2,
                ["sizing issue"] = 3,
                ["sizing_issue"] = 3,
                ["order cancelled"] = 4,
                ["order_cancelled"] = 4,
                ["customer dissatisfaction"] = 5,
                ["customer_dissatisfaction"] = 5
            };
        private static readonly Dictionary<int, string> _refundStatusFallback = new Dictionary<int, string>
        {
            [0] = "Draft",
            [1] = "Approved",
            [2] = "Paid",
            [3] = "Rejected",
            [4] = "Cancelled"
        };
        private static readonly Dictionary<int, string> _paymentTermFallback = new Dictionary<int, string>
        {
            [1] = "Cash",
            [2] = "30 Days",
            [3] = "60 Days",
            [4] = "90 Days"
        };
        private static readonly Dictionary<int, string> _shipMethodFallback = new Dictionary<int, string>
        {
            [0] = "Sea Freight",
            [1] = "Air Freight",
            [2] = "Land Transport",
            [3] = "Express Delivery",
            [4] = "Rail Freight",
            [5] = "In-house Delivery"
        };
        private static readonly Dictionary<int, string> _paymentVoucherFallback = new Dictionary<int, string>
        {
            [0] = "Draft",
            [1] = "Approved",
            [2] = "Paid",
            [3] = "Cancelled"
        };
        private static readonly Dictionary<int, string> _receiptVoucherFallback = new Dictionary<int, string>
        {
            [0] = "Draft",
            [1] = "Confirmed",
            [2] = "Cancelled"
        };
        private static readonly Dictionary<string, int> _shipMethodLegacyLookup =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["courier"] = 3,
                ["company truck"] = 5,
                ["customer pickup"] = 5,
                ["sea freight"] = 0,
                ["air freight"] = 1,
                ["express"] = 3,
                ["express delivery"] = 3,
                ["land transport"] = 2,
                ["rail freight"] = 4,
                ["in-house delivery"] = 5
            };

        public static void ClearCache() => _cache.Clear();

        public static string GetDisplayName(string category, int codeValue)
        {
            EnsureCategoryLoaded(category);
            if (_cache.TryGetValue(category, out var map) && map.TryGetValue(codeValue, out var name))
                return name;
            if (string.Equals(category, Categories.Quotation, StringComparison.OrdinalIgnoreCase)
                && _quotationFallback.TryGetValue(codeValue, out var qName))
                return qName;
            if (string.Equals(category, Categories.ReplySlip, StringComparison.OrdinalIgnoreCase)
                && _replySlipFallback.TryGetValue(codeValue, out var rsName))
                return rsName;
            if (string.Equals(category, Categories.InvoiceType, StringComparison.OrdinalIgnoreCase)
                && _invoiceTypeFallback.TryGetValue(codeValue, out var invTypeName))
                return invTypeName;
            if (string.Equals(category, Categories.Invoice, StringComparison.OrdinalIgnoreCase)
                && _invoiceStatusFallback.TryGetValue(codeValue, out var invStatusName))
                return invStatusName;
            if (string.Equals(category, Categories.RefundMethod, StringComparison.OrdinalIgnoreCase)
                && _refundMethodFallback.TryGetValue(codeValue, out var refundMethodName))
                return refundMethodName;
            if (string.Equals(category, Categories.RefundReason, StringComparison.OrdinalIgnoreCase)
                && _refundReasonFallback.TryGetValue(codeValue, out var refundReasonName))
                return refundReasonName;
            if (string.Equals(category, Categories.RefundStatus, StringComparison.OrdinalIgnoreCase)
                && _refundStatusFallback.TryGetValue(codeValue, out var refundStatusName))
                return refundStatusName;
            if (string.Equals(category, Categories.PaymentTerm, StringComparison.OrdinalIgnoreCase)
                && _paymentTermFallback.TryGetValue(codeValue, out var paymentTermName))
                return paymentTermName;
            if (string.Equals(category, Categories.ShipMethod, StringComparison.OrdinalIgnoreCase)
                && _shipMethodFallback.TryGetValue(codeValue, out var shipMethodName))
                return shipMethodName;
            if (string.Equals(category, Categories.PaymentVoucher, StringComparison.OrdinalIgnoreCase)
                && _paymentVoucherFallback.TryGetValue(codeValue, out var pvName))
                return pvName;
            if (string.Equals(category, Categories.ReceiptVoucher, StringComparison.OrdinalIgnoreCase)
                && _receiptVoucherFallback.TryGetValue(codeValue, out var rvName))
                return rvName;
            return codeValue.ToString();
        }

        public static string FormatShipMethod(string storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue)) return "";
            if (int.TryParse(storedValue.Trim(), out int code))
                return GetDisplayName(Categories.ShipMethod, code);
            return storedValue.Trim();
        }

        public static int? ResolveShipMethodCode(string storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue)) return null;
            storedValue = storedValue.Trim();
            if (int.TryParse(storedValue, out int code))
                return code;

            foreach (var item in GetItems(Categories.ShipMethod))
            {
                if (string.Equals(item.Value, storedValue, StringComparison.OrdinalIgnoreCase))
                    return item.Key;
            }

            if (_shipMethodLegacyLookup.TryGetValue(storedValue, out int legacyCode))
                return legacyCode;

            return null;
        }

        public static DataTable DecorateShipMethodColumn(DataTable source, string columnName = "Ship Method")
        {
            if (source == null || !source.Columns.Contains(columnName))
                return source;

            foreach (DataRow row in source.Rows)
            {
                if (row[columnName] == DBNull.Value) continue;
                row[columnName] = FormatShipMethod(row[columnName]?.ToString());
            }
            return source;
        }

        public static string GetRefundReasonStorageKey(int codeValue)
        {
            if (_refundReasonStorageKeys.TryGetValue(codeValue, out var key))
                return key;
            return codeValue.ToString();
        }

        public static int? ResolveRefundReasonCode(string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return null;
            if (int.TryParse(stored.Trim(), out int numeric))
                return numeric;
            string trimmed = stored.Trim();
            if (_refundReasonLegacyLookup.TryGetValue(trimmed, out int legacy))
                return legacy;
            string normalized = trimmed.ToLowerInvariant().Replace(" ", "_");
            if (_refundReasonLegacyLookup.TryGetValue(normalized, out legacy))
                return legacy;
            return null;
        }

        public static string GetRefundReasonDisplay(string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return "";
            var code = ResolveRefundReasonCode(stored);
            if (code.HasValue)
                return GetDisplayName(Categories.RefundReason, code.Value);
            return stored;
        }

        public static IList<KeyValuePair<int, string>> GetItems(string category)
        {
            EnsureCategoryLoaded(category);
            if (_cache.TryGetValue(category, out var map) && map.Count > 0)
                return map.OrderBy(x => x.Key).Select(x => new KeyValuePair<int, string>(x.Key, x.Value)).ToList();

            if (string.Equals(category, Categories.Quotation, StringComparison.OrdinalIgnoreCase))
                return _quotationFallback.Select(x => new KeyValuePair<int, string>(x.Key, x.Value)).ToList();
            if (string.Equals(category, Categories.ReplySlip, StringComparison.OrdinalIgnoreCase))
                return _replySlipFallback.Select(x => new KeyValuePair<int, string>(x.Key, x.Value)).ToList();
            if (string.Equals(category, Categories.InvoiceType, StringComparison.OrdinalIgnoreCase))
                return _invoiceTypeFallback.Select(x => new KeyValuePair<int, string>(x.Key, x.Value)).ToList();
            if (string.Equals(category, Categories.Invoice, StringComparison.OrdinalIgnoreCase))
                return _invoiceStatusFallback.Select(x => new KeyValuePair<int, string>(x.Key, x.Value)).ToList();
            if (string.Equals(category, Categories.RefundMethod, StringComparison.OrdinalIgnoreCase))
                return _refundMethodFallback.Select(x => new KeyValuePair<int, string>(x.Key, x.Value)).ToList();
            if (string.Equals(category, Categories.RefundReason, StringComparison.OrdinalIgnoreCase))
                return _refundReasonFallback.Select(x => new KeyValuePair<int, string>(x.Key, x.Value)).ToList();
            if (string.Equals(category, Categories.RefundStatus, StringComparison.OrdinalIgnoreCase))
                return _refundStatusFallback.Select(x => new KeyValuePair<int, string>(x.Key, x.Value)).ToList();
            if (string.Equals(category, Categories.PaymentTerm, StringComparison.OrdinalIgnoreCase))
                return _paymentTermFallback.Select(x => new KeyValuePair<int, string>(x.Key, x.Value)).ToList();

            return new List<KeyValuePair<int, string>>();
        }

        public static bool CanTransition(string category, int fromStatus, int toStatus)
        {
            if (fromStatus == toStatus) return true;
            if (!_transitions.TryGetValue(category, out var edges))
                return true;

            if (fromStatus < 0 || fromStatus >= edges.Length)
                return false;

            return edges[fromStatus].Contains(toStatus);
        }

        public static string ValidateTransition(string category, int fromStatus, int toStatus)
        {
            if (CanTransition(category, fromStatus, toStatus))
                return null;
            return $"Cannot change status from '{GetDisplayName(category, fromStatus)}' to '{GetDisplayName(category, toStatus)}'.";
        }

        public static DataTable DecorateStatusColumn(DataTable source, string statusColumn, string category, string labelColumn = "Status Label")
        {
            if (source == null || !source.Columns.Contains(statusColumn))
                return source;

            if (!source.Columns.Contains(labelColumn))
                source.Columns.Add(labelColumn, typeof(string));

            foreach (DataRow row in source.Rows)
            {
                if (row[statusColumn] == DBNull.Value) continue;
                int code = Convert.ToInt32(row[statusColumn]);
                row[labelColumn] = GetDisplayName(category, code);
            }
            return source;
        }

        private static void EnsureCategoryLoaded(string category)
        {
            if (string.IsNullOrWhiteSpace(category) || _cache.ContainsKey(category))
                return;

            var map = new Dictionary<int, string>();
            try
            {
                var dt = new SystemDictionaryController().GetByCategory(category);
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        int code = Convert.ToInt32(row["codeValue"]);
                        map[code] = row["displayNameEnglish"]?.ToString() ?? code.ToString();
                    }
                }
            }
            catch { }

            _cache[category] = map;
        }
    }
}
