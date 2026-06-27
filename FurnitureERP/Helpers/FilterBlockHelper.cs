using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class FilterBlockHelper
    {
        private const int RowHeight = 36;
        private const int HeaderHeight = 42;
        private const int BlockPadding = 10;

        private sealed class FilterBlockContext
        {
            public string StatusCategory;
            public Panel RowsPanel;
            public Action ResizeBlock;
            public bool UseServerSuggest;
        }

        private sealed class FilterColumnOption
        {
            public FilterColumnOption(string columnName, string displayName = null)
            {
                ColumnName = columnName;
                DisplayName = displayName ?? columnName;
            }

            public string ColumnName { get; }
            public string DisplayName { get; }
            public override string ToString() => DisplayName;
        }

        private sealed class FilterRowTag
        {
            public ComboBox ColumnCombo;
            public ComboBox OperatorCombo;
            public ComboBox TextSuggest;
            public FilterTextSuggestBinder TextSuggestBinder;
            public TextBox TextValue;
            public NumericUpDown NumValue;
            public DateTimePicker DateFrom;
            public DateTimePicker DateTo;
            public FlowLayoutPanel DateFlow;
            public ComboBox StatusValueCombo;
            public Panel ValueHost;
            public string StatusCategory;
            public bool UseServerSuggest;
            public bool DateFilterActive;
        }

        private static readonly ConditionalWeakTable<DataGridView, FilterBlockContext> FilterContexts =
            new ConditionalWeakTable<DataGridView, FilterBlockContext>();

        public static Panel CreateFilterBlock(DataGridView grid, string title = "Filters", string statusCategory = null,
            Action<DocumentListFilter> onServerApply = null, int serverPageSize = 100)
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

            var btnClear = UITheme.CreateDangerButton("Clear");
            btnClear.Width = 72;
            btnClear.Height = 28;
            var btnExport = UITheme.CreateSecondaryButton("Export CSV");
            btnExport.Width = 96;
            btnExport.Height = 28;
            var btnRemove = UITheme.CreateDangerButton("Remove");
            btnRemove.Width = 80;
            btnRemove.Height = 28;
            var btnAdd = UITheme.CreateSecondaryButton("+ Condition");
            btnAdd.Width = 108;
            btnAdd.Height = 28;

            actions.Controls.Add(btnRemove);
            actions.Controls.Add(btnExport);
            actions.Controls.Add(btnClear);
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

            var context = new FilterBlockContext
            {
                StatusCategory = statusCategory,
                RowsPanel = rowsPanel,
                ResizeBlock = resizeBlock,
                UseServerSuggest = onServerApply != null
            };
            FilterContexts.Add(grid, context);

            Action applyFilters = () =>
            {
                foreach (Control ctrl in rowsPanel.Controls)
                {
                    if (ctrl is TableLayoutPanel row && row.Tag is FilterRowTag tag && tag.DateFlow.Visible)
                        tag.DateFilterActive = true;
                }

                if (onServerApply != null)
                    onServerApply(BuildDocumentListFilter(rowsPanel, 1, serverPageSize, statusCategory));
                else
                    ApplyFilter(grid, rowsPanel);
            };

            Action addRow = () =>
            {
                var row = CreateConditionRow(grid, rowsPanel, resizeBlock, statusCategory, applyFilters, context.UseServerSuggest);
                rowsPanel.Controls.Add(row);
                resizeBlock();
            };

            btnAdd.Click += (s, e) => addRow();
            btnExport.Click += (s, e) =>
            {
                var owner = grid.FindForm();
                CsvExportHelper.ExportDataGridView(grid, title + "_export", owner);
            };
            btnRemove.Click += (s, e) =>
            {
                if (rowsPanel.Controls.Count <= 1) return;
                var last = rowsPanel.Controls[rowsPanel.Controls.Count - 1];
                rowsPanel.Controls.Remove(last);
                last.Dispose();
                resizeBlock();
                applyFilters();
            };
            btnClear.Click += (s, e) =>
            {
                if (onServerApply == null && grid.DataSource is DataTable table)
                    table.DefaultView.RowFilter = string.Empty;
                rowsPanel.Controls.Clear();
                addRow();
                if (onServerApply != null)
                    onServerApply(new DocumentListFilter { Page = 1, PageSize = serverPageSize });
            };

            grid.DataBindingComplete -= Grid_FilterDataBindingComplete;
            grid.DataBindingComplete += Grid_FilterDataBindingComplete;

            addRow();
            return box;
        }

        private static void Grid_FilterDataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (!(sender is DataGridView grid) || e.ListChangedType != ListChangedType.Reset)
                return;
            if (!FilterContexts.TryGetValue(grid, out var context) || context?.RowsPanel == null)
                return;

            RefreshAllFilterRows(grid, context);
        }

        private static void RefreshAllFilterRows(DataGridView grid, FilterBlockContext context)
        {
            foreach (Control ctrl in context.RowsPanel.Controls)
            {
                if (!(ctrl is TableLayoutPanel row) || !(row.Tag is FilterRowTag tag))
                    continue;

                string selectedColumn = GetSelectedColumnName(tag.ColumnCombo);
                PopulateColumns(tag.ColumnCombo, grid, context.StatusCategory);
                RestoreColumnSelection(tag.ColumnCombo, selectedColumn);
                ConfigureRowForColumn(grid, tag, context.StatusCategory);
                if (tag.TextSuggest.Visible)
                    RefreshTextSuggestSource(grid, tag, GetSelectedColumnName(tag.ColumnCombo));
            }
        }

        private static void RestoreColumnSelection(ComboBox cmbColumn, string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName) || cmbColumn.Items.Count == 0)
            {
                if (cmbColumn.Items.Count > 0)
                    cmbColumn.SelectedIndex = 0;
                return;
            }

            for (int i = 0; i < cmbColumn.Items.Count; i++)
            {
                if (cmbColumn.Items[i] is FilterColumnOption opt
                    && string.Equals(opt.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    cmbColumn.SelectedIndex = i;
                    return;
                }
            }

            cmbColumn.SelectedIndex = 0;
        }

        private static string GetSelectedColumnName(ComboBox cmbColumn)
        {
            if (cmbColumn?.SelectedItem is FilterColumnOption opt)
                return opt.ColumnName;
            return cmbColumn?.SelectedItem?.ToString();
        }

        private static Panel CreateConditionRow(DataGridView grid, Panel rowsPanel, Action resizeBlock, string statusCategory, Action applyFilters, bool useServerSuggest)
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
            var cmbTextSuggest = new ComboBox { Dock = DockStyle.Fill, Visible = true };
            var textSuggestBinder = new FilterTextSuggestBinder(cmbTextSuggest);
            var txtValue = new TextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f), Visible = false };
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
            var cmbStatusValue = CreateFilterCombo();
            cmbStatusValue.Visible = false;
            var dtFrom = new DateTimePicker
            {
                Width = 120,
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = false,
                Visible = false,
                Font = new Font("Segoe UI", 9f)
            };
            var dtTo = new DateTimePicker
            {
                Width = 120,
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = false,
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

            valueHost.Controls.Add(cmbTextSuggest);
            valueHost.Controls.Add(txtValue);
            valueHost.Controls.Add(numValue);
            valueHost.Controls.Add(cmbStatusValue);
            valueHost.Controls.Add(dateFlow);

            var btnApply = UITheme.CreatePrimaryButton("Apply");
            btnApply.Dock = DockStyle.Fill;
            btnApply.Height = 28;
            btnApply.Margin = new Padding(0, 2, 0, 0);
            btnApply.Click += (s, e) => applyFilters();

            var tag = new FilterRowTag
            {
                ColumnCombo = cmbColumn,
                OperatorCombo = cmbOperator,
                TextSuggest = cmbTextSuggest,
                TextSuggestBinder = textSuggestBinder,
                TextValue = txtValue,
                NumValue = numValue,
                DateFrom = dtFrom,
                DateTo = dtTo,
                DateFlow = dateFlow,
                StatusValueCombo = cmbStatusValue,
                ValueHost = valueHost,
                StatusCategory = statusCategory,
                UseServerSuggest = useServerSuggest
            };
            row.Tag = tag;

            dtFrom.ValueChanged += (s, e) => tag.DateFilterActive = true;
            dtTo.ValueChanged += (s, e) => tag.DateFilterActive = true;

            PopulateColumns(cmbColumn, grid, statusCategory);
            cmbColumn.SelectedIndexChanged += (s, e) => ConfigureRowForColumn(grid, tag, statusCategory);
            cmbOperator.SelectedIndexChanged += (s, e) => ConfigureValueControlsForOperator(grid, tag);

            if (cmbColumn.Items.Count > 0)
                cmbColumn.SelectedIndex = 0;

            row.Controls.Add(cmbColumn, 0, 0);
            row.Controls.Add(cmbOperator, 1, 0);
            row.Controls.Add(valueHost, 2, 0);
            row.Controls.Add(btnApply, 3, 0);
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

        private static void PopulateColumns(ComboBox cmbColumn, DataGridView grid, string statusCategory)
        {
            string selected = GetSelectedColumnName(cmbColumn);
            cmbColumn.Items.Clear();

            foreach (var option in GetFilterableColumnOptions(grid, statusCategory))
                cmbColumn.Items.Add(option);

            RestoreColumnSelection(cmbColumn, selected);
        }

        private static List<FilterColumnOption> GetFilterableColumnOptions(DataGridView grid, string statusCategory)
        {
            var options = new List<FilterColumnOption>();
            if (grid == null) return options;

            var table = grid.DataSource as DataTable;
            bool hasStatusLabel = table?.Columns.Contains("Status Label") == true;
            bool hasTextStatus = !hasStatusLabel && table?.Columns.Contains("Status") == true
                && table.Columns["Status"].DataType == typeof(string);

            if (table != null)
            {
                foreach (DataColumn col in table.Columns)
                {
                    if (IsIdLikeColumnName(col.ColumnName)) continue;
                    if (hasStatusLabel && string.Equals(col.ColumnName, "Status", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (string.Equals(col.ColumnName, "Status Label", StringComparison.OrdinalIgnoreCase))
                        options.Add(new FilterColumnOption("Status Label", "Status"));
                    else if (hasTextStatus && string.Equals(col.ColumnName, "Status", StringComparison.OrdinalIgnoreCase))
                        options.Add(new FilterColumnOption("Status", "Status"));
                    else
                        options.Add(new FilterColumnOption(col.ColumnName));
                }
                return options;
            }

            bool gridHasStatusLabel = grid.Columns.Cast<DataGridViewColumn>()
                .Any(c => string.Equals(c.Name, "Status Label", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c.DataPropertyName, "Status Label", StringComparison.OrdinalIgnoreCase));

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (!col.Visible) continue;
                string name = string.IsNullOrWhiteSpace(col.DataPropertyName) ? col.Name : col.DataPropertyName;
                if (string.IsNullOrWhiteSpace(name) || IsIdLikeColumnName(name)) continue;
                if (gridHasStatusLabel && string.Equals(name, "Status", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.Equals(name, "Status Label", StringComparison.OrdinalIgnoreCase))
                    options.Add(new FilterColumnOption("Status Label", "Status"));
                else
                    options.Add(new FilterColumnOption(name));
            }

            return options;
        }

        private static void ConfigureRowForColumn(DataGridView grid, FilterRowTag tag, string statusCategory)
        {
            if (!(grid.DataSource is DataTable table)) return;
            if (tag?.ColumnCombo?.SelectedItem == null) return;

            string colName = GetSelectedColumnName(tag.ColumnCombo);
            if (string.IsNullOrWhiteSpace(colName) || !table.Columns.Contains(colName)) return;

            bool isStatusLabel = string.Equals(colName, "Status Label", StringComparison.OrdinalIgnoreCase);
            bool isTextStatus = string.Equals(colName, "Status", StringComparison.OrdinalIgnoreCase)
                && table.Columns[colName].DataType == typeof(string);

            tag.OperatorCombo.Items.Clear();
            if (isStatusLabel && !string.IsNullOrWhiteSpace(statusCategory))
            {
                tag.OperatorCombo.Items.Add("Equals");
                tag.OperatorCombo.SelectedIndex = 0;
                BindStatusValueCombo(tag.StatusValueCombo, statusCategory, includeAny: true);
            }
            else if (isTextStatus)
            {
                tag.OperatorCombo.Items.Add("Equals");
                tag.OperatorCombo.SelectedIndex = 0;
                BindDistinctTextStatusCombo(tag.StatusValueCombo, table, "Status", includeAny: true);
            }
            else
            {
                var type = table.Columns[colName].DataType;
                bool isDate = type == typeof(DateTime) || colName.IndexOf("Date", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isNumber = IsNumericType(type);

                if (isDate)
                    tag.OperatorCombo.Items.AddRange(new object[] { "On", "From", "To", "Between" });
                else if (isNumber)
                    tag.OperatorCombo.Items.AddRange(new object[] { "Equals", ">=", "<=", "Between" });
                else
                    tag.OperatorCombo.Items.AddRange(new object[] { "Contains", "Equals", "StartsWith" });

                if (isDate)
                    tag.DateFilterActive = false;

                if (tag.OperatorCombo.Items.Count > 0)
                    tag.OperatorCombo.SelectedIndex = 0;
            }

            ConfigureValueControlsForOperator(grid, tag);
            if (tag.TextSuggest.Visible)
                RefreshTextSuggestSource(grid, tag, colName);
        }

        private static void BindStatusValueCombo(ComboBox combo, string statusCategory, bool includeAny)
        {
            combo.Items.Clear();
            if (includeAny)
                combo.Items.Add("(Any)");
            foreach (var item in DictionaryService.GetItems(statusCategory))
                combo.Items.Add(item.Value);
            combo.SelectedIndex = 0;
        }

        private static void BindDistinctTextStatusCombo(ComboBox combo, DataTable table, string column, bool includeAny)
        {
            combo.Items.Clear();
            if (includeAny)
                combo.Items.Add("(Any)");
            var values = table.Rows.Cast<DataRow>()
                .Select(r => r[column]?.ToString())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
                combo.Items.Add(value);
            combo.SelectedIndex = 0;
        }

        private static void ConfigureValueControlsForOperator(DataGridView grid, FilterRowTag tag)
        {
            if (!(grid.DataSource is DataTable table)) return;
            if (tag?.ColumnCombo?.SelectedItem == null || tag.OperatorCombo?.SelectedItem == null) return;

            string colName = GetSelectedColumnName(tag.ColumnCombo);
            if (string.IsNullOrWhiteSpace(colName) || !table.Columns.Contains(colName)) return;

            bool isStatusLabel = string.Equals(colName, "Status Label", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(tag.StatusCategory);
            bool isTextStatus = string.Equals(colName, "Status", StringComparison.OrdinalIgnoreCase)
                && table.Columns[colName].DataType == typeof(string);

            if (isStatusLabel || isTextStatus)
            {
                tag.TextSuggest.Visible = false;
                tag.TextValue.Visible = false;
                tag.NumValue.Visible = false;
                tag.DateFlow.Visible = false;
                tag.StatusValueCombo.Visible = true;
                tag.StatusValueCombo.Dock = DockStyle.Fill;
                return;
            }

            var type = table.Columns[colName].DataType;
            bool isDate = type == typeof(DateTime) || colName.IndexOf("Date", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isNumber = IsNumericType(type);
            string op = tag.OperatorCombo.SelectedItem.ToString();

            tag.StatusValueCombo.Visible = false;
            tag.TextSuggest.Visible = !isDate && !isNumber;
            tag.TextValue.Visible = !isDate && isNumber && op == "Between";
            tag.NumValue.Visible = !isDate && isNumber && op != "Between";
            tag.DateFlow.Visible = isDate;
            tag.DateFrom.Visible = isDate;
            tag.DateTo.Visible = isDate && op == "Between";

            if (tag.TextSuggest.Visible)
                RefreshTextSuggestSource(grid, tag, colName);
        }

        private static void ApplyFilter(DataGridView grid, Panel rowsPanel)
        {
            if (!(grid.DataSource is DataTable table)) return;
            var clauses = new List<string>();

            foreach (Control ctrl in rowsPanel.Controls)
            {
                if (!(ctrl is TableLayoutPanel row) || !(row.Tag is FilterRowTag tag)) continue;

                if (tag.ColumnCombo?.SelectedItem == null || tag.OperatorCombo?.SelectedItem == null) continue;
                string column = GetSelectedColumnName(tag.ColumnCombo);
                string op = tag.OperatorCombo.SelectedItem.ToString();
                if (!table.Columns.Contains(column)) continue;

                string clause = BuildClause(table, column, op, tag);
                if (!string.IsNullOrWhiteSpace(clause))
                    clauses.Add(clause);
            }

            table.DefaultView.RowFilter = string.Join(" AND ", clauses);
        }

        private static string BuildClause(DataTable table, string column, string op, FilterRowTag tag)
        {
            if (tag.StatusValueCombo.Visible)
            {
                string selected = tag.StatusValueCombo.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(selected) || selected == "(Any)")
                    return string.Empty;
                string escaped = selected.Replace("'", "''");
                return $"[{column}] = '{escaped}'";
            }

            return BuildClause(table, column, op, tag.TextSuggestBinder, tag.TextValue, tag.NumValue, tag.DateFrom, tag.DateTo, tag.DateFilterActive);
        }

        private static string BuildClause(DataTable table, string column, string op, FilterTextSuggestBinder textSuggest, TextBox betweenText, NumericUpDown numValue, DateTimePicker dtFrom, DateTimePicker dtTo, bool dateFilterActive)
        {
            var colType = table.Columns[column].DataType;
            bool isDate = colType == typeof(DateTime) || column.IndexOf("Date", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isDate)
            {
                if (dtFrom == null || !dateFilterActive) return string.Empty;

                bool asString = colType == typeof(string);
                string F(DateTime d) => asString
                    ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : d.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                string Lit(DateTime d) => asString ? $"'{F(d)}'" : $"#{F(d)}#";

                if (op == "On")
                {
                    var d = dtFrom.Value.Date;
                    return $"[{column}] >= {Lit(d)} AND [{column}] < {Lit(d.AddDays(1))}";
                }
                if (op == "From")
                {
                    var d = dtFrom.Value.Date;
                    return $"[{column}] >= {Lit(d)}";
                }
                if (op == "To")
                {
                    var d = dtFrom.Value.Date;
                    return $"[{column}] < {Lit(d.AddDays(1))}";
                }
                if (op == "Between")
                {
                    if (dtTo == null) return string.Empty;
                    var from = dtFrom.Value.Date;
                    var to = dtTo.Value.Date;
                    if (to < from) (from, to) = (to, from);
                    return $"[{column}] >= {Lit(from)} AND [{column}] < {Lit(to.AddDays(1))}";
                }
                return string.Empty;
            }

            bool numeric = IsNumericType(colType);

            if (numeric)
            {
                if (op == "Between")
                {
                    string raw = (betweenText?.Text ?? string.Empty).Trim();
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

            string rawText = (textSuggest?.GetText() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;
            string escaped = rawText.Replace("'", "''");
            if (op == "Equals") return $"[{column}] = '{escaped}'";
            if (op == "StartsWith") return $"[{column}] LIKE '{escaped}%'";
            return $"[{column}] LIKE '%{escaped}%'";
        }

        private static void RefreshTextSuggestSource(DataGridView grid, FilterRowTag tag, string columnName)
        {
            if (tag?.TextSuggestBinder == null || string.IsNullOrWhiteSpace(columnName))
                return;

            tag.TextSuggestBinder.SetLocalSource(GetDistinctColumnValues(grid, columnName));
            if (!string.IsNullOrWhiteSpace(tag.StatusCategory)
                && FilterColumnSuggestService.CanSuggest(tag.StatusCategory, columnName))
            {
                string category = tag.StatusCategory;
                tag.TextSuggestBinder.SetServerSuggest(prefix =>
                    FilterColumnSuggestService.Suggest(category, columnName, prefix));
            }
            else
            {
                tag.TextSuggestBinder.SetServerSuggest(null);
            }
        }

        private static IEnumerable<string> GetDistinctColumnValues(DataGridView grid, string column)
        {
            if (!(grid.DataSource is DataTable table) || !table.Columns.Contains(column))
                yield break;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in table.Rows)
            {
                string value = row[column]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(value) && seen.Add(value))
                    yield return value;
            }
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

        public static DocumentListFilter BuildDocumentListFilter(Panel rowsPanel, int page, int pageSize, string statusCategory = null)
        {
            var filter = new DocumentListFilter
            {
                Page = Math.Max(1, page),
                PageSize = pageSize
            };

            foreach (Control ctrl in rowsPanel.Controls)
            {
                if (!(ctrl is TableLayoutPanel row) || !(row.Tag is FilterRowTag tag)) continue;
                if (tag.ColumnCombo?.SelectedItem == null || tag.OperatorCombo?.SelectedItem == null) continue;

                string column = GetSelectedColumnName(tag.ColumnCombo);
                string op = tag.OperatorCombo.SelectedItem.ToString();
                var condition = ExtractServerCondition(tag, column, op, statusCategory);
                if (condition != null)
                    filter.Conditions.Add(condition);
            }

            return filter;
        }

        private static DocumentFilterCondition ExtractServerCondition(FilterRowTag tag, string column, string op, string statusCategory)
        {
            if (tag.StatusValueCombo.Visible)
            {
                string selected = tag.StatusValueCombo.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(selected) || selected == "(Any)")
                    return null;

                if (string.Equals(column, "Status Label", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(statusCategory))
                {
                    foreach (var item in DictionaryService.GetItems(statusCategory))
                    {
                        if (string.Equals(item.Value, selected, StringComparison.OrdinalIgnoreCase))
                        {
                            return new DocumentFilterCondition
                            {
                                Column = column,
                                Operator = "Equals",
                                StatusCode = item.Key
                            };
                        }
                    }
                    return null;
                }

                return new DocumentFilterCondition
                {
                    Column = column,
                    Operator = "Equals",
                    TextValue = selected
                };
            }

            if (tag.DateFlow.Visible)
            {
                if (!tag.DateFilterActive) return null;

                var cond = new DocumentFilterCondition { Column = column, Operator = op };
                if (op == "On")
                {
                    cond.Operator = "On";
                    cond.DateFrom = tag.DateFrom.Value.Date;
                    cond.DateTo = tag.DateFrom.Value.Date;
                }
                else if (op == "From")
                {
                    cond.DateFrom = tag.DateFrom.Value.Date;
                }
                else if (op == "To")
                {
                    cond.DateFrom = tag.DateFrom.Value.Date;
                }
                else if (op == "Between")
                {
                    cond.DateFrom = tag.DateFrom.Value.Date;
                    cond.DateTo = tag.DateTo.Value.Date;
                }
                else
                {
                    return null;
                }
                return cond;
            }

            if (tag.NumValue.Visible || (tag.TextValue.Visible && op == "Between"))
            {
                if (op == "Between")
                {
                    string raw = (tag.TextValue?.Text ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(raw)) return null;
                    var parts = raw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) return null;
                    if (!decimal.TryParse(parts[0], out var a) || !decimal.TryParse(parts[1], out var b)) return null;
                    if (b < a) (a, b) = (b, a);
                    return new DocumentFilterCondition
                    {
                        Column = column,
                        Operator = op,
                        NumericValue = a,
                        NumericValueTo = b
                    };
                }

                return new DocumentFilterCondition
                {
                    Column = column,
                    Operator = op,
                    NumericValue = tag.NumValue.Value
                };
            }

            if (tag.TextSuggest.Visible)
            {
                string text = tag.TextSuggestBinder?.GetText()?.Trim();
                if (string.IsNullOrWhiteSpace(text)) return null;
                return new DocumentFilterCondition
                {
                    Column = column,
                    Operator = op,
                    TextValue = text
                };
            }

            return null;
        }
    }
}
