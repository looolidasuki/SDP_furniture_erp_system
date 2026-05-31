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
            public const string Quotation = "QUOTATION_STATUS";
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
                    new[] { 0, 1, 3 },
                    new[] { 1, 2, 3, 4 },
                    new[] { 2, 4 },
                    new[] { 3 },
                    new[] { 4 }
                }
            };

        private static readonly Dictionary<int, string> _quotationFallback = new Dictionary<int, string>
        {
            [0] = "Draft",
            [1] = "Sent",
            [2] = "Accepted",
            [3] = "Rejected",
            [4] = "Converted"
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
            return codeValue.ToString();
        }

        public static IList<KeyValuePair<int, string>> GetItems(string category)
        {
            EnsureCategoryLoaded(category);
            if (_cache.TryGetValue(category, out var map) && map.Count > 0)
                return map.OrderBy(x => x.Key).Select(x => new KeyValuePair<int, string>(x.Key, x.Value)).ToList();

            if (string.Equals(category, Categories.Quotation, StringComparison.OrdinalIgnoreCase))
                return _quotationFallback.Select(x => new KeyValuePair<int, string>(x.Key, x.Value)).ToList();

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
