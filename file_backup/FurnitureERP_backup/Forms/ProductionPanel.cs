using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Sales_user.Controllers;
using Sales_user.Models;
using FurnitureERP.Helpers;

namespace FurnitureERP.Forms
{
    public class ProductionPanel : UserControl
    {
        private readonly ProductionOrderController _productionCtrl = new ProductionOrderController();
        private readonly RawMaterialController _rawMaterialCtrl = new RawMaterialController();
        private readonly RawMaterialRequestNoteController _rmrnCtrl = new RawMaterialRequestNoteController();
        private DataGridView _grid;
        private TextBox _searchBox;
        private ComboBox _statusFilter;

        private TabControl _tabs;

        public ProductionPanel(string module = "Production")
        {
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            BuildUI();
            LoadData();
            // Raw Materials view handled inline
        }

        private void BuildUI()
        {
            // Toolbar
            Panel toolbar = new Panel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(0, 8, 0, 8) };

            Button btnNew = UITheme.CreatePrimaryButton("+ New Production Order");
            btnNew.Location = new Point(0, 9);
            btnNew.Click += (s, e) => ShowCreateDialog();

            Button btnQuickNew = UITheme.CreateSecondaryButton("⚡ Quick Entry");
            btnQuickNew.Location = new Point(btnNew.Width + 10, 9);
            btnQuickNew.Click += (s, e) => ShowBatchCreateDialog();

            Button btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefresh.Location = new Point(btnNew.Width + btnQuickNew.Width + 20, 9);
            btnRefresh.Click += (s, e) => LoadData();
            Button btnDetail = UITheme.CreateSecondaryButton("View Detail");
            btnDetail.Location = new Point(btnRefresh.Right + 10, 9);
            btnDetail.Click += (s, e) =>
            {
                if (_grid?.CurrentRow?.Cells[0].Value == null)
                {
                    UITheme.ShowWarning("Please select a production order first.");
                    return;
                }
                ShowProductionDetailTableDialog(Convert.ToInt64(_grid.CurrentRow.Cells[0].Value));
            };

            _searchBox = new TextBox { Width = 180, Height = 28, Location = new Point(btnDetail.Right + 10, 12) };
            _searchBox.TextChanged += (s, e) => LoadData(_searchBox.Text.Trim());

            _statusFilter = new ComboBox
            {
                Width = 140,
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(_searchBox.Right + 10, 12)
            };
            _statusFilter.Items.AddRange(new object[]
            {
                "All Status",
                "Pending",
                "In Progress",
                "Completed",
                "Cancelled"
            });
            _statusFilter.SelectedIndex = 0;
            _statusFilter.SelectedIndexChanged += (s, e) => LoadData(_searchBox.Text.Trim());

            // Add Product button
            Button btnAddProduct = UITheme.CreateSecondaryButton("🗂 Add Product");
            btnAddProduct.Location = new Point(_statusFilter.Right + 10, 9);
            btnAddProduct.Click += (s, e) => ShowAddProductDialog();

            // Raw Material Request button
            Button btnRMRN = UITheme.CreateSecondaryButton("📋 RM Requests");
            btnRMRN.Location = new Point(btnAddProduct.Right + 10, 9);
            btnRMRN.Click += (s, e) => ShowRawMaterialRequestsPanel();

            toolbar.Controls.AddRange(new Control[] { btnNew, btnQuickNew, btnRefresh, btnDetail, _searchBox, _statusFilter, btnAddProduct, btnRMRN });

            // Grid
            _grid = GridHelper.CreateStyledGrid();
            _grid.CellDoubleClick += Grid_CellDoubleClick;

            Controls.Add(_grid);
            Controls.Add(toolbar);
        }

