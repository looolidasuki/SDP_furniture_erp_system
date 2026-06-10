using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Sales_user.Controllers;
using FurnitureERP.Helpers;

namespace FurnitureERP.Forms
{
    public class SystemAdminPanel : UserControl
    {
        private readonly ProductController _productCtrl = new ProductController();
        private readonly SystemDictionaryController _dictCtrl = new SystemDictionaryController();
        private readonly RawMaterialController _rawMaterialCtrl = new RawMaterialController();
        private readonly CurrencyController _currencyCtrl = new CurrencyController();
        private readonly string _module;
        private DataGridView _dictGrid;
        private DataGridView _productGrid;
        private DataGridView _currencyGrid;

        public SystemAdminPanel(string module = "System Admin")
        {
            _module = module;
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            BuildUI();
        }

        private void BuildUI()
        {
            this.SuspendLayout();
            try
            {
                var topBar = new Panel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(10, 8, 10, 0) };
                var btnImport = UITheme.CreatePrimaryButton("Import CSV...");
                btnImport.Width = 130;
                btnImport.Click += (s, e) => MasterDataImportDialog.ShowImportDialog(this);
                topBar.Controls.Add(btnImport);

                TableLayoutPanel mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(10) };
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

                Panel dictCard = CreateSafeCard("System Dictionary");
                Panel prodCard = CreateSafeCard("Product Catalog");
                Panel sqCard = CreateSafeCard("Supplier Raw Material Quotes");
                Panel currencyCard = CreateSafeCard("Currency & Exchange Rates");

                _dictGrid = GridHelper.CreateStyledGrid();
                _dictGrid.Dock = DockStyle.Fill;
                try { _dictGrid.DataSource = _dictCtrl.GetAllDictionaries(); GridHelper.StyleGrid(_dictGrid); } catch { }
                var dictWrap = new Panel { Dock = DockStyle.Fill };
                dictWrap.Controls.Add(_dictGrid);
                dictWrap.Controls.Add(FilterBlockHelper.CreateFilterBlock(_dictGrid, "Dictionary Filters"));
                AddCardContent(dictCard, "System Dictionary", dictWrap);

                _productGrid = GridHelper.CreateStyledGrid();
                _productGrid.Dock = DockStyle.Fill;
                try
                {
                    GridHelper.BindStatusWithStockAlert(
                        _productGrid,
                        _productCtrl.GetAllProductsWithStock(StockAlertHelper.DefaultProductMinStock),
                        "Status",
                        DictionaryService.Categories.Product,
                        "Available Stock",
                        "Min Stock Level");
                    GridHelper.ConfigureProductCatalogueGrid(_productGrid);
                }
                catch { }
                var prodWrap = new Panel { Dock = DockStyle.Fill };
                prodWrap.Controls.Add(_productGrid);
                prodWrap.Controls.Add(FilterBlockHelper.CreateFilterBlock(_productGrid, "Product Filters", DictionaryService.Categories.Product));
                AddCardContent(prodCard, "Product Catalog", prodWrap);

                _currencyGrid = GridHelper.CreateStyledGrid();
                _currencyGrid.Dock = DockStyle.Fill;
                _currencyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                _currencyGrid.ReadOnly = true;
                LoadCurrencies();

                var currencyToolbar = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 4, 0, 4) };
                var btnEditRate = UITheme.CreatePrimaryButton("Edit Rate");
                btnEditRate.Click += (s, e) => EditSelectedCurrencyRate();
                var btnRefreshCurrency = UITheme.CreateSecondaryButton("Refresh");
                btnRefreshCurrency.Location = new Point(btnEditRate.Right + 8, 4);
                btnRefreshCurrency.Click += (s, e) => LoadCurrencies();
                currencyToolbar.Controls.Add(btnEditRate);
                currencyToolbar.Controls.Add(btnRefreshCurrency);

                var currencyWrap = new Panel { Dock = DockStyle.Fill };
                currencyWrap.Controls.Add(_currencyGrid);
                currencyWrap.Controls.Add(currencyToolbar);
                AddCardContent(currencyCard, "Currency & Exchange Rates (HKD base)", currencyWrap);

                mainLayout.Controls.Add(dictCard, 0, 0);
                mainLayout.Controls.Add(prodCard, 1, 0);
                mainLayout.Controls.Add(sqCard, 0, 1);
                mainLayout.Controls.Add(currencyCard, 1, 1);

                this.Controls.Add(mainLayout);
                this.Controls.Add(topBar);
            }
            catch (Exception ex)
            {
                this.Controls.Add(new Label { Text = "Build Error: " + ex.Message, ForeColor = Color.Red, Dock = DockStyle.Fill });
            }
            this.ResumeLayout(true);
        }

        private void LoadCurrencies()
        {
            try
            {
                var dt = _currencyCtrl.GetAllForAdmin();
                _currencyGrid.DataSource = dt;
                GridHelper.StyleGrid(_currencyGrid);
            }
            catch (Exception ex)
            {
                UITheme.ShowError("Failed to load currencies: " + ex.Message);
            }
        }

        private void EditSelectedCurrencyRate()
        {
            if (_currencyGrid?.CurrentRow == null)
            {
                UITheme.ShowWarning("Please select a currency row first.");
                return;
            }

            long currencyId = Convert.ToInt64(_currencyGrid.CurrentRow.Cells["Currency ID"].Value);
            var currency = _currencyCtrl.GetById(currencyId);
            if (currency == null) return;
            if (currency.IsBaseCurrency)
            {
                UITheme.ShowWarning("HKD is the base currency; its rate is always 1.00.");
                return;
            }

            using (var dlg = new Form())
            {
                dlg.Text = $"Edit Rate — {currency.CurrencyCode}";
                dlg.Size = new Size(360, 180);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 2 };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                var txtRate = new TextBox { Text = currency.RateToBase.ToString("0.####"), Width = 160 };
                UITheme.AddFormField(layout, 0, "Rate to HKD", txtRate);
                UITheme.AddFormField(layout, 1, "Note", new Label
                {
                    Text = "New documents lock this rate at save time.",
                    AutoSize = true,
                    ForeColor = UITheme.TextGray
                });

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (!decimal.TryParse(txtRate.Text.Trim(), out decimal newRate) || newRate <= 0)
                    {
                        UITheme.ShowWarning("Enter a valid rate greater than zero.");
                        return;
                    }
                    if (!_currencyCtrl.UpdateRate(currencyId, newRate))
                    {
                        UITheme.ShowWarning("Rate update failed.");
                        return;
                    }
                    UITheme.ShowSuccess($"{currency.CurrencyCode} rate updated.");
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    LoadCurrencies();
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private Panel CreateSafeCard(string title)
        {
            Panel p = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
            p.Paint += (s, e) => {
                using (var pen = new System.Drawing.Pen(Color.FromArgb(200, 200, 200)))
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            return p;
        }

        private static void AddCardContent(Panel card, string title, Control content)
        {
            content.Dock = DockStyle.Fill;
            var lbl = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Height = 25
            };
            card.Controls.Add(content);
            card.Controls.Add(lbl);
        }
    }
}
