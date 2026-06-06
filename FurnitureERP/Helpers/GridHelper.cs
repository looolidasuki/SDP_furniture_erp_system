using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class GridHelper
    {
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
            grid.DataBindingComplete += (s, e) =>
            {
                if (s is DataGridView boundGrid && e.ListChangedType == ListChangedType.Reset)
                    StyleGrid(boundGrid);
            };
            return grid;
        }

        public static void ApplyStyle(DataGridView grid) => StyleGrid(grid);

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
    }
}
