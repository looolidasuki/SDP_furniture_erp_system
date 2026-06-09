using System;
using System.Data;
using System.Windows.Forms;
using Sales_user.Controllers;

namespace FurnitureERP.Helpers
{
    public static class CustomerComboHelper
    {
        public static DataTable BuildPickerTable(CustomerController customerCtrl)
        {
            var dt = customerCtrl?.GetAllCustomers();
            if (dt == null) return new DataTable();

            if (!dt.Columns.Contains("DisplayText"))
                dt.Columns.Add("DisplayText", typeof(string));

            foreach (DataRow row in dt.Rows)
            {
                string code = dt.Columns.Contains("Customer Code") ? row["Customer Code"]?.ToString() : "";
                string name = row["Customer Name"]?.ToString() ?? "";
                string billing = dt.Columns.Contains("Billing Address") ? row["Billing Address"]?.ToString() : "";
                string refNo = dt.Columns.Contains("Customer Ref Number") ? row["Customer Ref Number"]?.ToString() : "";

                string prefix = "";
                if (!string.IsNullOrWhiteSpace(code)) prefix = code.Trim();
                if (!string.IsNullOrWhiteSpace(refNo))
                    prefix += (prefix.Length > 0 ? " / " : "") + refNo.Trim();

                string display = string.IsNullOrWhiteSpace(prefix) ? name : $"{prefix} — {name}";
                if (!string.IsNullOrWhiteSpace(billing))
                {
                    string shortAddr = billing.Trim();
                    if (shortAddr.Length > 48)
                        shortAddr = shortAddr.Substring(0, 45) + "...";
                    display += $" ({shortAddr})";
                }

                row["DisplayText"] = display;
            }

            return dt;
        }

        public static FilteredComboBinder Attach(ComboBox cmb, CustomerController customerCtrl, long selectedCustomerId = 0)
        {
            if (cmb == null) throw new ArgumentNullException(nameof(cmb));
            if (customerCtrl == null) throw new ArgumentNullException(nameof(customerCtrl));
            if (cmb.Width <= 0) cmb.Width = 340;

            var binder = new FilteredComboBinder(cmb, "Customer ID", "DisplayText");
            binder.SetSource(BuildPickerTable(customerCtrl), selectedCustomerId);
            cmb.Tag = binder;
            return binder;
        }

        public static FilteredComboBinder GetBinder(ComboBox cmb) => cmb?.Tag as FilteredComboBinder;

        public static void SelectCustomer(ComboBox cmb, long customerId)
        {
            if (cmb == null || customerId <= 0) return;
            var binder = GetBinder(cmb);
            if (binder != null)
                binder.SelectById(customerId);
            else
            {
                try { cmb.SelectedValue = customerId; }
                catch { }
            }
        }

        public static long ResolveCustomerId(ComboBox cmb, CustomerController customerCtrl)
        {
            if (cmb == null || customerCtrl == null) return 0;

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
                && rowView.Row.Table.Columns.Contains("Customer ID")
                && rowView["Customer ID"] != DBNull.Value
                && long.TryParse(rowView["Customer ID"].ToString(), out long rowId) && rowId > 0)
                return rowId;

            return customerCtrl.FindCustomerIdByText(cmb.Text);
        }

        public static void WireCustomerChanged(ComboBox cmb, CustomerController customerCtrl, Action<long> onCustomerResolved)
        {
            if (cmb == null || onCustomerResolved == null) return;

            Action tryResolve = () =>
            {
                long id = ResolveCustomerId(cmb, customerCtrl);
                if (id > 0) onCustomerResolved(id);
            };

            var binder = GetBinder(cmb);
            if (binder != null)
                binder.SelectionCommitted += (s, e) => tryResolve();
            cmb.Leave += (s, e) => tryResolve();
        }
    }
}
