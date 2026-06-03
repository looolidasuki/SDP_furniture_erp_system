using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class FilterBlockHelper
    {
        public static GroupBox CreateFilterBlock(DataGridView grid, string title = "Filters")
        {
            var box = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 200,
                Padding = new Padding(8, 4, 8, 8),
                BackColor = Color.White
            };

            var rowsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };

            var topBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            var btnAdd = UITheme.CreateSecondaryButton("+ Condition");
            btnAdd.Width = 120;
            var btnApply = UITheme.CreatePrimaryButton("Apply");
            btnApply.Width = 80;
            var btnClear = UITheme.CreateSecondaryButton("Clear");
            btnClear.Width = 80;

            topBar.Controls.Add(btnAdd);
            topBar.Controls.Add(btnApply);
            topBar.Controls.Add(btnClear);

            box.Controls.Add(rowsPanel);
            box.Controls.Add(topBar);

            Action addRow = () => rowsPanel.Controls.Add(CreateConditionRow(grid, rowsPanel));
            btnAdd.Click += (s, e) => addRow();
            btnApply.Click += (s, e) => ApplyFilter(grid, rowsPanel);
            btnClear.Click += (s, e) =>
            {
                if (grid.DataSource is DataTable table)
                    table.DefaultView.RowFilter = string.Empty;
                rowsPanel.Controls.Clear();
                addRow();
            };

            addRow();
            return box;
        }

        private static Panel CreateConditionRow(DataGridView grid, FlowLayoutPanel rowsPanel)
        {
            var row = new Panel { Width = 1060, Height = 38, Margin = new Padding(0, 2, 0, 2) };

            var cmbColumn = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260, Left = 0, Top = 6 };
            var cmbOperator = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, Left = 268, Top = 6 };

            var valueHost = new Panel { Width = 420, Height = 28, Left = 406, Top = 6 };
            var txtValue = new TextBox { Width = 410, Left = 0, Top = 1 };
            var numValue = new NumericUpDown { Width = 190, Left = 0, Top = 0, DecimalPlaces = 2, Maximum = 1000000000, Minimum = -1000000000, ThousandsSeparator = true, Visible = false };
            var dtFrom = new DateTimePicker { Width = 190, Left = 0, Top = 0, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Visible = false };
            var dtTo = new DateTimePicker { Width = 190, Left = 200, Top = 0, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false, Visible = false };
            valueHost.Controls.Add(txtValue);
            valueHost.Controls.Add(numValue);
            valueHost.Controls.Add(dtFrom);
            valueHost.Controls.Add(dtTo);

            var btnRemove = UITheme.CreateSecondaryButton("Remove");
            btnRemove.Width = 90;
            btnRemove.Height = 26;
            btnRemove.Left = 836;
            btnRemove.Top = 6;
            btnRemove.Click += (s, e) =>
            {
                rowsPanel.Controls.Remove(row);
                row.Dispose();
            };

            PopulateColumns(cmbColumn, grid);
            cmbColumn.SelectedIndexChanged += (s, e) => ConfigureRowForColumn(grid, cmbColumn, cmbOperator, txtValue, numValue, dtFrom, dtTo);
            cmbOperator.SelectedIndexChanged += (s, e) => ConfigureValueControlsForOperator(grid, cmbColumn, cmbOperator, txtValue, numValue, dtFrom, dtTo);

            if (cmbColumn.Items.Count > 0)
                cmbColumn.SelectedIndex = 0;

            row.Controls.Add(cmbColumn);
            row.Controls.Add(cmbOperator);
            row.Controls.Add(valueHost);
            row.Controls.Add(btnRemove);
            return row;
        }

        private static void PopulateColumns(ComboBox cmbColumn, DataGridView grid)
        {
            cmbColumn.Items.Clear();
            if (!(grid.DataSource is DataTable table)) return;

            var visible = grid.Columns
                .Cast<DataGridViewColumn>()
                .Where(c => c.Visible)
                .Select(c => string.IsNullOrWhiteSpace(c.DataPropertyName) ? c.Name : c.DataPropertyName)
                .Where(name => !string.IsNullOrWhiteSpace(name) && table.Columns.Contains(name))
                .Where(name => !IsIdLikeColumnName(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (visible.Count > 0)
            {
                foreach (var name in visible)
                    cmbColumn.Items.Add(name);
                return;
            }

            foreach (DataColumn col in table.Columns)
            {
                if (IsIdLikeColumnName(col.ColumnName)) continue;
                cmbColumn.Items.Add(col.ColumnName);
            }
        }

        private static void ConfigureRowForColumn(DataGridView grid, ComboBox cmbColumn, ComboBox cmbOperator, TextBox txtValue, NumericUpDown numValue, DateTimePicker dtFrom, DateTimePicker dtTo)
        {
            if (!(grid.DataSource is DataTable table)) return;
            if (cmbColumn.SelectedItem == null) return;
            string colName = cmbColumn.SelectedItem.ToString();
            if (!table.Columns.Contains(colName)) return;

            var type = table.Columns[colName].DataType;
            bool isDate = type == typeof(DateTime) || colName.IndexOf("Date", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isNumber = IsNumericType(type);

            cmbOperator.Items.Clear();
            if (isDate)
                cmbOperator.Items.AddRange(new object[] { "On", "From", "To", "Between" });
            else if (isNumber)
                cmbOperator.Items.AddRange(new object[] { "Equals", ">=", "<=", "Between" });
            else
                cmbOperator.Items.AddRange(new object[] { "Contains", "Equals", "StartsWith" });

            cmbOperator.SelectedIndex = 0;
            ConfigureValueControlsForOperator(grid, cmbColumn, cmbOperator, txtValue, numValue, dtFrom, dtTo);
        }

        private static void ConfigureValueControlsForOperator(DataGridView grid, ComboBox cmbColumn, ComboBox cmbOperator, TextBox txtValue, NumericUpDown numValue, DateTimePicker dtFrom, DateTimePicker dtTo)
        {
            if (!(grid.DataSource is DataTable table)) return;
            if (cmbColumn.SelectedItem == null || cmbOperator.SelectedItem == null) return;
            string colName = cmbColumn.SelectedItem.ToString();
            if (!table.Columns.Contains(colName)) return;

            var type = table.Columns[colName].DataType;
            bool isDate = type == typeof(DateTime) || colName.IndexOf("Date", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isNumber = IsNumericType(type);
            string op = cmbOperator.SelectedItem.ToString();

            txtValue.Visible = !isDate && (!isNumber || op == "Between");
            numValue.Visible = !isDate && isNumber && op != "Between";
            dtFrom.Visible = isDate;
            dtTo.Visible = isDate && op == "Between";

            if (isDate)
            {
                dtFrom.Checked = false;
                dtTo.Checked = false;
            }
        }

        private static void ApplyFilter(DataGridView grid, FlowLayoutPanel rowsPanel)
        {
            if (!(grid.DataSource is DataTable table)) return;
            var clauses = new List<string>();

            foreach (Control ctrl in rowsPanel.Controls)
            {
                if (!(ctrl is Panel row) || row.Controls.Count < 4) continue;
                var cmbColumn = row.Controls[0] as ComboBox;
                var cmbOperator = row.Controls[1] as ComboBox;
                var valueHost = row.Controls[2] as Panel;
                var txtValue = valueHost?.Controls.OfType<TextBox>().FirstOrDefault();
                var numValue = valueHost?.Controls.OfType<NumericUpDown>().FirstOrDefault();
                var dtPickers = valueHost?.Controls.OfType<DateTimePicker>().ToList() ?? new List<DateTimePicker>();
                var dtFrom = dtPickers.Count > 0 ? dtPickers[0] : null;
                var dtTo = dtPickers.Count > 1 ? dtPickers[1] : null;

                if (cmbColumn?.SelectedItem == null || cmbOperator?.SelectedItem == null) continue;
                string column = cmbColumn.SelectedItem.ToString();
                string op = cmbOperator.SelectedItem.ToString();
                if (!table.Columns.Contains(column)) continue;

                string clause = BuildClause(table, column, op, txtValue, numValue, dtFrom, dtTo);
                if (!string.IsNullOrWhiteSpace(clause))
                    clauses.Add(clause);
            }

            table.DefaultView.RowFilter = string.Join(" AND ", clauses);
        }

        private static string BuildClause(DataTable table, string column, string op, TextBox txtValue, NumericUpDown numValue, DateTimePicker dtFrom, DateTimePicker dtTo)
        {
            var colType = table.Columns[column].DataType;
            bool isDate = colType == typeof(DateTime) || column.IndexOf("Date", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isDate)
            {
                if (dtFrom == null) return string.Empty;

                string F(DateTime d) => d.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                if (op == "On")
                {
                    if (!dtFrom.Checked) return string.Empty;
                    var d = dtFrom.Value.Date;
                    return $"[{column}] >= #{F(d)}# AND [{column}] < #{F(d.AddDays(1))}#";
                }
                if (op == "From")
                {
                    if (!dtFrom.Checked) return string.Empty;
                    var d = dtFrom.Value.Date;
                    return $"[{column}] >= #{F(d)}#";
                }
                if (op == "To")
                {
                    if (!dtFrom.Checked) return string.Empty;
                    var d = dtFrom.Value.Date;
                    return $"[{column}] < #{F(d.AddDays(1))}#";
                }
                if (op == "Between")
                {
                    if (dtTo == null || !dtFrom.Checked || !dtTo.Checked) return string.Empty;
                    var from = dtFrom.Value.Date;
                    var to = dtTo.Value.Date;
                    if (to < from) (from, to) = (to, from);
                    return $"[{column}] >= #{F(from)}# AND [{column}] < #{F(to.AddDays(1))}#";
                }
                return string.Empty;
            }

            bool numeric = IsNumericType(colType);

            if (numeric)
            {
                if (op == "Between")
                {
                    string raw = (txtValue?.Text ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
                    var parts = raw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) return string.Empty;
                    if (!decimal.TryParse(parts[0], out var a) || !decimal.TryParse(parts[1], out var b)) return string.Empty;
                    if (b < a) (a, b) = (b, a);
                    return $"[{column}] >= {a.ToString(CultureInfo.InvariantCulture)} AND [{column}] <= {b.ToString(CultureInfo.InvariantCulture)}";
                }

                if (numValue == null) return string.Empty;
                string v = numValue.Value.ToString(CultureInfo.InvariantCulture);
                if (op == "Equals") return $"[{column}] = {v}";
                if (op == ">=") return $"[{column}] >= {v}";
                if (op == "<=") return $"[{column}] <= {v}";
                return string.Empty;
            }

            string rawText = (txtValue?.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;
            string escaped = rawText.Replace("'", "''");
            if (op == "Equals") return $"[{column}] = '{escaped}'";
            if (op == "StartsWith") return $"[{column}] LIKE '{escaped}%'";
            return $"[{column}] LIKE '%{escaped}%'";
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long)
                || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }

        private static bool IsIdLikeColumnName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string n = name.Trim();
            return n.Equals("ID", StringComparison.OrdinalIgnoreCase)
                || n.EndsWith(" ID", StringComparison.OrdinalIgnoreCase)
                || n.EndsWith("ID", StringComparison.OrdinalIgnoreCase)
                || n.EndsWith(" Id", StringComparison.OrdinalIgnoreCase)
                || n.EndsWith("Id", StringComparison.OrdinalIgnoreCase);
        }
    }
}
