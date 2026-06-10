using System;
using System.Data;
using System.Windows.Forms;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class SalesOrderComboHelper
    {
        public static DataTable BuildPickerTable(SalesOrderController salesOrderCtrl, long customerId)
        {
            if (salesOrderCtrl == null || customerId <= 0)
            {
                var empty = new DataTable();
                empty.Columns.Add("Order ID", typeof(long));
                empty.Columns.Add("DisplayText", typeof(string));
                return empty;
            }

            var dt = salesOrderCtrl.GetSalesOrdersPickerByCustomer(customerId);
            if (dt == null)
            {
                dt = new DataTable();
                dt.Columns.Add("Order ID", typeof(long));
            }

            if (!dt.Columns.Contains("DisplayText"))
                dt.Columns.Add("DisplayText", typeof(string));

            foreach (DataRow row in dt.Rows)
            {
                string code = dt.Columns.Contains("Order Code") ? row["Order Code"]?.ToString() : "";
                string cref = dt.Columns.Contains("Customer Ref") ? row["Customer Ref"]?.ToString() : "";
                row["DisplayText"] = string.IsNullOrWhiteSpace(cref) ? code : $"{code} ({cref})";
            }

            return dt;
        }

        public static FilteredComboBinder Attach(
            ComboBox cmb,
            SalesOrderController salesOrderCtrl,
            long customerId,
            long selectedSalesOrderId = 0)
        {
            if (cmb == null) throw new ArgumentNullException(nameof(cmb));
            if (salesOrderCtrl == null) throw new ArgumentNullException(nameof(salesOrderCtrl));
            if (cmb.Width <= 0) cmb.Width = 340;

            var binder = new FilteredComboBinder(cmb, "Order ID", "DisplayText");
            binder.SetSource(BuildPickerTable(salesOrderCtrl, customerId), selectedSalesOrderId);
            cmb.Tag = binder;
            return binder;
        }

        public static FilteredComboBinder GetBinder(ComboBox cmb) => cmb?.Tag as FilteredComboBinder;

        public static void Rebind(ComboBox cmb, SalesOrderController salesOrderCtrl, long customerId, long selectedSalesOrderId = 0)
        {
            var binder = GetBinder(cmb);
            if (binder == null)
            {
                Attach(cmb, salesOrderCtrl, customerId, selectedSalesOrderId);
                return;
            }

            binder.SetSource(BuildPickerTable(salesOrderCtrl, customerId), selectedSalesOrderId);
        }

        public static long ResolveSalesOrderId(ComboBox cmb, SalesOrderController salesOrderCtrl)
        {
            var binder = GetBinder(cmb);
            if (binder != null)
            {
                long id = binder.GetSelectedId();
                if (id > 0) return id;
            }

            if (cmb?.SelectedValue != null && cmb.SelectedValue != DBNull.Value
                && long.TryParse(cmb.SelectedValue.ToString(), out long selected) && selected > 0)
                return selected;

            return 0;
        }
    }
}
