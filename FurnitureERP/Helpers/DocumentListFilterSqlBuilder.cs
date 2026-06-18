using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class DocumentListFilterSqlBuilder
    {
        private static readonly Dictionary<string, Dictionary<string, string>> ColumnMaps =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                [DictionaryService.Categories.Quotation] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Quotation Code"] = "q.quotationCode",
                    ["Customer"] = "c.customerName",
                    ["Currency"] = "cur.currencyCode",
                    ["Total"] = "q.totalAmount",
                    ["Total (HKD)"] = "q.totalAmountBase",
                    ["Rate"] = "q.exchangeRate",
                    ["Create Date"] = "q.createDate",
                    ["Status Label"] = "q.status",
                    ["Status"] = "q.status"
                },
                [DictionaryService.Categories.SalesOrder] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Order Code"] = "so.salesOrderCode",
                    ["Sales Order Code"] = "so.salesOrderCode",
                    ["Customer Ref Number"] = "so.customerReferenceNumber",
                    ["Customer"] = "c.customerName",
                    ["Currency"] = "cur.currencyCode",
                    ["Total"] = "so.totalAmount",
                    ["Total (HKD)"] = "so.totalAmountBase",
                    ["Rate"] = "so.exchangeRate",
                    ["Delivery Address"] = "so.deliveryAddress",
                    ["Create Date"] = "so.createDate",
                    ["Status Label"] = "so.status",
                    ["Status"] = "so.status"
                },
                [DictionaryService.Categories.PurchaseOrder] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Purchase Order Code"] = "po.purchaseOrderCode",
                    ["Supplier"] = "s.supplierName",
                    ["Currency"] = "cur.currencyCode",
                    ["Total"] = "po.totalAmount",
                    ["Total (HKD)"] = "po.totalAmountBase",
                    ["Create Date"] = "po.createDate",
                    ["Request Delivery Date"] = "po.requestDeliveryDate",
                    ["Status Label"] = "po.status",
                    ["Status"] = "po.status"
                },
                [DictionaryService.Categories.Invoice] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Invoice Code"] = "i.invoiceCode",
                    ["Customer"] = "c.customerName",
                    ["Sales Order"] = "so.salesOrderCode",
                    ["Currency"] = "cur.currencyCode",
                    ["Total"] = "i.totalAmount",
                    ["Total (HKD)"] = "i.totalAmountBase",
                    ["Invoice Type"] = "i.invoiceType",
                    ["Type Label"] = "i.invoiceType",
                    ["Create Date"] = "i.createDate",
                    ["Remark"] = "i.remark",
                    ["Status Label"] = "i.status",
                    ["Status"] = "i.status"
                }
            };

        public static string ResolveSqlColumn(string statusCategory, string uiColumn)
        {
            if (string.IsNullOrWhiteSpace(statusCategory) || string.IsNullOrWhiteSpace(uiColumn))
                return null;
            if (!ColumnMaps.TryGetValue(statusCategory, out var map))
                return null;
            return map.TryGetValue(uiColumn.Trim(), out var sql) ? sql : null;
        }

        public static void Apply(string statusCategory, DocumentListFilter filter,
            List<string> conditions, List<MySqlParameter> parameters)
        {
            if (filter == null) return;

            int seq = 0;
            if (filter.Conditions != null && filter.Conditions.Count > 0)
            {
                foreach (var cond in filter.Conditions)
                    ApplyCondition(statusCategory, cond, conditions, parameters, ref seq);
                return;
            }

            ApplyLegacyKeyword(statusCategory, filter, conditions, parameters, ref seq);
            string statusCol = ResolveSqlColumn(statusCategory, "Status");
            if (statusCol != null)
                SearchQueryHelper.AddStatus(conditions, parameters, statusCol, filter.Status);
        }

        private static void ApplyLegacyKeyword(string statusCategory, DocumentListFilter filter,
            List<string> conditions, List<MySqlParameter> parameters, ref int seq)
        {
            if (string.IsNullOrWhiteSpace(filter.Keyword)) return;

            string kw = "%" + SqlGuard.EscapeLikeValue(filter.Keyword.Trim()) + "%";
            string clause = BuildLegacyKeywordClause(statusCategory);
            if (string.IsNullOrWhiteSpace(clause)) return;

            string param = "@p" + seq++;
            conditions.Add(clause.Replace("@kw", param));
            parameters.Add(new MySqlParameter(param, kw));
        }

        private static string BuildLegacyKeywordClause(string statusCategory)
        {
            if (string.Equals(statusCategory, DictionaryService.Categories.Quotation, StringComparison.OrdinalIgnoreCase))
                return "(q.quotationCode LIKE @kw ESCAPE '\\\\' OR c.customerName LIKE @kw ESCAPE '\\\\')";
            if (string.Equals(statusCategory, DictionaryService.Categories.SalesOrder, StringComparison.OrdinalIgnoreCase))
                return "(so.salesOrderCode LIKE @kw ESCAPE '\\\\' OR c.customerName LIKE @kw ESCAPE '\\\\' OR so.customerReferenceNumber LIKE @kw ESCAPE '\\\\')";
            if (string.Equals(statusCategory, DictionaryService.Categories.PurchaseOrder, StringComparison.OrdinalIgnoreCase))
                return "(po.purchaseOrderCode LIKE @kw ESCAPE '\\\\' OR s.supplierName LIKE @kw ESCAPE '\\\\')";
            if (string.Equals(statusCategory, DictionaryService.Categories.Invoice, StringComparison.OrdinalIgnoreCase))
                return "(i.invoiceCode LIKE @kw ESCAPE '\\\\' OR c.customerName LIKE @kw ESCAPE '\\\\' OR so.salesOrderCode LIKE @kw ESCAPE '\\\\')";
            return null;
        }

        private static void ApplyCondition(string statusCategory, DocumentFilterCondition cond,
            List<string> conditions, List<MySqlParameter> parameters, ref int seq)
        {
            if (cond == null || string.IsNullOrWhiteSpace(cond.Column)) return;

            if (cond.StatusCode.HasValue)
            {
                string statusCol = ResolveSqlColumn(statusCategory, cond.Column);
                if (statusCol == null) return;
                string p = "@p" + seq++;
                conditions.Add($"{statusCol} = {p}");
                parameters.Add(new MySqlParameter(p, cond.StatusCode.Value));
                return;
            }

            if (TryResolveInvoiceTypeCode(statusCategory, cond, out int invoiceTypeCode))
            {
                string p = "@p" + seq++;
                conditions.Add($"i.invoiceType = {p}");
                parameters.Add(new MySqlParameter(p, invoiceTypeCode));
                return;
            }

            string sqlCol = ResolveSqlColumn(statusCategory, cond.Column);
            if (sqlCol == null) return;

            string op = cond.Operator ?? string.Empty;

            if (cond.DateFrom.HasValue || cond.DateTo.HasValue || op == "To")
                ApplyDateCondition(sqlCol, cond, conditions, parameters, ref seq);
            else if (cond.NumericValue.HasValue || cond.NumericValueTo.HasValue)
                ApplyNumericCondition(sqlCol, cond, conditions, parameters, ref seq);
            else if (!string.IsNullOrWhiteSpace(cond.TextValue))
                ApplyTextCondition(sqlCol, op, cond.TextValue, conditions, parameters, ref seq);
        }

        private static bool TryResolveInvoiceTypeCode(string statusCategory, DocumentFilterCondition cond, out int code)
        {
            code = 0;
            if (!string.Equals(statusCategory, DictionaryService.Categories.Invoice, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.Equals(cond.Column, "Type Label", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.Equals(cond.Operator, "Equals", StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.IsNullOrWhiteSpace(cond.TextValue)) return false;

            foreach (var item in DictionaryService.GetItems(DictionaryService.Categories.InvoiceType))
            {
                if (string.Equals(item.Value, cond.TextValue.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    code = item.Key;
                    return true;
                }
            }
            return false;
        }

        private static void ApplyTextCondition(string sqlCol, string op, string text,
            List<string> conditions, List<MySqlParameter> parameters, ref int seq)
        {
            string p = "@p" + seq++;
            string trimmed = text.Trim();
            if (string.Equals(op, "Equals", StringComparison.OrdinalIgnoreCase))
            {
                conditions.Add($"{sqlCol} = {p}");
                parameters.Add(new MySqlParameter(p, trimmed));
            }
            else if (string.Equals(op, "StartsWith", StringComparison.OrdinalIgnoreCase))
            {
                conditions.Add($"{sqlCol} LIKE {p} ESCAPE '\\\\'");
                parameters.Add(new MySqlParameter(p, SqlGuard.EscapeLikeValue(trimmed) + "%"));
            }
            else
            {
                conditions.Add($"{sqlCol} LIKE {p} ESCAPE '\\\\'");
                parameters.Add(new MySqlParameter(p, "%" + SqlGuard.EscapeLikeValue(trimmed) + "%"));
            }
        }

        private static void ApplyNumericCondition(string sqlCol, DocumentFilterCondition cond,
            List<string> conditions, List<MySqlParameter> parameters, ref int seq)
        {
            string op = cond.Operator ?? string.Empty;
            if (string.Equals(op, "Between", StringComparison.OrdinalIgnoreCase)
                && cond.NumericValue.HasValue && cond.NumericValueTo.HasValue)
            {
                decimal a = cond.NumericValue.Value;
                decimal b = cond.NumericValueTo.Value;
                if (b < a) (a, b) = (b, a);
                string pFrom = "@p" + seq++;
                string pTo = "@p" + seq++;
                conditions.Add($"{sqlCol} >= {pFrom} AND {sqlCol} <= {pTo}");
                parameters.Add(new MySqlParameter(pFrom, a));
                parameters.Add(new MySqlParameter(pTo, b));
                return;
            }

            if (!cond.NumericValue.HasValue) return;
            string p = "@p" + seq++;
            if (string.Equals(op, ">=", StringComparison.OrdinalIgnoreCase))
            {
                conditions.Add($"{sqlCol} >= {p}");
                parameters.Add(new MySqlParameter(p, cond.NumericValue.Value));
            }
            else if (string.Equals(op, "<=", StringComparison.OrdinalIgnoreCase))
            {
                conditions.Add($"{sqlCol} <= {p}");
                parameters.Add(new MySqlParameter(p, cond.NumericValue.Value));
            }
            else
            {
                conditions.Add($"{sqlCol} = {p}");
                parameters.Add(new MySqlParameter(p, cond.NumericValue.Value));
            }
        }

        private static void ApplyDateCondition(string sqlCol, DocumentFilterCondition cond,
            List<string> conditions, List<MySqlParameter> parameters, ref int seq)
        {
            string op = cond.Operator ?? string.Empty;

            if (string.Equals(op, "To", StringComparison.OrdinalIgnoreCase) && cond.DateFrom.HasValue)
            {
                string p = "@p" + seq++;
                conditions.Add($"{sqlCol} < {p}");
                parameters.Add(new MySqlParameter(p, cond.DateFrom.Value.Date.AddDays(1)));
                return;
            }

            if (string.Equals(op, "From", StringComparison.OrdinalIgnoreCase) && cond.DateFrom.HasValue)
            {
                string p = "@p" + seq++;
                conditions.Add($"{sqlCol} >= {p}");
                parameters.Add(new MySqlParameter(p, cond.DateFrom.Value.Date));
                return;
            }

            if ((string.Equals(op, "On", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(op, "Between", StringComparison.OrdinalIgnoreCase))
                && cond.DateFrom.HasValue)
            {
                DateTime from = cond.DateFrom.Value.Date;
                DateTime to = cond.DateTo?.Date ?? from;
                if (to < from) (from, to) = (to, from);

                string pFrom = "@p" + seq++;
                string pTo = "@p" + seq++;
                conditions.Add($"{sqlCol} >= {pFrom} AND {sqlCol} < {pTo}");
                parameters.Add(new MySqlParameter(pFrom, from));
                parameters.Add(new MySqlParameter(pTo, to.AddDays(1)));
            }
        }
    }
}
