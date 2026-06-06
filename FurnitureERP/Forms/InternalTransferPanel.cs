using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using Sales_user.Controllers;
using FurnitureERP.Helpers;

namespace FurnitureERP.Forms
{
    public class InternalTransferPanel : UserControl
    {
        private readonly WarehouseController _warehouseCtrl = new WarehouseController();
        private readonly InventoryWorkflowService _inventoryWorkflow = new InventoryWorkflowService();

        private ComboBox _cmbItemType;
        private ComboBox _cmbFromWarehouse;
        private ComboBox _cmbToWarehouse;
        private DataGridView _lineGrid;
        private DataTable _lineTable;
        private Button _btnTransfer;
        private Button _btnLoad;

        public InternalTransferPanel()
        {
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            BuildUI();
        }

        private void BuildUI()
        {
            var title = new Label
            {
                Text = "Internal Transfer",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = UITheme.Primary,
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(16, 10, 0, 0)
            };

            var info = new Label
            {
                Text = "Move stock between warehouses instantly. Select source warehouse and item type, enter transfer qty, then confirm. No transfer document is stored.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = UITheme.TextGray,
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(16, 0, 16, 0)
            };

            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 88, Padding = new Padding(16, 8, 16, 8) };
            var filterLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 8,
                RowCount = 1
            };
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

            _cmbItemType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            _cmbItemType.Items.AddRange(new object[] { "Raw Material", "Product" });
            _cmbItemType.SelectedIndex = 0;

            _cmbFromWarehouse = BuildWarehouseCombo();
            _cmbToWarehouse = BuildWarehouseCombo();

            _btnLoad = UITheme.CreateSecondaryButton("Load Items");
            _btnLoad.Dock = DockStyle.Fill;
            _btnLoad.Click += (s, e) => LoadTransferLines();

            _btnTransfer = UITheme.CreatePrimaryButton("Transfer");
            _btnTransfer.Dock = DockStyle.Fill;
            _btnTransfer.Click += (s, e) => ExecuteTransfer();
            PermissionGuard.ApplyCreateButton(_btnTransfer, PermissionModule.InternalTransferForm);
            if (!AppSession.CanCreate(PermissionModule.InternalTransferForm))
                _btnTransfer.Enabled = false;

            filterLayout.Controls.Add(MakeFilterLabel("Item Type"), 0, 0);
            filterLayout.Controls.Add(_cmbItemType, 1, 0);
            filterLayout.Controls.Add(MakeFilterLabel("From Warehouse *"), 2, 0);
            filterLayout.Controls.Add(_cmbFromWarehouse, 3, 0);
            filterLayout.Controls.Add(MakeFilterLabel("To Warehouse *"), 4, 0);
            filterLayout.Controls.Add(_cmbToWarehouse, 5, 0);
            filterLayout.Controls.Add(_btnLoad, 6, 0);
            filterLayout.Controls.Add(_btnTransfer, 7, 0);
            filterPanel.Controls.Add(filterLayout);

            _lineGrid = GridHelper.CreateStyledGrid();
            _lineGrid.ReadOnly = false;
            _lineGrid.Dock = DockStyle.Fill;

