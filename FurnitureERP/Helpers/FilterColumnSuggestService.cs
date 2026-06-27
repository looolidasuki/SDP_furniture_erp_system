using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class FilterColumnSuggestService
    {
        public static bool CanSuggest(string statusCategory, string columnName) =>
            !string.IsNullOrWhiteSpace(BuildSql(statusCategory, columnName));

        public static IEnumerable<string> Suggest(string statusCategory, string columnName, string prefix, int limit = 25)
        {
            string sql = BuildSql(statusCategory, columnName);
            if (string.IsNullOrWhiteSpace(sql)) return Array.Empty<string>();

            limit = SqlGuard.ClampLimit(limit, 50);
            prefix = (prefix ?? "").Trim();
            string like = string.IsNullOrEmpty(prefix)
                ? "%"
                : "%" + SqlGuard.EscapeLikeValue(prefix) + "%";

            var parameters = new[]
            {
                new MySqlParameter("@like", like),
                new MySqlParameter("@lim", limit)
            };

            var results = new List<string>();
            try
            {
                var dt = DatabaseConnect.ExecuteQuery(sql, parameters);
                if (dt == null) return results;
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    if (row[0] == null || row[0] == DBNull.Value) continue;
                    string value = row[0].ToString().Trim();
                    if (!string.IsNullOrEmpty(value))
                        results.Add(value);
                }
            }
            catch { }

            return results;
        }

        private static string BuildSql(string statusCategory, string columnName)
        {
            if (string.IsNullOrWhiteSpace(statusCategory) || string.IsNullOrWhiteSpace(columnName))
                return null;

            columnName = columnName.Trim();
            if (string.Equals(statusCategory, DictionaryService.Categories.Quotation, StringComparison.OrdinalIgnoreCase))
            {
                if (columnName.Equals("Quotation Code", StringComparison.OrdinalIgnoreCase))
                    return "SELECT DISTINCT q.quotationCode FROM quotation q WHERE q.quotationCode LIKE @like ESCAPE '\\\\' ORDER BY q.quotationCode LIMIT @lim";
                if (columnName.Equals("Customer", StringComparison.OrdinalIgnoreCase))
                    return @"SELECT DISTINCT c.customerName FROM quotation q
                             INNER JOIN Customer c ON q.customerID = c.customerID
                             WHERE c.customerName LIKE @like ESCAPE '\\\\' ORDER BY c.customerName LIMIT @lim";
                if (columnName.Equals("Currency", StringComparison.OrdinalIgnoreCase))
                    return @"SELECT DISTINCT cur.currencyCode FROM quotation q
                             LEFT JOIN Currency cur ON q.currencyID = cur.currencyID
                             WHERE cur.currencyCode LIKE @like ESCAPE '\\\\' ORDER BY cur.currencyCode LIMIT @lim";
            }

            if (string.Equals(statusCategory, DictionaryService.Categories.SalesOrder, StringComparison.OrdinalIgnoreCase))
            {
                if (columnName.Equals("Order Code", StringComparison.OrdinalIgnoreCase)
                    || columnName.Equals("Sales Order Code", StringComparison.OrdinalIgnoreCase))
                    return "SELECT DISTINCT so.salesOrderCode FROM salesorder so WHERE so.salesOrderCode LIKE @like ESCAPE '\\\\' ORDER BY so.salesOrderCode LIMIT @lim";
                if (columnName.Equals("Customer", StringComparison.OrdinalIgnoreCase))
                    return @"SELECT DISTINCT c.customerName FROM salesorder so
                             INNER JOIN Customer c ON so.customerID = c.customerID
                             WHERE c.customerName LIKE @like ESCAPE '\\\\' ORDER BY c.customerName LIMIT @lim";
                if (columnName.Equals("Currency", StringComparison.OrdinalIgnoreCase))
                    return @"SELECT DISTINCT cur.currencyCode FROM salesorder so
                             LEFT JOIN Currency cur ON so.currencyID = cur.currencyID
                             WHERE cur.currencyCode LIKE @like ESCAPE '\\\\' ORDER BY cur.currencyCode LIMIT @lim";
            }

            if (string.Equals(statusCategory, DictionaryService.Categories.PurchaseOrder, StringComparison.OrdinalIgnoreCase))
            {
                if (columnName.Equals("Purchase Order Code", StringComparison.OrdinalIgnoreCase))
                    return "SELECT DISTINCT po.purchaseOrderCode FROM purchaseorder po WHERE po.purchaseOrderCode LIKE @like ESCAPE '\\\\' ORDER BY po.purchaseOrderCode LIMIT @lim";
                if (columnName.Equals("Supplier", StringComparison.OrdinalIgnoreCase))
                    return @"SELECT DISTINCT s.supplierName FROM purchaseorder po
                             INNER JOIN Supplier s ON po.supplierID = s.supplierID
                             WHERE s.supplierName LIKE @like ESCAPE '\\\\' ORDER BY s.supplierName LIMIT @lim";
                if (columnName.Equals("Currency", StringComparison.OrdinalIgnoreCase))
                    return @"SELECT DISTINCT cur.currencyCode FROM purchaseorder po
                             LEFT JOIN Currency cur ON po.currencyID = cur.currencyID
                             WHERE cur.currencyCode LIKE @like ESCAPE '\\\\' ORDER BY cur.currencyCode LIMIT @lim";
            }

            if (string.Equals(statusCategory, DictionaryService.Categories.Supplier, StringComparison.OrdinalIgnoreCase))
            {
                if (columnName.Equals("Supplier Name", StringComparison.OrdinalIgnoreCase))
                    return "SELECT DISTINCT supplierName FROM Supplier WHERE supplierName LIKE @like ESCAPE '\\\\' ORDER BY supplierName LIMIT @lim";
                if (columnName.Equals("Contact Person", StringComparison.OrdinalIgnoreCase))
                    return "SELECT DISTINCT contactPerson FROM Supplier WHERE contactPerson LIKE @like ESCAPE '\\\\' ORDER BY contactPerson LIMIT @lim";
                if (columnName.Equals("Phone", StringComparison.OrdinalIgnoreCase))
                    return "SELECT DISTINCT phone FROM Supplier WHERE phone LIKE @like ESCAPE '\\\\' ORDER BY phone LIMIT @lim";
                if (columnName.Equals("Email", StringComparison.OrdinalIgnoreCase))
                    return "SELECT DISTINCT email FROM Supplier WHERE email LIKE @like ESCAPE '\\\\' ORDER BY email LIMIT @lim";
                if (columnName.Equals("Supplier Code", StringComparison.OrdinalIgnoreCase))
                    return "SELECT DISTINCT CONCAT('SUP-', supplierID) FROM Supplier WHERE CONCAT('SUP-', supplierID) LIKE @like ESCAPE '\\\\' ORDER BY 1 LIMIT @lim";
            }

            if (string.Equals(statusCategory, DictionaryService.Categories.Invoice, StringComparison.OrdinalIgnoreCase))
            {
                if (columnName.Equals("Invoice Code", StringComparison.OrdinalIgnoreCase))
                    return "SELECT DISTINCT i.invoiceCode FROM invoice i WHERE i.invoiceCode LIKE @like ESCAPE '\\\\' ORDER BY i.invoiceCode LIMIT @lim";
                if (columnName.Equals("Customer", StringComparison.OrdinalIgnoreCase))
                    return @"SELECT DISTINCT c.customerName FROM invoice i
                             INNER JOIN Customer c ON i.customerID = c.customerID
                             WHERE c.customerName LIKE @like ESCAPE '\\\\' ORDER BY c.customerName LIMIT @lim";
                if (columnName.Equals("Currency", StringComparison.OrdinalIgnoreCase))
                    return @"SELECT DISTINCT cur.currencyCode FROM invoice i
                             LEFT JOIN Currency cur ON i.currencyID = cur.currencyID
                             WHERE cur.currencyCode LIKE @like ESCAPE '\\\\' ORDER BY cur.currencyCode LIMIT @lim";
            }

            return null;
        }
    }
}
