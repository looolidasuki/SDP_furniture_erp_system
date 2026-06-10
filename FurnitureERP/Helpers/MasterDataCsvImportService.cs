using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using Sales_user.Controllers;
using Sales_user.Models;

namespace FurnitureERP.Helpers
{
    public enum MasterDataImportKind
    {
        Customer,
        Supplier,
        Product,
        RawMaterial
    }

    public sealed class MasterDataImportPreviewRow
    {
        public int RowNumber { get; set; }
        public string Validation { get; set; }
        public bool IsValid => string.IsNullOrEmpty(Validation);
        public DataRow Source { get; set; }
    }

    public sealed class MasterDataImportResult
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public List<string> Messages { get; } = new List<string>();
    }

    public static class MasterDataCsvImportService
    {
        private static readonly CustomerController CustomerCtrl = new CustomerController();
        private static readonly SupplierController SupplierCtrl = new SupplierController();
        private static readonly ProductController ProductCtrl = new ProductController();
        private static readonly RawMaterialController RawMaterialCtrl = new RawMaterialController();
        private static readonly CurrencyController CurrencyCtrl = new CurrencyController();

        public static string GetSampleFileName(MasterDataImportKind kind)
        {
            switch (kind)
            {
                case MasterDataImportKind.Customer: return "customers_sample.csv";
                case MasterDataImportKind.Supplier: return "suppliers_sample.csv";
                case MasterDataImportKind.Product: return "products_sample.csv";
                case MasterDataImportKind.RawMaterial: return "raw_materials_sample.csv";
                default: return "import_sample.csv";
            }
        }

        public static string GetSampleFilePath(MasterDataImportKind kind) =>
            System.IO.Path.Combine(CsvImportHelper.SampleImportFolder, GetSampleFileName(kind));

        public static string GetKindDisplayName(MasterDataImportKind kind)
        {
            switch (kind)
            {
                case MasterDataImportKind.Customer: return "Customers";
                case MasterDataImportKind.Supplier: return "Suppliers";
                case MasterDataImportKind.Product: return "Products";
                case MasterDataImportKind.RawMaterial: return "Raw Materials";
                default: return kind.ToString();
            }
        }

        public static string GetRequiredColumnsHint(MasterDataImportKind kind)
        {
            switch (kind)
            {
                case MasterDataImportKind.Customer:
                    return "Required: customerName. Optional: billingAddress, paymentTerm";
                case MasterDataImportKind.Supplier:
                    return "Required: supplierName. Optional: billingAddress, contactPerson, phone, email, paymentTerm, bankAccount, status";
                case MasterDataImportKind.Product:
                    return "Required: productCode. Optional: category, styleNumber, size, color, basePrice, unit, status, currencyCode, remark";
                case MasterDataImportKind.RawMaterial:
                    return "Required: rawMaterialCode. Optional: category, size, color, minimumStockLevel, status";
                default: return "";
            }
        }

        public static List<MasterDataImportPreviewRow> BuildPreview(MasterDataImportKind kind, DataTable table)
        {
            var list = new List<MasterDataImportPreviewRow>();
            if (table == null) return list;

            int rowNum = 1;
            foreach (DataRow row in table.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;
                list.Add(new MasterDataImportPreviewRow
                {
                    RowNumber = rowNum++,
                    Validation = ValidateRow(kind, row),
                    Source = row
                });
            }
            return list;
        }

        public static DataTable BuildPreviewGrid(List<MasterDataImportPreviewRow> preview)
        {
            var grid = new DataTable();
            grid.Columns.Add("Row", typeof(int));
            grid.Columns.Add("Status", typeof(string));
            if (preview == null || preview.Count == 0)
                return grid;

            foreach (DataColumn col in preview[0].Source.Table.Columns)
                grid.Columns.Add(col.ColumnName, typeof(string));

            foreach (var item in preview)
            {
                var row = grid.NewRow();
                row["Row"] = item.RowNumber;
                row["Status"] = item.IsValid ? "OK" : item.Validation;
                foreach (DataColumn col in item.Source.Table.Columns)
                    row[col.ColumnName] = item.Source[col]?.ToString() ?? "";
                grid.Rows.Add(row);
            }
            return grid;
        }

        public static MasterDataImportResult Import(MasterDataImportKind kind, List<MasterDataImportPreviewRow> preview, bool upsert = true)
        {
            var result = new MasterDataImportResult();
            if (preview == null) return result;

            foreach (var item in preview)
            {
                if (!item.IsValid)
                {
                    result.Skipped++;
                    continue;
                }

                try
                {
                    switch (kind)
                    {
                        case MasterDataImportKind.Customer:
                            ImportCustomer(item, upsert, result);
                            break;
                        case MasterDataImportKind.Supplier:
                            ImportSupplier(item, upsert, result);
                            break;
                        case MasterDataImportKind.Product:
                            ImportProduct(item, upsert, result);
                            break;
                        case MasterDataImportKind.RawMaterial:
                            ImportRawMaterial(item, upsert, result);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Messages.Add("Row " + item.RowNumber + ": " + ex.Message);
                }
            }

            return result;
        }

        private static string ValidateRow(MasterDataImportKind kind, DataRow row)
        {
            switch (kind)
            {
                case MasterDataImportKind.Customer:
                    if (string.IsNullOrWhiteSpace(CsvImportHelper.GetCell(row, "customerName", "CustomerName")))
                        return "customerName is required";
                    return null;
                case MasterDataImportKind.Supplier:
                    if (string.IsNullOrWhiteSpace(CsvImportHelper.GetCell(row, "supplierName", "SupplierName")))
                        return "supplierName is required";
                    return null;
                case MasterDataImportKind.Product:
                    if (string.IsNullOrWhiteSpace(CsvImportHelper.GetCell(row, "productCode", "ProductCode")))
                        return "productCode is required";
                    return null;
                case MasterDataImportKind.RawMaterial:
                    if (string.IsNullOrWhiteSpace(CsvImportHelper.GetCell(row, "rawMaterialCode", "RawMaterialCode")))
                        return "rawMaterialCode is required";
                    return null;
                default:
                    return "Unknown import type";
            }
        }

        private static void ImportCustomer(MasterDataImportPreviewRow item, bool upsert, MasterDataImportResult result)
        {
            var row = item.Source;
            string name = CsvImportHelper.GetCell(row, "customerName", "CustomerName");
            var customer = new Customer
            {
                CustomerName = name,
                BillingAddress = NullIfEmpty(CsvImportHelper.GetCell(row, "billingAddress", "BillingAddress")),
                PaymentTerm = NullIfEmpty(CsvImportHelper.GetCell(row, "paymentTerm", "PaymentTerm"))
            };

            long existingId = CustomerCtrl.FindCustomerIdByText(name);
            if (existingId > 0 && upsert)
            {
                customer.CustomerID = existingId;
                var existing = CustomerCtrl.GetById(existingId);
                customer.CustomerCode = existing?.CustomerCode;
                if (CustomerCtrl.Update(customer))
                {
                    result.Updated++;
                    DocumentAuditService.LogAction(DocumentAuditService.Types.Customer, existingId,
                        customer.CustomerCode, DocumentAuditService.Actions.Import, "Updated from CSV import");
                }
                else
                {
                    result.Failed++;
                    result.Messages.Add("Row " + item.RowNumber + ": update failed");
                }
                return;
            }

            if (existingId > 0)
            {
                result.Skipped++;
                result.Messages.Add("Row " + item.RowNumber + ": customer already exists — " + name);
                return;
            }

            long id = CustomerCtrl.Insert(customer);
            if (id > 0)
            {
                result.Created++;
                DocumentAuditService.LogAction(DocumentAuditService.Types.Customer, id,
                    customer.CustomerCode, DocumentAuditService.Actions.Import, "Imported from CSV");
            }
            else
            {
                result.Failed++;
                result.Messages.Add("Row " + item.RowNumber + ": insert failed");
            }
        }

        private static void ImportSupplier(MasterDataImportPreviewRow item, bool upsert, MasterDataImportResult result)
        {
            var row = item.Source;
            string name = CsvImportHelper.GetCell(row, "supplierName", "SupplierName");
            var supplier = new Supplier
            {
                SupplierName = name,
                BillingAddress = NullIfEmpty(CsvImportHelper.GetCell(row, "billingAddress", "BillingAddress")),
                ContactPerson = NullIfEmpty(CsvImportHelper.GetCell(row, "contactPerson", "ContactPerson")),
                Phone = NullIfEmpty(CsvImportHelper.GetCell(row, "phone", "Phone")),
                Email = NullIfEmpty(CsvImportHelper.GetCell(row, "email", "Email")),
                PaymentTerm = NullIfEmpty(CsvImportHelper.GetCell(row, "paymentTerm", "PaymentTerm")),
                BankAccount = NullIfEmpty(CsvImportHelper.GetCell(row, "bankAccount", "BankAccount")),
                Status = ParseStatus(CsvImportHelper.GetCell(row, "status", "Status"), 1)
            };

            long existingId = SupplierCtrl.FindSupplierIdByName(name);
            if (existingId > 0 && upsert)
            {
                supplier.SupplierID = existingId;
                SupplierCtrl.Update(supplier);
                result.Updated++;
                DocumentAuditService.LogAction(DocumentAuditService.Types.Supplier, existingId,
                    name, DocumentAuditService.Actions.Import, "Updated from CSV import");
                return;
            }

            if (existingId > 0)
            {
                result.Skipped++;
                result.Messages.Add("Row " + item.RowNumber + ": supplier already exists — " + name);
                return;
            }

            long id = SupplierCtrl.Insert(supplier);
            if (id > 0)
            {
                result.Created++;
                DocumentAuditService.LogAction(DocumentAuditService.Types.Supplier, id,
                    name, DocumentAuditService.Actions.Import, "Imported from CSV");
            }
            else
            {
                result.Failed++;
                result.Messages.Add("Row " + item.RowNumber + ": insert failed");
            }
        }

        private static void ImportProduct(MasterDataImportPreviewRow item, bool upsert, MasterDataImportResult result)
        {
            var row = item.Source;
            string code = CsvImportHelper.GetCell(row, "productCode", "ProductCode");
            var product = new Product
            {
                ProductCode = code,
                Category = NullIfEmpty(CsvImportHelper.GetCell(row, "category", "Category")),
                StyleNumber = NullIfEmpty(CsvImportHelper.GetCell(row, "styleNumber", "StyleNumber")),
                Size = NullIfEmpty(CsvImportHelper.GetCell(row, "size", "Size")),
                Color = NullIfEmpty(CsvImportHelper.GetCell(row, "color", "Color")),
                BasePriceByCurrency = ParseDecimal(CsvImportHelper.GetCell(row, "basePrice", "BasePrice"), 0),
                Unit = NullIfEmpty(CsvImportHelper.GetCell(row, "unit", "Unit")) ?? "PCS",
                Status = ParseStatus(CsvImportHelper.GetCell(row, "status", "Status"), 1),
                Remark = NullIfEmpty(CsvImportHelper.GetCell(row, "remark", "Remark")),
                CurrencyID = ResolveCurrencyId(CsvImportHelper.GetCell(row, "currencyCode", "CurrencyCode", "Currency")),
                StaffID = AppSession.IsLoggedIn && AppSession.CurrentUser != null
                    ? AppSession.CurrentUser.StaffID : 1
            };

            var existing = ProductCtrl.GetByCode(code);
            if (existing != null && upsert)
            {
                product.ProductID = existing.ProductID;
                if (product.CurrencyID <= 0) product.CurrencyID = existing.CurrencyID > 0 ? existing.CurrencyID : 1;
                if (ProductCtrl.Update(product))
                {
                    result.Updated++;
                    DocumentAuditService.LogAction(DocumentAuditService.Types.Product, existing.ProductID,
                        code, DocumentAuditService.Actions.Import, "Updated from CSV import");
                }
                else
                {
                    result.Failed++;
                    result.Messages.Add("Row " + item.RowNumber + ": update failed");
                }
                return;
            }

            if (existing != null)
            {
                result.Skipped++;
                result.Messages.Add("Row " + item.RowNumber + ": product code already exists — " + code);
                return;
            }

            if (product.CurrencyID <= 0) product.CurrencyID = 1;
            long id = ProductCtrl.Insert(product);
            if (id > 0)
            {
                result.Created++;
                DocumentAuditService.LogAction(DocumentAuditService.Types.Product, id,
                    code, DocumentAuditService.Actions.Import, "Imported from CSV");
            }
            else
            {
                result.Failed++;
                result.Messages.Add("Row " + item.RowNumber + ": insert failed");
            }
        }

        private static void ImportRawMaterial(MasterDataImportPreviewRow item, bool upsert, MasterDataImportResult result)
        {
            var row = item.Source;
            string code = CsvImportHelper.GetCell(row, "rawMaterialCode", "RawMaterialCode");
            var material = new RawMaterial
            {
                RawMaterialCode = code,
                Category = NullIfEmpty(CsvImportHelper.GetCell(row, "category", "Category")),
                Size = NullIfEmpty(CsvImportHelper.GetCell(row, "size", "Size")),
                Color = NullIfEmpty(CsvImportHelper.GetCell(row, "color", "Color")),
                MinimumStockLevel = ParseDecimal(CsvImportHelper.GetCell(row, "minimumStockLevel", "MinimumStockLevel", "MinStock"), 0),
                Status = ParseStatus(CsvImportHelper.GetCell(row, "status", "Status"), 1)
            };

            var existing = GetRawMaterialByCode(code);
            if (existing != null && upsert)
            {
                material.RawMaterialID = existing.RawMaterialID;
                RawMaterialCtrl.Update(material);
                result.Updated++;
                return;
            }

            if (existing != null)
            {
                result.Skipped++;
                result.Messages.Add("Row " + item.RowNumber + ": raw material code already exists — " + code);
                return;
            }

            long id = RawMaterialCtrl.Insert(material);
            if (id > 0)
                result.Created++;
            else
            {
                result.Failed++;
                result.Messages.Add("Row " + item.RowNumber + ": insert failed");
            }
        }

        private static RawMaterial GetRawMaterialByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            object id = DatabaseConnect.ExecuteScalar(
                "SELECT rawMaterialID FROM RawMaterial WHERE rawMaterialCode = @code LIMIT 1",
                new[] { new MySqlParameter("@code", code.Trim()) });
            if (id == null || id == DBNull.Value) return null;
            return RawMaterialCtrl.GetById(Convert.ToInt64(id));
        }

        private static long ResolveCurrencyId(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode)) return 1;
            object value = DatabaseConnect.ExecuteScalar(
                "SELECT currencyID FROM Currency WHERE currencyCode = @code LIMIT 1",
                new[] { new MySqlParameter("@code", currencyCode.Trim().ToUpperInvariant()) });
            if (value == null || value == DBNull.Value) return 1;
            return Convert.ToInt64(value);
        }

        private static int ParseStatus(string text, int defaultValue)
        {
            if (string.IsNullOrWhiteSpace(text)) return defaultValue;
            if (int.TryParse(text.Trim(), out int code)) return code;
            string lower = text.Trim().ToLowerInvariant();
            if (lower == "active" || lower == "enabled" || lower == "yes") return 1;
            if (lower == "inactive" || lower == "disabled" || lower == "no") return 0;
            return defaultValue;
        }

        private static decimal ParseDecimal(string text, decimal defaultValue)
        {
            if (string.IsNullOrWhiteSpace(text)) return defaultValue;
            return decimal.TryParse(text.Trim(), out decimal value) ? value : defaultValue;
        }

        private static string NullIfEmpty(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
