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
        private const int RowHeight = 36;
        private const int HeaderHeight = 42;
        private const int BlockPadding = 10;

        public static Panel CreateFilterBlock(DataGridView grid, string title = "Filters")
        {
            var box = new Panel
            {
                Dock = DockStyle.Top,
                Height = HeaderHeight + RowHeight + BlockPadding * 2 + 6,
                Padding = new Padding(BlockPadding),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 6)
            };
            box.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, box.Width - 1, box.Height - 1);
                using (var pen = new Pen(UITheme.CardBorder))
                    e.Graphics.DrawRectangle(pen, rect);
            };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = HeaderHeight,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = title,
                AutoSize = false,
                Dock = DockStyle.Left,
                Width = 260,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Padding = new Padding(4, 0, 0, 0)
            };

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 6, 0, 0)
            };

            var btnClear = UITheme.CreateSecondaryButton("Clear");
            btnClear.Width = 72;
            btnClear.Height = 28;
            var btnApply = UITheme.CreatePrimaryButton("Apply");
            btnApply.Width = 72;
            btnApply.Height = 28;
            var btnAdd = UITheme.CreateSecondaryButton("+ Condition");
            btnAdd.Width = 108;
            btnAdd.Height = 28;

            actions.Controls.Add(btnClear);
            actions.Controls.Add(btnApply);
            actions.Controls.Add(btnAdd);

            header.Controls.Add(actions);
            header.Controls.Add(lblTitle);

            var rowsPanel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(248, 250, 254),
                Padding = new Padding(6, 4, 6, 6)
            };
            rowsPanel.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, rowsPanel.Width - 1, rowsPanel.Height - 1);
                using (var pen = new Pen(Color.FromArgb(232, 236, 244)))
                    e.Graphics.DrawRectangle(pen, rect);
            };

            box.Controls.Add(rowsPanel);
            box.Controls.Add(header);

            Action resizeBlock = () =>
            {
                int rowsHeight = Math.Max(RowHeight, rowsPanel.Controls.Count * (RowHeight + 4) + 10);
                box.Height = HeaderHeight + rowsHeight + BlockPadding * 2 + 6;
            };

            Action addRow = () =>
            {
                var row = CreateConditionRow(grid, rowsPanel, resizeBlock);
                rowsPanel.Controls.Add(row);
                resizeBlock();
            };

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

        private static Panel CreateConditionRow(DataGridView grid, Panel rowsPanel, Action resizeBlock)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = RowHeight,
                ColumnCount = 4,
                Margin = new Padding(0, 0, 0, 4),
                BackColor = Color.Transparent
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14f));

            var cmbColumn = CreateFilterCombo();
            var cmbOperator = CreateFilterCombo();
            var valueHost = new Panel { Dock = DockStyle.Fill, Height = RowHeight - 4, Margin = new Padding(0, 2, 4, 0) };
            var txtValue = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
            var numValue = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                DecimalPlaces = 2,
                Maximum = 1000000000,
                Minimum = -1000000000,
                ThousandsSeparator = true,
                Visible = false,
                Font = new Font("Segoe UI", 9f)
            };
            var dtFrom = new DateTimePicker
            {
                Width = 120,
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = true,
                Checked = false,
                Visible = false,
                Font = new Font("Segoe UI", 9f)
            };
            var dtTo = new DateTimePicker
            {
                Width = 120,
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = true,
                Checked = false,
                Visible = false,
                Font = new Font("Segoe UI", 9f)
            };
            var dateFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Visible = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            dateFlow.Controls.Add(dtFrom);
            dateFlow.Controls.Add(dtTo);

            valueHost.Controls.Add(txtValue);
            valueHost.Controls.Add(numValue);
            valueHost.Controls.Add(dateFlow);

            var btnRemove = UITheme.CreateSecondaryButton("Remove");
            btnRemove.Dock = DockStyle.Fill;
            btnRemove.Height = 28;
            btnRemove.Margin = new Padding(0, 2, 0, 0);
            btnRemove.Click += (s, e) =>
            {
                rowsPanel.Controls.Remove(row);
                row.Dispose();
                resizeBlock();
            };

            PopulateColumns(cmbColumn, grid);
            cmbColumn.SelectedIndexChanged += (s, e) => ConfigureRowForColumn(grid, cmbColumn, cmbOperator, txtValue, numValue, dtFrom, dtTo, dateFlow);
            cmbOperator.SelectedIndexChanged += (s, e) => ConfigureValueControlsForOperator(grid, cmbColumn, cmbOperator, txtValue, numValue, dtFrom, dtTo, dateFlow);

            if (cmbColumn.Items.Count > 0)
                cmbColumn.SelectedIndex = 0;

            row.Controls.Add(cmbColumn, 0, 0);
            row.Controls.Add(cmbOperator, 1, 0);
            row.Controls.Add(valueHost, 2, 0);
            row.Controls.Add(btnRemove, 3, 0);
            return row;
        }

        private static ComboBox CreateFilterCombo()
        {
            return new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(0, 2, 4, 0)
            };
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

        private static void ConfigureRowForColumn(DataGridView grid, ComboBox cmbColumn, ComboBox cmbOperator, TextBox txtValue, NumericUpDown numValue, DateTimePicker dtFrom, DateTimePicker dtTo, FlowLayoutPanel dateFlow)
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
            ConfigureValueControlsForOperator(grid, cmbColumn, cmbOperator, txtValue, numValue, dtFrom, dtTo, dateFlow);
        }

        private static void ConfigureValueControlsForOperator(DataGridView grid, ComboBox cmbColumn, ComboBox cmbOperator, TextBox txtValue, NumericUpDown numValue, DateTimePicker dtFrom, DateTimePicker dtTo, FlowLayoutPanel dateFlow)
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
            dateFlow.Visible = isDate;
            dtFrom.Visible = isDate;
            dtTo.Visible = isDate && op == "Between";

            if (isDate)
            {
                dtFrom.Checked = false;
                dtTo.Checked = false;
            }
        }

        private static void ApplyFilter(DataGridView grid, Panel rowsPanel)
        {
            if (!(grid.DataSource is DataTable table)) return;
            var clauses = new List<string>();

            foreach (Control ctrl in rowsPanel.Controls)
            {
                if (!(ctrl is TableLayoutPanel row) || row.Controls.Count < 4) continue;
                var cmbColumn = row.Controls[0] as ComboBox;
                var cmbOperator = row.Controls[1] as ComboBox;
                var valueHost = row.Controls[2] as Panel;
                var txtValue = valueHost?.Controls.OfType<TextBox>().FirstOrDefault();
                var numValue = valueHost?.Controls.OfType<NumericUpDown>().FirstOrDefault();
                var dateFlow = valueHost?.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
                var dtPickers = dateFlow?.Controls.OfType<DateTimePicker>().ToList() ?? new List<DateTimePicker>();
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