        private void LoadData(string keyword = null)
        {
            try
            {
                DataTable dt;
                if (string.IsNullOrEmpty(keyword))
                    dt = _productionCtrl.GetAllProductionOrders();
                else
                    dt = _productionCtrl.Search(new SearchFilterCriteria { Keyword = keyword });

                if (dt != null)
                {
                    // Add status labels
                    dt.Columns.Add("Status Label", typeof(string));
                    foreach (DataRow row in dt.Rows)
                    {
                        if (row["Status"] != DBNull.Value)
                        {
                            int status = Convert.ToInt32(row["Status"]);
                            string label;
                            if (status == 0) label = "Pending";
                            else if (status == 1) label = "In Progress";
                            else if (status == 2) label = "Completed";
                            else if (status == 3) label = "Cancelled";
                            else label = status.ToString();
                            row["Status Label"] = label;
                        }
                    }
                }
                if (dt != null && _statusFilter != null && _statusFilter.SelectedIndex > 0)
                {
                    int status = _statusFilter.SelectedIndex - 1;
                    dt.DefaultView.RowFilter = "[Status] = " + status;
                    dt = dt.DefaultView.ToTable();
                }

                _grid.DataSource = dt;
                GridHelper.StyleGrid(_grid);
            }
            catch (Exception ex)
            {
                // Silently handle no connection
            }
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _grid.Rows[e.RowIndex];
            if (row.Cells[0].Value == null) return;
            long id = Convert.ToInt64(row.Cells[0].Value);
            ShowDetailDialog(id);
        }

