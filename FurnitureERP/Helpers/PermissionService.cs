using System;
using System.Collections.Generic;
using Sales_user.Models;

namespace FurnitureERP.Helpers
{
    public static class PermissionService
    {
        private static readonly Dictionary<string, Dictionary<string, PermissionFlags>> Matrix =
            BuildMatrix();

        public static bool IsSuperUser(Staff user)
        {
            if (user == null) return false;
            if (string.Equals(user.Username, "root", StringComparison.OrdinalIgnoreCase)) return true;
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

        private static Dictionary<string, Dictionary<string, PermissionFlags>> BuildMatrix()
        {
            var matrix = new Dictionary<string, Dictionary<string, PermissionFlags>>(StringComparer.OrdinalIgnoreCase);

            matrix["sales"] = Dict(
                P(PermissionModule.SalesOrder, PermissionFlags.All),
                P(PermissionModule.Quotation, PermissionFlags.All),
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
