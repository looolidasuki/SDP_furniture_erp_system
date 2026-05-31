using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Sales_user.Controllers;
using FurnitureERP.Helpers;

namespace FurnitureERP.Forms
{
    public class SystemAdminPanel : Panel
    {
        private readonly ProductController _productCtrl = new ProductController();
        private readonly SystemDictionaryController _dictCtrl = new SystemDictionaryController();
        private readonly RawMaterialController _rawMaterialCtrl = new RawMaterialController();
        private readonly string _module;
        private DataGridView _dictGrid;
        private DataGridView _productGrid;

        public SystemAdminPanel(string module = "System Admin")
        {
            _module = module ?? "System Admin";
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            BuildUI();
        }

        private void BuildUI()
        {
            if (_module == "Staff")
            {
                BuildStaffFocusedUI();
                return;
            }

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 68));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 32));

            // System Dictionary
            Panel dictCard = UITheme.CreateCard("System Dictionary");
            _dictGrid = GridHelper.CreateStyledGrid();
            _dictGrid.Dock = DockStyle.Fill;
            _dictGrid.CellDoubleClick += DictGrid_CellDoubleClick;
            try { _dictGrid.DataSource = _dictCtrl.GetAllDictionaries(); GridHelper.StyleGrid(_dictGrid); } catch { }
            var dictToolbar = BuildSearchFilterToolbar(_dictGrid, () =>
            {
                if (_dictGrid?.CurrentRow == null) { UITheme.ShowWarning("Please select a dictionary record first."); return; }
                ShowGridRowDetail("Dictionary Detail", _dictGrid.CurrentRow);
            });
            Button addDictBtn = UITheme.CreatePrimaryButton("+ Add Entry");
            addDictBtn.Dock = DockStyle.Bottom;
            addDictBtn.Height = 32;
            addDictBtn.Click += AddDictEntry_Click;
            dictCard.Controls.Add(_dictGrid);
            dictCard.Controls.Add(dictToolbar);
            dictCard.Controls.Add(addDictBtn);
            layout.Controls.Add(dictCard, 0, 0);

            // Product Catalog
            Panel productCard = UITheme.CreateCard("Product Catalog");
            _productGrid = GridHelper.CreateStyledGrid();
            _productGrid.Dock = DockStyle.Fill;
            _productGrid.CellDoubleClick += ProductGrid_CellDoubleClick;
            try { _productGrid.DataSource = _productCtrl.GetAllProducts(); GridHelper.StyleGrid(_productGrid); } catch { }
            var productToolbar = BuildSearchFilterToolbar(_productGrid, () =>
            {
                if (_productGrid?.CurrentRow == null) { UITheme.ShowWarning("Please select a product record first."); return; }
                ShowGridRowDetail("Product Detail", _productGrid.CurrentRow);
            });
            Button addProductBtn = UITheme.CreatePrimaryButton("+ Add Product");
            addProductBtn.Dock = DockStyle.Bottom;
            addProductBtn.Height = 32;
            addProductBtn.Click += AddProduct_Click;
            productCard.Controls.Add(_productGrid);
            productCard.Controls.Add(productToolbar);
            productCard.Controls.Add(addProductBtn);
            layout.Controls.Add(productCard, 1, 0);

            // Supplier Raw Material Quotes
            Panel sqCard = UITheme.CreateCard("Supplier Raw Material Quotes");
            DataGridView sqGrid = GridHelper.CreateStyledGrid();
            sqGrid.Dock = DockStyle.Fill;
            try { sqGrid.DataSource = _rawMaterialCtrl.GetAllSupplierQuotes(); GridHelper.StyleGrid(sqGrid); } catch { }
            sqCard.Controls.Add(sqGrid);
            layout.Controls.Add(sqCard, 0, 1);

            // System Information
            Panel infoCard = UITheme.CreateCard("System Information");
            Label infoLabel = new Label
            {
                Text = "Premium Living Furniture ERP\n\nVersion: 2.0\nFramework: .NET 4.8\nDatabase: MySQL\n\n" +
                       "Modules:\n• Sales & Quotations\n• Production Management\n• Warehouse & Inventory\n" +
                       "• Procurement & GRN\n• Finance & Invoicing\n• System Administration",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = UITheme.TextDark,
                Padding = new Padding(12)
            };
            infoCard.Controls.Add(infoLabel);
            layout.Controls.Add(infoCard, 1, 1);

            Controls.Add(layout);
        }

        private void BuildStaffFocusedUI()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Dictionary card (left)
            Panel dictCard = UITheme.CreateCard("System Dictionary");
            _dictGrid = GridHelper.CreateStyledGrid();
            _dictGrid.Dock = DockStyle.Fill;
            _dictGrid.CellDoubleClick += DictGrid_CellDoubleClick;
            try { _dictGrid.DataSource = _dictCtrl.GetAllDictionaries(); GridHelper.StyleGrid(_dictGrid); } catch { }
            var dictToolbar = BuildSearchFilterToolbar(_dictGrid, () =>
            {
                if (_dictGrid?.CurrentRow == null) { UITheme.ShowWarning("Please select a dictionary record first."); return; }
                ShowGridRowDetail("Dictionary Detail", _dictGrid.CurrentRow);
            });
            Button addDictBtn = UITheme.CreatePrimaryButton("+ Add Entry");
            addDictBtn.Dock = DockStyle.Bottom;
            addDictBtn.Height = 34;
            addDictBtn.Click += AddDictEntry_Click;
            dictCard.Controls.Add(_dictGrid);
            dictCard.Controls.Add(dictToolbar);
            dictCard.Controls.Add(addDictBtn);
            layout.Controls.Add(dictCard, 0, 0);

            // Product card (right)
            Panel productCard = UITheme.CreateCard("Product Catalog");
            _productGrid = GridHelper.CreateStyledGrid();
            _productGrid.Dock = DockStyle.Fill;
            _productGrid.CellDoubleClick += ProductGrid_CellDoubleClick;
            try { _productGrid.DataSource = _productCtrl.GetAllProducts(); GridHelper.StyleGrid(_productGrid); } catch { }
            var productToolbar = BuildSearchFilterToolbar(_productGrid, () =>
            {
                if (_productGrid?.CurrentRow == null) { UITheme.ShowWarning("Please select a product record first."); return; }
                ShowGridRowDetail("Product Detail", _productGrid.CurrentRow);
            });
            Button addProductBtn = UITheme.CreatePrimaryButton("+ Add Product");
            addProductBtn.Dock = DockStyle.Bottom;
            addProductBtn.Height = 34;
            addProductBtn.Click += AddProduct_Click;
            productCard.Controls.Add(_productGrid);
            productCard.Controls.Add(productToolbar);
            productCard.Controls.Add(addProductBtn);
            layout.Controls.Add(productCard, 1, 0);

            Controls.Add(layout);
        }

        private Panel BuildSearchFilterToolbar(DataGridView grid, Action onViewDetail)
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 38 };
            var txtSearch = new TextBox { Width = 180, Height = 26, Location = new Point(6, 6) };
            var cmbStatus = new ComboBox { Width = 120, Height = 26, DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(txtSearch.Right + 8, 6) };
            cmbStatus.Items.AddRange(new object[] { "All Status", "0", "1", "2", "3", "4" });
            cmbStatus.SelectedIndex = 0;
            var btnDetail = UITheme.CreateSecondaryButton("View Detail");
            btnDetail.Width = 100;
            btnDetail.Height = 26;
            btnDetail.Location = new Point(cmbStatus.Right + 8, 5);
            btnDetail.Click += (s, e) => onViewDetail?.Invoke();

            Action apply = () =>
            {
                if (!(grid.DataSource is DataTable dt)) return;
                string keyword = txtSearch.Text.Trim().Replace("'", "''");
                var conditions = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var cols = dt.Columns.Cast<DataColumn>().Where(c => c.DataType == typeof(string)).Select(c => $"[{c.ColumnName}] LIKE '%{keyword}%'");
                    string filter = string.Join(" OR ", cols);
                    if (!string.IsNullOrWhiteSpace(filter)) conditions.Add("(" + filter + ")");
                }
                if (cmbStatus.SelectedIndex > 0 && dt.Columns.Contains("Status"))
                {
                    conditions.Add("[Status] = " + (cmbStatus.SelectedIndex - 1));
                }
                dt.DefaultView.RowFilter = string.Join(" AND ", conditions);
            };

            txtSearch.TextChanged += (s, e) => apply();
            cmbStatus.SelectedIndexChanged += (s, e) => apply();
            panel.Controls.Add(txtSearch);
            panel.Controls.Add(cmbStatus);
            panel.Controls.Add(btnDetail);
            return panel;
        }

        private void ShowGridRowDetail(string title, DataGridViewRow row)
        {
            DetailViewHelper.ShowKeyValueDetail(this, title, row);
        }

        private void DictGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _dictGrid.Rows[e.RowIndex];
            string category = row.Cells["Category"]?.Value?.ToString();
            int codeValue = row.Cells["CodeValue"]?.Value == null ? 0 : Convert.ToInt32(row.Cells["CodeValue"].Value);
            string display = row.Cells["DisplayNameEnglish"]?.Value?.ToString();
            int sortOrder = row.Cells["SortOrder"]?.Value == null ? 0 : Convert.ToInt32(row.Cells["SortOrder"].Value);

            using (var dlg = UITheme.BuildInputDialog("Edit Dictionary Entry",
                new[] { "Category *", "Code Value *", "Display Name *", "Sort Order" }))
            {
                var layout = dlg.Tag as TableLayoutPanel;
                var textBoxes = layout?.Controls.OfType<TextBox>().ToArray();
                if (textBoxes != null && textBoxes.Length >= 4)
                {
                    textBoxes[0].Text = category ?? "";
                    textBoxes[1].Text = codeValue.ToString();
                    textBoxes[2].Text = display ?? "";
                    textBoxes[3].Text = sortOrder.ToString();
                }

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var vals = UITheme.GetDialogValues(dlg);
                    var entity = new Sales_user.Models.SystemDictionary
                    {
                        Category = vals[0],
                        CodeValue = string.IsNullOrWhiteSpace(vals[1]) ? 0 : int.Parse(vals[1]),
                        DisplayNameEnglish = vals[2],
                        SortOrder = string.IsNullOrWhiteSpace(vals[3]) ? 0 : int.Parse(vals[3])
                    };
                    _dictCtrl.Update(entity, category, codeValue);
                    _dictGrid.DataSource = _dictCtrl.GetAllDictionaries();
                    GridHelper.StyleGrid(_dictGrid);
                }
            }
        }

        private void ProductGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var idObj = _productGrid.Rows[e.RowIndex].Cells[0].Value;
            if (idObj == null) return;
            var product = _productCtrl.GetById(Convert.ToInt64(idObj));
            if (product == null) return;

            using (var dlg = new Form())
            {
                dlg.Text = $"Edit Product — {product.ProductCode}";
                dlg.Size = new Size(520, 560);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 10, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtCode = new TextBox { Text = product.ProductCode ?? "" };
                var txtCategory = new TextBox { Text = product.Category ?? "" };
                var txtStyle = new TextBox { Text = product.StyleNumber ?? "" };
                var txtSize = new TextBox { Text = product.Size ?? "" };
                var txtColor = new TextBox { Text = product.Color ?? "" };
                var txtUnit = new TextBox { Text = product.Unit ?? "" };
                var txtPrice = new TextBox { Text = product.BasePriceByCurrency.ToString() };
                var txtStatus = new TextBox { Text = product.Status.ToString() };

                UITheme.AddFormField(layout, 0, "Product Code *", txtCode);
                UITheme.AddFormField(layout, 1, "Category", txtCategory);
                UITheme.AddFormField(layout, 2, "Style Number", txtStyle);
                UITheme.AddFormField(layout, 3, "Size", txtSize);
                UITheme.AddFormField(layout, 4, "Color", txtColor);
                UITheme.AddFormField(layout, 5, "Unit", txtUnit);
                UITheme.AddFormField(layout, 6, "Base Price", txtPrice);
                UITheme.AddFormField(layout, 7, "Status", txtStatus);

                // Image upload row
                byte[] imageBytes = product.ProductImage;
                var picBox = new PictureBox
                {
                    Width = 120, Height = 90,
                    BorderStyle = BorderStyle.FixedSingle,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.WhiteSmoke
                };
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    try { using (var ms = new System.IO.MemoryStream(imageBytes)) picBox.Image = System.Drawing.Image.FromStream(ms); } catch { }
                }
                var btnUpload = UITheme.CreateSecondaryButton("📁 Upload Image");
                btnUpload.Click += (s, e2) =>
                {
                    using (var ofd = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif" })
                    {
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            imageBytes = System.IO.File.ReadAllBytes(ofd.FileName);
                            using (var ms = new System.IO.MemoryStream(imageBytes))
                                picBox.Image = System.Drawing.Image.FromStream(ms);
                        }
                    }
                };
                var imgPanel = new Panel { Height = 100 };
                imgPanel.Controls.Add(picBox);
                btnUpload.Location = new Point(130, 30);
                imgPanel.Controls.Add(btnUpload);
                UITheme.AddFormField(layout, 8, "Product Image", imgPanel);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e2) => dlg.Close();
                btnSave.Click += (s, e2) =>
                {
                    if (string.IsNullOrWhiteSpace(txtCode.Text))
                    { UITheme.ShowWarning("Product Code is required."); return; }
                    try
                    {
                        product.ProductCode = txtCode.Text.Trim();
                        product.Category = txtCategory.Text.Trim();
                        product.StyleNumber = txtStyle.Text.Trim();
                        product.Size = txtSize.Text.Trim();
                        product.Color = txtColor.Text.Trim();
                        product.Unit = txtUnit.Text.Trim();
                        product.BasePriceByCurrency = string.IsNullOrWhiteSpace(txtPrice.Text) ? 0 : decimal.Parse(txtPrice.Text);
                        product.Status = string.IsNullOrWhiteSpace(txtStatus.Text) ? 1 : int.Parse(txtStatus.Text);
                        product.ProductImage = imageBytes;
                        _productCtrl.Update(product);
                        UITheme.ShowSuccess("Product updated successfully.");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        _productGrid.DataSource = _productCtrl.GetAllProducts();
                        GridHelper.StyleGrid(_productGrid);
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private void AddDictEntry_Click(object sender, EventArgs e)
        {
            using (var dlg = UITheme.BuildInputDialog("New Dictionary Entry",
                new[] { "Category *", "Code Value *", "Display Name *", "Sort Order" }))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var vals = UITheme.GetDialogValues(dlg);
                    try
                    {
                        _dictCtrl.Insert(new Sales_user.Models.SystemDictionary
                        {
                            Category = vals[0],
                            CodeValue = string.IsNullOrEmpty(vals[1]) ? 0 : int.Parse(vals[1]),
                            DisplayNameEnglish = vals[2],
                            SortOrder = string.IsNullOrEmpty(vals[3]) ? 0 : int.Parse(vals[3])
                        });
                        MessageBox.Show("Entry added.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
            }
        }

        private void AddProduct_Click(object sender, EventArgs e)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Product";
                dlg.Size = new Size(520, 560);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 10, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtCode = new TextBox();
                var txtCategory = new TextBox();
                var txtStyle = new TextBox();
                var txtSize = new TextBox();
                var txtColor = new TextBox();
                var txtUnit = new TextBox();
                var txtPrice = new TextBox { Text = "0" };
                var txtStatus = new TextBox { Text = "1" };

                UITheme.AddFormField(layout, 0, "Product Code *", txtCode);
                UITheme.AddFormField(layout, 1, "Category", txtCategory);
                UITheme.AddFormField(layout, 2, "Style Number", txtStyle);
                UITheme.AddFormField(layout, 3, "Size", txtSize);
                UITheme.AddFormField(layout, 4, "Color", txtColor);
                UITheme.AddFormField(layout, 5, "Unit", txtUnit);
                UITheme.AddFormField(layout, 6, "Base Price", txtPrice);
                UITheme.AddFormField(layout, 7, "Status", txtStatus);

                byte[] imageBytes = null;
                var picBox = new PictureBox
                {
                    Width = 120, Height = 90,
                    BorderStyle = BorderStyle.FixedSingle,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.WhiteSmoke
                };
                var btnUpload = UITheme.CreateSecondaryButton("📁 Upload Image");
                btnUpload.Click += (s, e2) =>
                {
                    using (var ofd = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif" })
                    {
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            imageBytes = System.IO.File.ReadAllBytes(ofd.FileName);
                            using (var ms = new System.IO.MemoryStream(imageBytes))
                                picBox.Image = System.Drawing.Image.FromStream(ms);
                        }
                    }
                };
                var imgPanel = new Panel { Height = 100 };
                imgPanel.Controls.Add(picBox);
                btnUpload.Location = new Point(130, 30);
                imgPanel.Controls.Add(btnUpload);
                UITheme.AddFormField(layout, 8, "Product Image", imgPanel);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e2) => dlg.Close();
                btnSave.Click += (s, e2) =>
                {
                    if (string.IsNullOrWhiteSpace(txtCode.Text))
                    { UITheme.ShowWarning("Product Code is required."); return; }
                    try
                    {
                        _productCtrl.Insert(new Sales_user.Models.Product
                        {
                            ProductCode = txtCode.Text.Trim(),
                            Category = txtCategory.Text.Trim(),
                            StyleNumber = txtStyle.Text.Trim(),
                            Size = txtSize.Text.Trim(),
                            Color = txtColor.Text.Trim(),
                            Unit = txtUnit.Text.Trim(),
                            BasePriceByCurrency = string.IsNullOrWhiteSpace(txtPrice.Text) ? 0 : decimal.Parse(txtPrice.Text),
                            Status = string.IsNullOrWhiteSpace(txtStatus.Text) ? 1 : int.Parse(txtStatus.Text),
                            ProductImage = imageBytes
                        });
                        UITheme.ShowSuccess("Product added successfully.");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        _productGrid.DataSource = _productCtrl.GetAllProducts();
                        GridHelper.StyleGrid(_productGrid);
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }
    }
}