        private void ShowCreateDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "New Production Order";
                dlg.Size = new Size(480, 360);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtSalesOrderId = new TextBox();
                var txtStaffId = new TextBox();
                var dtpFinishDate = new DateTimePicker { Format = DateTimePickerFormat.Short };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new[] { "0 - Pending", "1 - In Progress", "2 - Completed", "3 - Cancelled" });
                cmbStatus.SelectedIndex = 0;
                var txtRemark = new TextBox { Multiline = true, Height = 60 };

                UITheme.AddFormField(layout, 0, "Sales Order ID *", txtSalesOrderId);
                UITheme.AddFormField(layout, 1, "Staff ID *", txtStaffId);
                UITheme.AddFormField(layout, 2, "Est. Finish Date *", dtpFinishDate);
                UITheme.AddFormField(layout, 3, "Status", cmbStatus);
                UITheme.AddFormField(layout, 4, "Remark", txtRemark);

                var btnSave = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtSalesOrderId.Text) || string.IsNullOrWhiteSpace(txtStaffId.Text))
                    { MessageBox.Show("Sales Order ID and Staff ID are required.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    try
                    {
                        var po = new ProductionOrder
                        {
                            ProductionOrderCode = "PO-TEMP",
                            SalesOrderID = long.Parse(txtSalesOrderId.Text.Trim()),
                            StaffID = long.Parse(txtStaffId.Text.Trim()),
                            EstFinishDate = dtpFinishDate.Value,
                            Status = cmbStatus.SelectedIndex,
                            Remark = txtRemark.Text.Trim()
                        };
                        long id = _productionCtrl.Insert(po);
                        _productionCtrl.UpdateCodeAfterInsert(id);
                        MessageBox.Show($"Production Order PO-{id} created.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                        LoadData();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave); btnPanel.Controls.Add(btnCancel);
                dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private void ShowBatchCreateDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Quick Create Production Orders";
                dlg.Size = new Size(900, 500);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var info = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 40,
                    Padding = new Padding(10, 10, 0, 0),
                    Text = "Enter multiple orders below. Required: Sales Order ID, Staff ID, Est. Finish Date.",
                    ForeColor = UITheme.TextDark
                };

                var grid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AllowUserToAddRows = true,
                    AllowUserToDeleteRows = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                };
                grid.Columns.Add("SalesOrderID", "Sales Order ID *");
                grid.Columns.Add("StaffID", "Staff ID *");
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "EstFinishDate", HeaderText = "Est. Finish Date * (yyyy-MM-dd)" });
                var statusCol = new DataGridViewComboBoxColumn
                {
                    Name = "Status",
                    HeaderText = "Status",
                    DataSource = new[] { "Pending", "In Progress", "Completed", "Cancelled" }
                };
                grid.Columns.Add(statusCol);
                grid.Columns.Add("Remark", "Remark");

                var btnSave = UITheme.CreatePrimaryButton("Save All");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    int successCount = 0;
                    for (int i = 0; i < grid.Rows.Count; i++)
                    {
                        var row = grid.Rows[i];
                        if (row.IsNewRow) continue;

                        string soText = row.Cells["SalesOrderID"]?.Value?.ToString();
                        string staffText = row.Cells["StaffID"]?.Value?.ToString();
                        string dateText = row.Cells["EstFinishDate"]?.Value?.ToString();
                        string statusText = row.Cells["Status"]?.Value?.ToString();
                        string remarkText = row.Cells["Remark"]?.Value?.ToString();

                        if (string.IsNullOrWhiteSpace(soText) && string.IsNullOrWhiteSpace(staffText) && string.IsNullOrWhiteSpace(dateText))
                        {
                            continue;
                        }

                        if (!long.TryParse(soText, out long soId) || !long.TryParse(staffText, out long staffId) || !DateTime.TryParse(dateText, out DateTime estDate))
                        {
                            MessageBox.Show($"Row {i + 1} has invalid required fields.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        int status = 0;
                        if (statusText == "In Progress") status = 1;
                        else if (statusText == "Completed") status = 2;
                        else if (statusText == "Cancelled") status = 3;

                        try
                        {
                            var po = new ProductionOrder
                            {
                                ProductionOrderCode = "PO-TEMP",
                                SalesOrderID = soId,
                                StaffID = staffId,
                                EstFinishDate = estDate,
                                Status = status,
                                Remark = string.IsNullOrWhiteSpace(remarkText) ? null : remarkText.Trim()
                            };
                            long id = _productionCtrl.Insert(po);
                            _productionCtrl.UpdateCodeAfterInsert(id);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Row {i + 1} failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    MessageBox.Show($"{successCount} production orders created.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    LoadData(_searchBox.Text.Trim());
                };

                var btnPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(8)
                };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnCancel);

                dlg.Controls.Add(grid);
                dlg.Controls.Add(info);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private void ShowDetailDialog(long id)
        {
            var po = _productionCtrl.GetById(id);
            if (po == null) { MessageBox.Show("Record not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

            using (var dlg = new Form())
            {
                dlg.Text = $"Production Order - {po.ProductionOrderCode}";
                dlg.Size = new Size(560, 450);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var dtpFinishDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = po.EstFinishDate };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                cmbStatus.Items.AddRange(new[] { "0 - Pending", "1 - In Progress", "2 - Completed", "3 - Cancelled" });
                cmbStatus.SelectedIndex = Math.Max(0, Math.Min(po.Status, 3));
                var txtRemark = new TextBox { Text = po.Remark ?? "", Multiline = true, Height = 60 };

                UITheme.AddFormField(layout, 0, "Order Code", new Label { Text = po.ProductionOrderCode, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
                UITheme.AddFormField(layout, 1, "Est. Finish Date", dtpFinishDate);
                UITheme.AddFormField(layout, 2, "Status", cmbStatus);
                UITheme.AddFormField(layout, 3, "Remark", txtRemark);

                var btnUpdate = UITheme.CreatePrimaryButton("Update");
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                btnUpdate.Click += (s, e) =>
                {
                    try
                    {
                        po.EstFinishDate = dtpFinishDate.Value;
                        po.Status = cmbStatus.SelectedIndex;
                        po.Remark = txtRemark.Text.Trim();
                        _productionCtrl.Update(po);
                        MessageBox.Show("Updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.Close();
                        LoadData();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnUpdate); btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(layout); dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(this);
            }
        }

        private void ShowProductionDetailTableDialog(long id)
        {
            var po = _productionCtrl.GetById(id);
            if (po == null) { UITheme.ShowWarning("Record not found."); return; }

            var headerDt = new DataTable();
            headerDt.Columns.Add("Field");
            headerDt.Columns.Add("Value");
            headerDt.Rows.Add("Production Order ID", po.ProductionOrderID);
            headerDt.Rows.Add("Production Order Code", po.ProductionOrderCode ?? "");
            headerDt.Rows.Add("Sales Order ID", po.SalesOrderID);
            headerDt.Rows.Add("Staff ID", po.StaffID);
            headerDt.Rows.Add("Est. Finish Date", po.EstFinishDate.ToString("yyyy-MM-dd"));
            headerDt.Rows.Add("Status", po.Status);
            headerDt.Rows.Add("Remark", po.Remark ?? "");

            DataTable lines = null;
            try { lines = _productionCtrl.GetProductLines(id); } catch { }

            DetailViewHelper.ShowDetail(this, $"Production Detail - {po.ProductionOrderCode}",
                headerDt, lines, $"Production_{po.ProductionOrderCode}");
        }

        private void ShowAddProductDialog()
        {
            var _productCtrl = new Sales_user.Controllers.ProductController();
            byte[] selectedImageBytes = null;

            using (var dlg = new Form())
            {
                dlg.Text = "New Product";
                dlg.Size = new Size(560, 620);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 10,
                    Padding = new Padding(16)
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtCode     = new TextBox { Dock = DockStyle.Fill };
                var txtCategory = new TextBox { Dock = DockStyle.Fill };
                var txtStyle    = new TextBox { Dock = DockStyle.Fill };
                var txtSize     = new TextBox { Dock = DockStyle.Fill };
                var txtColor    = new TextBox { Dock = DockStyle.Fill };
                var txtUnit     = new TextBox { Dock = DockStyle.Fill };
                var txtPrice    = new TextBox { Dock = DockStyle.Fill, Text = "0" };
                var txtStatus   = new TextBox { Dock = DockStyle.Fill, Text = "1" };

                UITheme.AddFormField(layout, 0, "Product Code *", txtCode);
                UITheme.AddFormField(layout, 1, "Category",       txtCategory);
                UITheme.AddFormField(layout, 2, "Style Number",   txtStyle);
                UITheme.AddFormField(layout, 3, "Size",           txtSize);
                UITheme.AddFormField(layout, 4, "Color",          txtColor);
                UITheme.AddFormField(layout, 5, "Unit",           txtUnit);
                UITheme.AddFormField(layout, 6, "Base Price",     txtPrice);
                UITheme.AddFormField(layout, 7, "Status",         txtStatus);

                // Image row
                var picBox = new PictureBox
                {
                    Width = 120, Height = 90,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.WhiteSmoke
                };
                var btnUpload = UITheme.CreateSecondaryButton("Upload Image");
                btnUpload.Click += (s, e) =>
                {
                    using (var ofd = new OpenFileDialog())
                    {
                        ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                        ofd.Title = "Select Product Image";
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            selectedImageBytes = System.IO.File.ReadAllBytes(ofd.FileName);
                            picBox.Image = System.Drawing.Image.FromFile(ofd.FileName);
                        }
                    }
                };
                var imgPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
                imgPanel.Controls.Add(picBox);
                imgPanel.Controls.Add(btnUpload);
                UITheme.AddFormField(layout, 8, "Product Image", imgPanel);

                var btnSave   = UITheme.CreatePrimaryButton("Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtCode.Text))
                    {
                        MessageBox.Show("Product Code is required.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    try
                    {
                        _productCtrl.Insert(new Sales_user.Models.Product
                        {
                            ProductCode          = txtCode.Text.Trim(),
                            Category             = txtCategory.Text.Trim(),
                            StyleNumber          = txtStyle.Text.Trim(),
                            Size                 = txtSize.Text.Trim(),
                            Color                = txtColor.Text.Trim(),
                            Unit                 = txtUnit.Text.Trim(),
                            BasePriceByCurrency  = string.IsNullOrEmpty(txtPrice.Text) ? 0 : decimal.Parse(txtPrice.Text),
                            Status               = string.IsNullOrEmpty(txtStatus.Text) ? 1 : int.Parse(txtStatus.Text),
                            ProductImage         = selectedImageBytes
                        });
                        MessageBox.Show("Product added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
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

        private void ShowRawMaterialRequestsPanel()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Raw Material Request Notes";
                dlg.Size = new Size(800, 500);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var grid = GridHelper.CreateStyledGrid();
                grid.Dock = DockStyle.Fill;
                try { grid.DataSource = _rmrnCtrl.GetAllRequestNotes(); GridHelper.StyleGrid(grid); } catch { }

                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Dock = DockStyle.Bottom;
                btnClose.Click += (s, e) => dlg.Close();

                dlg.Controls.Add(grid);
                dlg.Controls.Add(btnClose);
                dlg.ShowDialog(this);
            }
        }
    }
}
