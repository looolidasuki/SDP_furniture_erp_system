using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class StockAlertHelper
    {
        /// <summary>Default minimum stock when product has no per-SKU threshold in database.</summary>
        public const decimal DefaultProductMinStock = 5m;

        public static readonly Color CriticalBackColor = Color.FromArgb(255, 210, 210);
        public static readonly Color LowStockBackColor = Color.FromArgb(255, 243, 205);
        public static readonly Color CriticalForeColor = Color.FromArgb(140, 20, 20);
        public static readonly Color LowStockForeColor = Color.FromArgb(120, 80, 0);

        public static void WireStockLevelHighlight(
            DataGridView grid,
            string stockColumn,
            string minStockColumn,
            decimal fallbackMinStock = DefaultProductMinStock)
        {
            if (grid == null || string.IsNullOrWhiteSpace(stockColumn))
                return;

            grid.Tag = new StockHighlightContext(stockColumn, minStockColumn, fallbackMinStock);
            grid.DataBindingComplete -= Grid_DataBindingComplete;
            grid.DataBindingComplete += Grid_DataBindingComplete;
            ApplyStockLevelHighlight(grid, stockColumn, minStockColumn, fallbackMinStock);
        }

        private static void Grid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (e.ListChangedType != ListChangedType.Reset)
                return;
            if (!(sender is DataGridView grid) || !(grid.Tag is StockHighlightContext ctx))
                return;
            ApplyStockLevelHighlight(grid, ctx.StockColumn, ctx.MinStockColumn, ctx.FallbackMinStock);
        }

        public static void ApplyStockLevelHighlight(
            DataGridView grid,
            string stockColumn,
            string minStockColumn,
            decimal fallbackMinStock = DefaultProductMinStock)
        {
            if (grid == null || string.IsNullOrWhiteSpace(stockColumn))
                return;

            if (!grid.Columns.Contains(stockColumn))
                return;

            bool hasMinColumn = !string.IsNullOrWhiteSpace(minStockColumn)
                && grid.Columns.Contains(minStockColumn);

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;

                try
                {
                    decimal current = ToDecimal(row.Cells[stockColumn].Value);
                    decimal min = hasMinColumn ? ToDecimal(row.Cells[minStockColumn].Value) : fallbackMinStock;
                    if (min <= 0) min = fallbackMinStock;

                    ResetRowStyle(row, row.Index % 2 == 1);
                    ApplyRowAlertStyle(row, current, min);
                }
                catch { }
            }
        }

        private static void ResetRowStyle(DataGridViewRow row, bool alternate)
        {
            row.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = alternate ? Color.FromArgb(248, 250, 255) : Color.White,
                ForeColor = UITheme.TextDark,
                Font = new Font("Segoe UI", 8.5f),
                Padding = new Padding(4, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(210, 225, 255),
                SelectionForeColor = UITheme.TextDark
            };
        }

        private sealed class StockHighlightContext
        {
            public StockHighlightContext(string stockColumn, string minStockColumn, decimal fallbackMinStock)
            {
                StockColumn = stockColumn;
                MinStockColumn = minStockColumn;
                FallbackMinStock = fallbackMinStock;
            }

            public string StockColumn { get; }
            public string MinStockColumn { get; }
            public decimal FallbackMinStock { get; }
        }

        public static void ApplyRowAlertStyle(DataGridViewRow row, decimal currentStock, decimal minStock)
        {
            if (row == null) return;

            if (currentStock <= 0)
            {
                row.DefaultCellStyle.BackColor = CriticalBackColor;
                row.DefaultCellStyle.ForeColor = CriticalForeColor;
                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 180, 180);
            }
            else if (currentStock < minStock)
            {
                row.DefaultCellStyle.BackColor = LowStockBackColor;
                row.DefaultCellStyle.ForeColor = LowStockForeColor;
                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 230, 170);
            }
        }

        public static Label CreateLegendLabel()
        {
            return new Label
            {
                Text = "Red = out of stock   Amber = below minimum",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = UITheme.TextGray,
                Padding = new Padding(4, 8, 0, 0)
            };
        }

        private static decimal ToDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            return Convert.ToDecimal(value);
        }
    }
}
