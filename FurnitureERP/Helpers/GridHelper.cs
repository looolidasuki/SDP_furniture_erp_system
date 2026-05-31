using System.Drawing;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class GridHelper
    {
        public static DataGridView CreateStyledGrid()
        {
            return new DataGridView
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
        }

        public static void ApplyStyle(DataGridView grid) => StyleGrid(grid);

        public static void StyleGrid(DataGridView grid)
        {
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
        }

        public static void StyleGridWithStockAlert(DataGridView grid, string stockColumn, string minStockColumn)
        {
            StyleGrid(grid);
            StockAlertHelper.ApplyStockLevelHighlight(grid, stockColumn, minStockColumn);
        }
    }
}
