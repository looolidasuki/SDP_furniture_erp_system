using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class GridHelper
    {
        private sealed class StatusPresentationState
        {
            public string Category;
        }

        private sealed class TextStatusState
        {
            public string ColumnName;
        }

        private static readonly ConditionalWeakTable<DataGridView, StatusPresentationState> StatusPresentationStates =
            new ConditionalWeakTable<DataGridView, StatusPresentationState>();

        private static readonly ConditionalWeakTable<DataGridView, TextStatusState> TextStatusStates =
            new ConditionalWeakTable<DataGridView, TextStatusState>();

        public static DataGridView CreateStyledGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                BorderStyle = BorderStyle.None,
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(230, 235, 245),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                RowTemplate = { Height = 32 }
            };
            grid.DataBindingComplete += Grid_DataBindingComplete;
            return grid;
        }

        private static void Grid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (!(sender is DataGridView boundGrid) || e.ListChangedType != ListChangedType.Reset)
                return;

            StyleGrid(boundGrid);

            if (StatusPresentationStates.TryGetValue(boundGrid, out var state)
                && !string.IsNullOrWhiteSpace(state?.Category))
            {
                ApplyStatusPresentation(boundGrid, state.Category);
            }
        }

        public static void ApplyStyle(DataGridView grid) => StyleGrid(grid);

        public static DataTable DecorateStatusTable(DataTable source, string statusColumn, string category) =>
            DictionaryService.DecorateStatusColumn(source, statusColumn, category);

        public static void BindStatusData(DataGridView grid, DataTable data, string statusCategory)
        {
            if (grid == null) return;

            if (!string.IsNullOrWhiteSpace(statusCategory))
            {
                if (!StatusPresentationStates.TryGetValue(grid, out var state))
                {
                    state = new StatusPresentationState();
                    StatusPresentationStates.Add(grid, state);
                    grid.CellFormatting -= StatusGrid_CellFormatting;
                    grid.CellFormatting += StatusGrid_CellFormatting;
                }
                state.Category = statusCategory;
            }

            grid.DataSource = data;
            StyleGrid(grid);

            if (!string.IsNullOrWhiteSpace(statusCategory))
                ApplyStatusPresentation(grid, statusCategory);
        }

        public static void BindStatusData(DataGridView grid, DataTable raw, string statusColumn, string statusCategory)
        {
            if (raw == null)
            {
                BindStatusData(grid, (DataTable)null, statusCategory);
                return;
            }

            BindStatusData(grid, DecorateStatusTable(raw, statusColumn, statusCategory), statusCategory);
        }

        public static void LoadStatusData(DataGridView grid, Func<DataTable> loader, string statusColumn, string statusCategory)
        {
            if (loader == null) return;
            BindStatusData(grid, loader(), statusColumn, statusCategory);
        }

        public static void BindStatusWithStockAlert(
            DataGridView grid,
            DataTable raw,
            string statusColumn,
            string statusCategory,
            string stockColumn,
            string minStockColumn)
        {
            BindStatusData(grid, DecorateStatusTable(raw, statusColumn, statusCategory), statusCategory);
            StockAlertHelper.WireStockLevelHighlight(grid, stockColumn, minStockColumn);
        }

        public static void StyleStatusGrid(DataGridView grid, string statusCategory)
        {
            if (!string.IsNullOrWhiteSpace(statusCategory))
            {
                if (!StatusPresentationStates.TryGetValue(grid, out var state))
                {
                    state = new StatusPresentationState();
                    StatusPresentationStates.Add(grid, state);
                    grid.CellFormatting -= StatusGrid_CellFormatting;
                    grid.CellFormatting += StatusGrid_CellFormatting;
                }
                state.Category = statusCategory;
            }

            StyleGrid(grid);
            ApplyStatusPresentation(grid, statusCategory);
        }

        public static void ApplyStatusPresentation(DataGridView grid, string statusCategory)
        {
            if (grid == null || string.IsNullOrWhiteSpace(statusCategory))
                return;

            if (!StatusPresentationStates.TryGetValue(grid, out var state))
            {
                state = new StatusPresentationState { Category = statusCategory };
                StatusPresentationStates.Add(grid, state);
                grid.CellFormatting -= StatusGrid_CellFormatting;
                grid.CellFormatting += StatusGrid_CellFormatting;
            }
            else
            {
                state.Category = statusCategory;
            }

            string labelColumn = FindStatusLabelColumn(grid);
            if (string.IsNullOrEmpty(labelColumn))
                return;

            var statusCol = ResolveGridColumn(grid, "Status");
            if (statusCol != null)
                statusCol.Visible = false;

            var labelCol = ResolveGridColumn(grid, labelColumn);
            if (labelCol == null)
                return;

            labelCol.HeaderText = "Status";
            labelCol.DisplayIndex = Math.Max(0, grid.Columns.Count - 1);
        }

        public static void StyleTextStatusColumn(DataGridView grid, string columnName = "Status")
        {
            if (grid == null || string.IsNullOrWhiteSpace(columnName))
                return;

            if (!TextStatusStates.TryGetValue(grid, out var state))
            {
                state = new TextStatusState();
                TextStatusStates.Add(grid, state);
                grid.CellFormatting -= TextStatusGrid_CellFormatting;
                grid.CellFormatting += TextStatusGrid_CellFormatting;
            }
            state.ColumnName = columnName;

            StyleGrid(grid);
        }

        public static void StyleGrid(DataGridView grid)
        {
            if (grid == null || grid.Columns.Count == 0) return;

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = UITheme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Padding = new Padding(4, 0, 0, 0)
            };
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = UITheme.TextDark,
                Font = new Font("Segoe UI", 8.5f),
                Padding = new Padding(4, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(210, 225, 255),
                SelectionForeColor = UITheme.TextDark
            };
            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 255)
            };
            grid.EnableHeadersVisualStyles = false;

            var codeColumns = new List<DataGridViewColumn>();
            var otherColumns = new List<DataGridViewColumn>();

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col == null) continue;
                if (col is DataGridViewComboBoxColumn)
                {
                    otherColumns.Add(col);
                    continue;
                }

                if (IsIdColumn(col))
                {
                    col.Visible = false;
                    continue;
                }

                if (IsCodeColumn(col))
                    codeColumns.Add(col);
                else
                    otherColumns.Add(col);
            }

            int displayIndex = 0;
            foreach (var col in codeColumns)
                col.DisplayIndex = displayIndex++;
            foreach (var col in otherColumns.Where(c => c.Visible))
                col.DisplayIndex = displayIndex++;
        }

        public static long TryGetRowLongId(DataGridView grid, DataGridViewRow row, params string[] preferredIdColumns)
        {
            if (grid == null || row == null) return 0;

            if (preferredIdColumns != null)
            {
                foreach (string columnName in preferredIdColumns)
                {
                    if (string.IsNullOrWhiteSpace(columnName) || !grid.Columns.Contains(columnName)) continue;
                    if (TryParseCellLong(row.Cells[columnName]?.Value, out long preferredId))
                        return preferredId;
                }
            }

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (!IsIdColumn(col)) continue;
                if (TryParseCellLong(row.Cells[col.Name]?.Value, out long id))
                    return id;
            }

            if (row.Cells.Count > 0 && TryParseCellLong(row.Cells[0].Value, out long fallbackId))
                return fallbackId;

            return 0;
        }

        public static void StyleGridWithStockAlert(DataGridView grid, string stockColumn, string minStockColumn)
        {
            StyleGrid(grid);
            StockAlertHelper.WireStockLevelHighlight(grid, stockColumn, minStockColumn);
        }

        public static void ConfigureProductCatalogueGrid(DataGridView grid)
        {
            if (grid == null || grid.Columns.Count == 0) return;

            grid.ColumnHeadersHeight = 34;
            grid.RowTemplate.Height = 30;
            grid.ScrollBars = ScrollBars.Both;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var weights = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["Product Code"] = 130f,
                ["Category"] = 95f,
                ["Style Number"] = 120f,
                ["Size"] = 95f,
                ["Color"] = 85f,
                ["Base Price"] = 90f,
                ["Unit"] = 55f,
                ["Status"] = 70f,
                ["Total Stock"] = 85f,
                ["Available Stock"] = 105f,
                ["Min Stock Level"] = 105f
            };

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (!col.Visible) continue;
                col.MinimumWidth = 52;
                col.FillWeight = weights.TryGetValue(col.HeaderText, out float weight) ? weight : 80f;
            }
        }

        private static bool TryParseCellLong(object value, out long id)
        {
            id = 0;
            if (value == null || value == DBNull.Value) return false;
            return long.TryParse(value.ToString(), out id);
        }

        private static bool IsIdColumn(DataGridViewColumn col)
        {
            string header = (col.HeaderText ?? string.Empty).Trim();
            string name = (col.Name ?? col.DataPropertyName ?? string.Empty).Trim();
            return IsIdLikeName(header) || IsIdLikeName(name);
        }

        private static bool IsCodeColumn(DataGridViewColumn col)
        {
            string header = (col.HeaderText ?? string.Empty).Trim();
            string name = (col.Name ?? col.DataPropertyName ?? string.Empty).Trim();
            return header.IndexOf("Code", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Code", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsIdLikeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return name.Equals("ID", System.StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(" ID", System.StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("Id", System.StringComparison.OrdinalIgnoreCase)
                || (name.Length > 2 && name.EndsWith("ID", System.StringComparison.OrdinalIgnoreCase));
        }

        private static DataGridViewColumn ResolveGridColumn(DataGridView grid, string columnName)
        {
            if (grid?.Columns == null || string.IsNullOrWhiteSpace(columnName) || grid.Columns.Count == 0)
                return null;

            if (grid.Columns.Contains(columnName))
                return grid.Columns[columnName];

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (string.Equals(col.DataPropertyName, columnName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(col.Name, columnName, StringComparison.OrdinalIgnoreCase))
                    return col;
            }

            return null;
        }

        private static string FindStatusLabelColumn(DataGridView grid)
        {
            if (grid?.Columns == null || grid.Columns.Count == 0)
                return null;

            var labelCol = ResolveGridColumn(grid, "Status Label");
            if (labelCol != null)
                return labelCol.DataPropertyName ?? labelCol.Name;

            foreach (DataGridViewColumn col in grid.Columns)
            {
                string header = col.HeaderText ?? string.Empty;
                if (header.Equals("Status Label", StringComparison.OrdinalIgnoreCase))
                    return col.DataPropertyName ?? col.Name;
            }

            return null;
        }

        private static void StatusGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null || e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (!StatusPresentationStates.TryGetValue(grid, out var state)
                || string.IsNullOrWhiteSpace(state?.Category))
                return;

            string category = state.Category;

            string labelColumn = FindStatusLabelColumn(grid);
            if (string.IsNullOrEmpty(labelColumn))
                return;

            var column = grid.Columns[e.ColumnIndex];
            if (!string.Equals(column.Name, labelColumn, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(column.HeaderText, "Status", StringComparison.OrdinalIgnoreCase))
                return;

            int statusCode = TryGetStatusCodeFromRow(grid, e.RowIndex);
            var colors = StatusStyleHelper.GetColors(category, statusCode);
            e.CellStyle.BackColor = colors.Background;
            e.CellStyle.ForeColor = colors.Foreground;
            e.CellStyle.SelectionBackColor = colors.Background;
            e.CellStyle.SelectionForeColor = colors.Foreground;
        }

        private static void TextStatusGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null || e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (!TextStatusStates.TryGetValue(grid, out var state)
                || string.IsNullOrWhiteSpace(state?.ColumnName))
                return;

            var column = grid.Columns[e.ColumnIndex];
            if (!string.Equals(column.Name, state.ColumnName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(column.DataPropertyName, state.ColumnName, StringComparison.OrdinalIgnoreCase))
                return;

            var colors = StatusStyleHelper.GetColorsByLabel(e.Value?.ToString());
            e.CellStyle.BackColor = colors.Background;
            e.CellStyle.ForeColor = colors.Foreground;
            e.CellStyle.SelectionBackColor = colors.Background;
            e.CellStyle.SelectionForeColor = colors.Foreground;
        }

        private static int TryGetStatusCodeFromRow(DataGridView grid, int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count)
                return 0;

            var row = grid.Rows[rowIndex];
            if (row.DataBoundItem is DataRowView drv && drv.Row.Table.Columns.Contains("Status")
                && drv["Status"] != DBNull.Value)
            {
                return Convert.ToInt32(drv["Status"]);
            }

            if (grid.Columns.Contains("Status"))
            {
                object value = row.Cells["Status"]?.Value;
                if (value != null && value != DBNull.Value)
                    return Convert.ToInt32(value);
            }

            return 0;
        }

        public static DataTable CopyGridToDataTable(DataGridView grid)
        {
            var table = new DataTable();
            if (grid == null) return table;

            var visibleColumns = new System.Collections.Generic.List<DataGridViewColumn>();
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (!col.Visible) continue;
                visibleColumns.Add(col);
                table.Columns.Add(string.IsNullOrWhiteSpace(col.HeaderText) ? col.Name : col.HeaderText);
            }

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;
                var values = new object[visibleColumns.Count];
                for (int i = 0; i < visibleColumns.Count; i++)
                    values[i] = row.Cells[visibleColumns[i].Index].FormattedValue ?? row.Cells[visibleColumns[i].Index].Value ?? DBNull.Value;
                table.Rows.Add(values);
            }

            return table;
        }
    }
}
