using System;
using System.Collections.Generic;
using System.Data;
using Sales_user.Models;

namespace FurnitureERP.Helpers
{
    public static class PermissionService
    {
        private static readonly Dictionary<string, Dictionary<string, PermissionFlags>> Matrix =
            BuildMatrix();

        private static readonly (string Key, string DisplayName)[] OverviewDepartments =
        {
            ("sales", "Sales"),
            ("production", "Production"),
            ("warehouse", "Warehouse & Inventory"),
            ("logistic", "Logistic"),
            ("account", "Account / Finance"),
            ("superuser", "Super User — Full Access")
        };

        public static IReadOnlyList<(string Key, string DisplayName)> GetOverviewDepartments() => OverviewDepartments;

        public static bool IsSuperUser(Staff user)
        {
            if (user == null) return false;
            if (string.Equals(user.Username, "root", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(user.Username, "admin", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(user.Username, "superuser", StringComparison.OrdinalIgnoreCase)) return true;
            var dept = NormalizeDepartment(user.Department);
            return dept == "superuser";
        }

        public static PermissionFlags GetPermissions(Staff user, string module)
        {
            if (user == null || string.IsNullOrWhiteSpace(module)) return PermissionFlags.None;
            if (IsSuperUser(user)) return PermissionFlags.All;

            var dept = NormalizeDepartment(user.Department);
            if (!Matrix.TryGetValue(dept, out var modules)) return PermissionFlags.None;
            return modules.TryGetValue(module, out var flags) ? flags : PermissionFlags.None;
        }

        public static bool Has(Staff user, string module, PermissionAction action)
        {
            var flags = GetPermissions(user, module);
            switch (action)
            {
                case PermissionAction.View: return flags.HasFlag(PermissionFlags.View);
                case PermissionAction.Create: return flags.HasFlag(PermissionFlags.Create);
                case PermissionAction.Edit: return flags.HasFlag(PermissionFlags.Edit);
                default: return false;
            }
        }

        public static string NormalizeDepartment(string department)
        {
            if (string.IsNullOrWhiteSpace(department)) return string.Empty;
            var value = department.Trim().ToLowerInvariant();
            if (value == "sales") return "sales";
            if (value == "production") return "production";
            if (value == "warehouse & inventory" || value == "warehouse and inventory" || value == "warehouse" || value == "inventory")
                return "warehouse";
            if (value == "logistic" || value == "logistics") return "logistic";
            if (value == "account" || value == "accounting" || value == "accounts" || value == "finance")
                return "account";
            if (value == "super user" || value == "superuser") return "superuser";
            return value;
        }

        public static string GetDepartmentDisplayName(string departmentKey)
        {
            if (string.IsNullOrWhiteSpace(departmentKey)) return "Unknown";
            foreach (var item in OverviewDepartments)
            {
                if (string.Equals(item.Key, departmentKey, StringComparison.OrdinalIgnoreCase))
                    return item.DisplayName;
            }
            return char.ToUpper(departmentKey[0]) + departmentKey.Substring(1);
        }

        public static string GetModuleDisplayName(string module)
        {
            if (string.IsNullOrWhiteSpace(module)) return module;
            switch (module)
            {
                case PermissionModule.SalesOrder: return "Sales Order";
                case PermissionModule.Quotation: return "Quotation";
                case PermissionModule.Customer: return "Customer";
                case PermissionModule.DeliveryNote: return "Delivery Note";
                case PermissionModule.Invoice: return "Invoice";
                case PermissionModule.ReplySlip: return "Reply Slip";
                case PermissionModule.Warehouse: return "Warehouse";
                case PermissionModule.Refund: return "Refund";
                case PermissionModule.ProductionOrder: return "Production Order";
                case PermissionModule.RawMaterialRequestNote: return "Raw Material Request";
                case PermissionModule.Product: return "Product";
                case PermissionModule.InternalTransferForm: return "Internal Transfer";
                case PermissionModule.RawMaterial: return "Raw Material";
                case PermissionModule.Supplier: return "Supplier";
                case PermissionModule.PurchaseOrder: return "Purchase Order";
                case PermissionModule.GoodsReceivedNote: return "Goods Received Note";
                case PermissionModule.PaymentVoucher: return "Payment Voucher";
                case PermissionModule.ReceiptVoucher: return "Receipt Voucher";
                default: return module;
            }
        }

        public static PermissionFlags GetDepartmentModulePermissions(string departmentKey, string module)
        {
            if (string.IsNullOrWhiteSpace(departmentKey) || string.IsNullOrWhiteSpace(module))
                return PermissionFlags.None;
            if (string.Equals(departmentKey, "superuser", StringComparison.OrdinalIgnoreCase))
                return PermissionFlags.All;

            string normalized = NormalizeDepartment(departmentKey);
            if (!Matrix.TryGetValue(normalized, out var modules))
                return PermissionFlags.None;
            return modules.TryGetValue(module, out var flags) ? flags : PermissionFlags.None;
        }

        public static IReadOnlyList<(string Key, string DisplayName)> GetMatrixDepartments()
        {
            var list = new List<(string Key, string DisplayName)>();
            foreach (var dept in OverviewDepartments)
            {
                if (string.Equals(dept.Key, "superuser", StringComparison.OrdinalIgnoreCase))
                    continue;
                list.Add(dept);
            }
            return list;
        }

        public static string FormatPermissionSummary(PermissionFlags flags)
        {
            if (flags == PermissionFlags.None)
                return "No access";

            var parts = new List<string>();
            if (flags.HasFlag(PermissionFlags.View)) parts.Add("View");
            if (flags.HasFlag(PermissionFlags.Create)) parts.Add("Create");
            if (flags.HasFlag(PermissionFlags.Edit)) parts.Add("Edit");
            return parts.Count == 0 ? "No access" : string.Join(", ", parts);
        }

        public static DataTable BuildPermissionMatrixTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("Module", typeof(string));
            foreach (var dept in GetMatrixDepartments())
                dt.Columns.Add(dept.DisplayName, typeof(string));

            var modules = new List<string>(GetAllConfiguredModules());
            modules.Sort((a, b) => string.Compare(GetModuleDisplayName(a), GetModuleDisplayName(b), StringComparison.OrdinalIgnoreCase));

            foreach (string module in modules)
            {
                var row = dt.NewRow();
                row["Module"] = GetModuleDisplayName(module);
                foreach (var dept in GetMatrixDepartments())
                    row[dept.DisplayName] = FormatPermissionSummary(GetDepartmentModulePermissions(dept.Key, module));
                dt.Rows.Add(row);
            }
            return dt;
        }

        public static DataTable BuildPermissionOverviewTable(string departmentKey)
        {
            var dt = new DataTable();
            dt.Columns.Add("Module", typeof(string));
            dt.Columns.Add("Permissions", typeof(string));

            if (string.Equals(departmentKey, "superuser", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string module in GetAllConfiguredModules())
                {
                    dt.Rows.Add(
                        GetModuleDisplayName(module),
                        FormatPermissionSummary(PermissionFlags.All));
                }
                return dt;
            }

            string normalized = NormalizeDepartment(departmentKey);
            if (!Matrix.TryGetValue(normalized, out var modules) || modules.Count == 0)
                return dt;

            var sorted = new List<KeyValuePair<string, PermissionFlags>>(modules);
            sorted.Sort((a, b) => string.Compare(GetModuleDisplayName(a.Key), GetModuleDisplayName(b.Key), StringComparison.OrdinalIgnoreCase));

            foreach (var entry in sorted)
            {
                if (entry.Value == PermissionFlags.None) continue;
                dt.Rows.Add(
                    GetModuleDisplayName(entry.Key),
                    FormatPermissionSummary(entry.Value));
            }
            return dt;
        }

        public static string ResolveOverviewDepartmentKey(Staff user)
        {
            if (user == null) return string.Empty;
            if (IsSuperUser(user)) return "superuser";
            return NormalizeDepartment(user.Department);
        }

        private static IEnumerable<string> GetAllConfiguredModules()
        {
            var modules = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dept in Matrix.Values)
            {
                foreach (string key in dept.Keys)
                    modules.Add(key);
            }
            return modules;
        }

