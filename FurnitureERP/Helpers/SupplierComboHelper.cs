using System;
using System.Data;
using System.Windows.Forms;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class SupplierComboHelper
    {
        public static DataTable BuildPickerTable(SupplierController supplierCtrl)
        {
            var dt = supplierCtrl?.GetAllSuppliers();
            if (dt == null) return new DataTable();

            if (!dt.Columns.Contains("DisplayText"))
                dt.Columns.Add("DisplayText", typeof(string));

            foreach (DataRow row in dt.Rows)
                row["DisplayText"] = row["Supplier Name"]?.ToString() ?? "";

            return dt;
        }

        public static FilteredComboBinder Attach(ComboBox cmb, SupplierController supplierCtrl, long selectedSupplierId = 0)
        {
            if (cmb == null) throw new ArgumentNullException(nameof(cmb));
            if (supplierCtrl == null) throw new ArgumentNullException(nameof(supplierCtrl));
            if (cmb.Width <= 0) cmb.Width = 360;

            var binder = new FilteredComboBinder(cmb, "Supplier ID", "DisplayText");
            binder.SetSource(BuildPickerTable(supplierCtrl), selectedSupplierId);
            cmb.Tag = binder;
            return binder;
        }

        public static FilteredComboBinder GetBinder(ComboBox cmb) => cmb?.Tag as FilteredComboBinder;

        public static void SelectSupplier(ComboBox cmb, long supplierId)
        {
            if (cmb == null || supplierId <= 0) return;
            var binder = GetBinder(cmb);
            if (binder != null)
                binder.SelectById(supplierId);
            else
            {
                try { cmb.SelectedValue = supplierId; }
                catch { }
            }
        }

        public static long ResolveSupplierId(ComboBox cmb, SupplierController supplierCtrl)
        {
            if (cmb == null || supplierCtrl == null) return 0;

            var binder = GetBinder(cmb);
            if (binder != null)
            {
                long id = binder.GetSelectedId();
                if (id > 0) return id;
            }

            object selected = cmb.SelectedValue;
            if (selected != null && selected != DBNull.Value && long.TryParse(selected.ToString(), out long parsed) && parsed > 0)
                return parsed;

            if (cmb.SelectedItem is DataRowView rowView
                && rowView.Row.Table.Columns.Contains("Supplier ID")
                && rowView["Supplier ID"] != DBNull.Value
                && long.TryParse(rowView["Supplier ID"].ToString(), out long rowId) && rowId > 0)
                return rowId;

            try
            {
                return supplierCtrl.FindSupplierIdByName(cmb.Text);
            }
            catch
            {
                return 0;
            }
        }
    }
}
