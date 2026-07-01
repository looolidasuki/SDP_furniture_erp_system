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
            return supplierCtrl?.SearchForPicker("", 200) ?? SupplierController.BuildPickerTableSchema();
        }

        public static FilteredComboBinder Attach(ComboBox cmb, SupplierController supplierCtrl, long selectedSupplierId = 0)
        {
            if (cmb == null) throw new ArgumentNullException(nameof(cmb));
            if (supplierCtrl == null) throw new ArgumentNullException(nameof(supplierCtrl));
            if (cmb.Width <= 0) cmb.Width = 360;

            var binder = new FilteredComboBinder(cmb, "Supplier ID", "DisplayText");
            binder.MinTypeAheadChars = 2;
            binder.SetServerSearch(term => supplierCtrl.SearchForPicker(term, 25));

            DataTable initial = selectedSupplierId > 0
                ? supplierCtrl.GetSupplierPickerById(selectedSupplierId)
                : supplierCtrl.SearchForPicker("", 50);
            binder.SetSource(initial, selectedSupplierId);

            cmb.Tag = new SupplierComboContext { Binder = binder, Controller = supplierCtrl };
            return binder;
        }

        private sealed class SupplierComboContext
        {
            public FilteredComboBinder Binder { get; set; }
            public SupplierController Controller { get; set; }
        }

        public static FilteredComboBinder GetBinder(ComboBox cmb)
        {
            if (cmb?.Tag is SupplierComboContext ctx)
                return ctx.Binder;
            return cmb?.Tag as FilteredComboBinder;
        }

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

            object selected = null;
            try { selected = cmb.SelectedValue; }
            catch { }

            if (selected != null && selected != DBNull.Value && long.TryParse(selected.ToString(), out long parsed) && parsed > 0)
                return parsed;

            try
            {
                if (cmb.SelectedItem is DataRowView rowView
                    && rowView.Row.Table.Columns.Contains("Supplier ID")
                    && rowView["Supplier ID"] != DBNull.Value
                    && long.TryParse(rowView["Supplier ID"].ToString(), out long rowId) && rowId > 0)
                    return rowId;
            }
            catch { }

            try
            {
                return supplierCtrl.FindSupplierIdByName(cmb.Text);
            }
            catch
            {
                return 0;
            }
        }

        public static void WireGridSupplierComboColumn(DataGridView grid, string columnName, SupplierController supplierCtrl)
        {
            if (grid == null || supplierCtrl == null || string.IsNullOrWhiteSpace(columnName)) return;

            ComboBox activeCombo = null;
            EventHandler textUpdateHandler = null;

            grid.EditingControlShowing += (s, e) =>
            {
                if (activeCombo != null && textUpdateHandler != null)
                {
                    activeCombo.TextUpdate -= textUpdateHandler;
                    activeCombo = null;
                    textUpdateHandler = null;
                }

                if (grid.CurrentCell?.OwningColumn?.Name != columnName) return;
                if (!(e.Control is ComboBox cmb)) return;

                activeCombo = cmb;
                cmb.DropDownStyle = ComboBoxStyle.DropDown;
                cmb.AutoCompleteMode = AutoCompleteMode.None;
                RefreshGridSupplierComboDataSource(cmb, supplierCtrl, cmb.Text);

                textUpdateHandler = (sender, args) =>
                {
                    string snapshot = "";
                    try { snapshot = cmb.Text ?? ""; } catch { }
                    string captured = snapshot;
                    cmb.BeginInvoke(new Action(() =>
                        RefreshGridSupplierComboDataSource(cmb, supplierCtrl, captured)));
                };
                cmb.TextUpdate += textUpdateHandler;
            };
        }

        private static void RefreshGridSupplierComboDataSource(ComboBox cmb, SupplierController supplierCtrl, string text)
        {
            if (cmb == null || supplierCtrl == null || cmb.IsDisposed) return;
            if (cmb.DroppedDown) return;

            string workingText = text ?? "";
            string term = workingText.Trim();
            DataTable source = term.Length < FilteredComboBinder.DefaultMinTypeAheadChars
                ? supplierCtrl.SearchForPicker("", 50)
                : supplierCtrl.SearchForPicker(term, 25);

            ComboSelectionGuard.BeforeRebind(cmb);
            cmb.DataSource = source;
            cmb.DisplayMember = "DisplayText";
            cmb.ValueMember = "Supplier ID";
            ComboSelectionGuard.Clamp(cmb);

            try
            {
                if (!string.Equals(cmb.Text, workingText, StringComparison.Ordinal))
                    cmb.Text = workingText;
            }
            catch { }

            try
            {
                int count = cmb.Items.Count;
                if (cmb.SelectedIndex >= count)
                    ComboSelectionGuard.Clamp(cmb);
                cmb.SelectionStart = workingText.Length;
                cmb.SelectionLength = 0;
            }
            catch { }
        }
    }
}
