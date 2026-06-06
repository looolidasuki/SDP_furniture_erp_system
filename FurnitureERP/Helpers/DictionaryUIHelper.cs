using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class DictionaryUIHelper
    {
        public static void BindStatusCombo(ComboBox combo, string category, int selectedCode)
        {
            combo.Items.Clear();
            combo.DisplayMember = "Value";
            combo.ValueMember = "Key";
            var items = DictionaryService.GetItems(category)
                .Select(x => new ComboBoxItem(x.Key, x.Value))
                .ToList();
            foreach (var item in items)
                combo.Items.Add(item);

            SelectStatusCombo(combo, selectedCode);
        }

        public static void SelectStatusCombo(ComboBox combo, int selectedCode)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is ComboBoxItem item && item.Code == selectedCode)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        public static int GetSelectedStatusCode(ComboBox combo)
        {
            if (combo.SelectedItem is ComboBoxItem item)
                return item.Code;
            return combo.SelectedIndex >= 0 ? combo.SelectedIndex : 0;
        }

        public static void BindPaymentTermCombo(ComboBox combo, string selectedLabel)
        {
            combo.Items.Clear();
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var item in DictionaryService.GetItems(DictionaryService.Categories.PaymentTerm))
                combo.Items.Add(item.Value);

            selectedLabel = selectedLabel?.Trim();
            if (!string.IsNullOrWhiteSpace(selectedLabel) && combo.Items.IndexOf(selectedLabel) < 0)
                combo.Items.Insert(0, selectedLabel);

            if (!string.IsNullOrWhiteSpace(selectedLabel))
            {
                int idx = combo.Items.IndexOf(selectedLabel);
                if (idx >= 0)
                {
                    combo.SelectedIndex = idx;
                    return;
                }
            }

            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        public static string GetSelectedPaymentTerm(ComboBox combo)
        {
            return combo.SelectedItem?.ToString()?.Trim() ?? "";
        }

        public static void BindStatusFilter(ComboBox combo, string category)
        {
            combo.Items.Clear();
            combo.Items.Add("All Status");
            foreach (var item in DictionaryService.GetItems(category))
                combo.Items.Add(new ComboBoxItem(item.Key, item.Value));
            combo.DisplayMember = "Value";
            combo.SelectedIndex = 0;
        }

        public static int? GetFilterStatusCode(ComboBox combo)
        {
            if (combo.SelectedIndex <= 0)
                return null;
            if (combo.SelectedItem is ComboBoxItem item)
                return item.Code;
            return combo.SelectedIndex - 1;
        }

        public static DataTable LoadWithStatusLabels(Func<DataTable> loader, string statusColumn, string category)
        {
            var dt = loader();
            return DictionaryService.DecorateStatusColumn(dt, statusColumn, category);
        }

        public sealed class ComboBoxItem
        {
            public ComboBoxItem(int code, string label)
            {
                Code = code;
                Value = label;
            }

            public int Code { get; }
            public string Value { get; }
            public override string ToString() => Value;
        }
    }
}