        private static Dictionary<string, Dictionary<string, PermissionFlags>> BuildMatrix()
        {
            var matrix = new Dictionary<string, Dictionary<string, PermissionFlags>>(StringComparer.OrdinalIgnoreCase);

            matrix["sales"] = Dict(
                P(PermissionModule.SalesOrder, PermissionFlags.All),
                P(PermissionModule.Quotation, PermissionFlags.All),
                P(PermissionModule.ReplySlip, PermissionFlags.All),
                P(PermissionModule.Customer, PermissionFlags.All),
                P(PermissionModule.DeliveryNote, PermissionFlags.View),
                P(PermissionModule.Invoice, PermissionFlags.View),
                P(PermissionModule.Warehouse, PermissionFlags.View),
                P(PermissionModule.Refund, PermissionFlags.All),
                P(PermissionModule.ProductionOrder, PermissionFlags.All));

            matrix["production"] = Dict(
                P(PermissionModule.ProductionOrder, PermissionFlags.View | PermissionFlags.Edit),
                P(PermissionModule.RawMaterialRequestNote, PermissionFlags.All),
                P(PermissionModule.Product, PermissionFlags.All),
                P(PermissionModule.InternalTransferForm, PermissionFlags.All),
                P(PermissionModule.RawMaterial, PermissionFlags.All),
                P(PermissionModule.Supplier, PermissionFlags.All));

            matrix["warehouse"] = Dict(
                P(PermissionModule.PurchaseOrder, PermissionFlags.All),
                P(PermissionModule.InternalTransferForm, PermissionFlags.All),
                P(PermissionModule.RawMaterial, PermissionFlags.View),
                P(PermissionModule.Supplier, PermissionFlags.View),
                P(PermissionModule.GoodsReceivedNote, PermissionFlags.All),
                P(PermissionModule.Warehouse, PermissionFlags.View | PermissionFlags.Edit));

            matrix["logistic"] = Dict(
                P(PermissionModule.DeliveryNote, PermissionFlags.All));

            matrix["account"] = Dict(
                P(PermissionModule.Invoice, PermissionFlags.All),
                P(PermissionModule.Refund, PermissionFlags.View | PermissionFlags.Edit),
                P(PermissionModule.PaymentVoucher, PermissionFlags.All),
                P(PermissionModule.ReceiptVoucher, PermissionFlags.All),
                P(PermissionModule.SalesOrder, PermissionFlags.View),
                P(PermissionModule.PurchaseOrder, PermissionFlags.View));

            return matrix;
        }

        private static KeyValuePair<string, PermissionFlags> P(string module, PermissionFlags flags)
        {
            return new KeyValuePair<string, PermissionFlags>(module, flags);
        }

        private static Dictionary<string, PermissionFlags> Dict(params KeyValuePair<string, PermissionFlags>[] items)
        {
            var dict = new Dictionary<string, PermissionFlags>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
                dict[item.Key] = item.Value;
            return dict;
        }
    }
}