            var gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 16, 12) };
            gridPanel.Controls.Add(_lineGrid);
            gridPanel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Text = "Enter Transfer Qty for items to move. Only available stock (physical − reserved) can be transferred.",
                ForeColor = UITheme.TextGray,
                Font = new Font("Segoe UI", 8.5f),
                Padding = new Padding(0, 4, 0, 0)
            });

            Controls.Add(gridPanel);
            Controls.Add(filterPanel);
            Controls.Add(info);
            Controls.Add(title);
        }

        private static Label MakeFilterLabel(string text) =>
            new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = UITheme.TextDark,
                Font = new Font("Segoe UI", 9f)
            };

        private ComboBox BuildWarehouseCombo()
        {
            var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            var dt = _warehouseCtrl.GetAllWarehouses();
            if (dt != null && dt.Rows.Count > 0)
            {
                dt.Columns.Add("DisplayText", typeof(string));
                foreach (DataRow row in dt.Rows)
                    row["DisplayText"] = $"{row["Warehouse Name"]} — {row["Address"]}";
                cmb.DisplayMember = "DisplayText";
                cmb.ValueMember = "Warehouse ID";
                cmb.DataSource = dt;
            }
            return cmb;
        }

        private static long GetComboLongId(ComboBox cmb)
        {
            if (cmb?.SelectedValue == null || cmb.SelectedValue == DBNull.Value) return 0;
            if (cmb.SelectedValue is long l) return l;
            if (cmb.SelectedValue is int i) return i;
            long.TryParse(cmb.SelectedValue.ToString(), out long id);
            return id;
        }

        private bool IsRawMaterialTransfer() => _cmbItemType.SelectedIndex == 0;

        private void LoadTransferLines()
        {
            long fromWhId = GetComboLongId(_cmbFromWarehouse);
            if (fromWhId <= 0)
            {
                UITheme.ShowWarning("Please select a source warehouse.");
                return;
            }

            DataTable source = IsRawMaterialTransfer()
                ? _warehouseCtrl.GetTransferableRawMaterials(fromWhId)
                : _warehouseCtrl.GetTransferableProducts(fromWhId);

            _lineTable = source?.Copy() ?? new DataTable();
            if (!_lineTable.Columns.Contains("Transfer Qty"))
                _lineTable.Columns.Add("Transfer Qty", typeof(decimal));

            foreach (DataRow row in _lineTable.Rows)
                row["Transfer Qty"] = 0m;

            _lineGrid.DataSource = _lineTable;
            GridHelper.StyleGrid(_lineGrid);
            ConfigureLineGridColumns();
        }

        private void ConfigureLineGridColumns()
        {
            if (_lineGrid.Columns.Count == 0) return;

            foreach (DataGridViewColumn col in _lineGrid.Columns)
                col.ReadOnly = col.Name != "Transfer Qty";

            if (_lineGrid.Columns.Contains("Item ID"))
                _lineGrid.Columns["Item ID"].Visible = false;
        }

        private void ExecuteTransfer()
        {
            if (!PermissionGuard.Ensure(PermissionModule.InternalTransferForm, PermissionAction.Create, this))
                return;

            long fromWhId = GetComboLongId(_cmbFromWarehouse);
            long toWhId = GetComboLongId(_cmbToWarehouse);
            if (fromWhId <= 0 || toWhId <= 0)
            {
                UITheme.ShowWarning("Please select both warehouses.");
                return;
            }
            if (fromWhId == toWhId)
            {
                UITheme.ShowWarning("Source and destination warehouses must be different.");
                return;
            }
            if (_lineTable == null || _lineTable.Rows.Count == 0)
            {
                UITheme.ShowWarning("Load items first, then enter transfer quantities.");
                return;
            }

            var lines = new List<StockTransferLine>();
            foreach (DataRow row in _lineTable.Rows)
            {
                decimal qty = row["Transfer Qty"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Transfer Qty"]);
                if (qty <= 0) continue;

                decimal available = row["Available Qty"] == DBNull.Value ? 0 : Convert.ToDecimal(row["Available Qty"]);
                string itemCode = row["Item Code"]?.ToString() ?? "item";
                if (qty > available)
                {
                    UITheme.ShowWarning($"Transfer qty for {itemCode} exceeds available stock ({available:N2}).");
                    return;
                }

                lines.Add(new StockTransferLine
                {
                    ItemId = Convert.ToInt64(row["Item ID"]),
                    Quantity = qty
                });
            }

            if (lines.Count == 0)
            {
                UITheme.ShowWarning("Enter at least one transfer quantity greater than zero.");
                return;
            }

            string fromName = _cmbFromWarehouse.Text;
            string toName = _cmbToWarehouse.Text;
            string typeName = IsRawMaterialTransfer() ? "raw material" : "product";
            if (MessageBox.Show(
                    $"Transfer {lines.Count} {typeName} line(s) from\n{fromName}\nto\n{toName}?",
                    "Confirm Transfer",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var itemType = IsRawMaterialTransfer() ? StockTransferItemType.RawMaterial : StockTransferItemType.Product;
            var result = _inventoryWorkflow.TransferBetweenWarehouses(fromWhId, toWhId, itemType, lines);
            if (!result.Success)
            {
                UITheme.ShowError(result.Message);
                return;
            }

            UITheme.ShowSuccess(result.Message);
            LoadTransferLines();
        }
    }
}
