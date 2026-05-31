using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Sales_user.Controllers;
using FurnitureERP.Helpers;

namespace FurnitureERP.Forms
{
    // 💡 修正 1：繼承自 UserControl，這比 Panel 更穩定，能處理焦點與重繪
    public class SystemAdminPanel : UserControl
    {
        private readonly ProductController _productCtrl = new ProductController();
        private readonly SystemDictionaryController _dictCtrl = new SystemDictionaryController();
        private readonly RawMaterialController _rawMaterialCtrl = new RawMaterialController();
        private readonly string _module;
        private DataGridView _dictGrid;
        private DataGridView _productGrid;

        public SystemAdminPanel(string module = "System Admin")
        {
            _module = module;
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;

            // 💡 修正 2：在建構子內手動調用 BuildUI
            BuildUI();
        }

        private void BuildUI()
        {
            this.SuspendLayout(); // 防止佈局抖動
            try
            {
                // 主容器
                TableLayoutPanel mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(10) };
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

                // 建立四個卡片
                Panel dictCard = CreateSafeCard("System Dictionary");
                Panel prodCard = CreateSafeCard("Product Catalog");
                Panel sqCard = CreateSafeCard("Supplier Raw Material Quotes");
                Panel infoCard = CreateSafeCard("System Information");

                // 填入內容
                _dictGrid = GridHelper.CreateStyledGrid();
                _dictGrid.Dock = DockStyle.Fill;
                try { _dictGrid.DataSource = _dictCtrl.GetAllDictionaries(); GridHelper.StyleGrid(_dictGrid); } catch { }
                dictCard.Controls.Add(_dictGrid);

                _productGrid = GridHelper.CreateStyledGrid();
                _productGrid.Dock = DockStyle.Fill;
                try { _productGrid.DataSource = _productCtrl.GetAllProductsWithStock(StockAlertHelper.DefaultProductMinStock); GridHelper.StyleGridWithStockAlert(_productGrid, "Available Stock", "Min Stock Level"); } catch { }
                prodCard.Controls.Add(_productGrid);

                // 放入主佈局
                mainLayout.Controls.Add(dictCard, 0, 0);
                mainLayout.Controls.Add(prodCard, 1, 0);
                mainLayout.Controls.Add(sqCard, 0, 1);
                mainLayout.Controls.Add(infoCard, 1, 1);

                this.Controls.Add(mainLayout);
            }
            catch (Exception ex)
            {
                // 如果出錯，顯示紅字，你才知道哪裡掛了
                this.Controls.Add(new Label { Text = "Build Error: " + ex.Message, ForeColor = Color.Red, Dock = DockStyle.Fill });
            }
            this.ResumeLayout(true);
        }

        // 💡 修正 3：手動定義卡片，不依賴 UITheme 的 CreateCard 函數
        // 因為舊的 CreateCard 可能是導致排版失效的罪魁禍首
        private Panel CreateSafeCard(string title)
        {
            Panel p = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
            p.Paint += (s, e) => {
                using (var pen = new System.Drawing.Pen(Color.FromArgb(200, 200, 200)))
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            Label lbl = new Label { Text = title, Dock = DockStyle.Top, Font = new Font("Segoe UI", 9, FontStyle.Bold), Height = 25 };
            p.Controls.Add(lbl);
            return p;
        }
    }
}