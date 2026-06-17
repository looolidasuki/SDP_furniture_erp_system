using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class StartupHealthChecks
    {
        public static void WarnIfDictionaryMissing()
        {
            try
            {
                string report = BuildDictionaryCoverageReport();
                if (string.IsNullOrWhiteSpace(report)) return;

                MessageBox.Show(
                    "Some status dictionary values are missing. UI may show raw numbers or lack status filters.\n\n" +
                    report +
                    "\n\nFix: insert missing rows into `systemdictionary` (or run Scripts/merge_new_database_patches.sql when applicable).",
                    "Dictionary Coverage Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch
            {
                // Never block startup on diagnostics.
            }
        }

        private static string BuildDictionaryCoverageReport()
        {
            // category -> SQL that returns a single column of distinct status codes
            var checks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DictionaryService.Categories.Quotation] = "SELECT DISTINCT status AS codeValue FROM quotation",
                [DictionaryService.Categories.SalesOrder] = "SELECT DISTINCT status AS codeValue FROM salesorder",
                [DictionaryService.Categories.PurchaseOrder] = "SELECT DISTINCT status AS codeValue FROM purchaseorder",
                [DictionaryService.Categories.Production] = "SELECT DISTINCT status AS codeValue FROM productionorder",
                [DictionaryService.Categories.Delivery] = "SELECT DISTINCT status AS codeValue FROM deliverynote",
                [DictionaryService.Categories.Invoice] = "SELECT DISTINCT status AS codeValue FROM invoice",
                [DictionaryService.Categories.PaymentVoucher] = "SELECT DISTINCT status AS codeValue FROM paymentvoucher",
                [DictionaryService.Categories.ReceiptVoucher] = "SELECT DISTINCT status AS codeValue FROM receiptvoucher",
                [DictionaryService.Categories.RefundStatus] = "SELECT DISTINCT status AS codeValue FROM refundrequest"
            };

            var missing = new List<string>();
            foreach (var kv in checks)
            {
                string category = kv.Key;
                var codes = SafeReadCodes(kv.Value);
                if (codes.Count == 0) continue;

                var mapped = SafeReadMappedCodes(category);
                foreach (var code in codes.OrderBy(c => c))
                {
                    if (!mapped.Contains(code))
                        missing.Add($"{category}: {code}");
                }
            }

            if (missing.Count == 0) return null;

            var sb = new StringBuilder();
            sb.AppendLine("Missing mappings:");
            foreach (var line in missing.Take(40))
                sb.AppendLine("- " + line);
            if (missing.Count > 40)
                sb.AppendLine($"... and {missing.Count - 40} more");
            return sb.ToString();
        }

        private static HashSet<int> SafeReadCodes(string sql)
        {
            var set = new HashSet<int>();
            try
            {
                DataTable dt = DatabaseConnect.ExecuteQuery(sql);
                if (dt == null || dt.Rows.Count == 0) return set;
                foreach (DataRow row in dt.Rows)
                {
                    try { set.Add(Convert.ToInt32(row["codeValue"])); } catch { }
                }
            }
            catch { }
            return set;
        }

        private static HashSet<int> SafeReadMappedCodes(string category)
        {
            var set = new HashSet<int>();
            try
            {
                DataTable dt = DatabaseConnect.ExecuteQuery(
                    "SELECT codeValue FROM systemdictionary WHERE category = @cat",
                    new[] { new MySqlParameter("@cat", category) });
                if (dt == null || dt.Rows.Count == 0) return set;
                foreach (DataRow row in dt.Rows)
                {
                    try { set.Add(Convert.ToInt32(row["codeValue"])); } catch { }
                }
            }
            catch { }
            return set;
        }
    }
}

