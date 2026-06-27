using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Sales_user.Controllers;
using Sales_user.Models;

namespace FurnitureERP.Helpers
{
    public static class RawMaterialDialogHelper
    {
        private static readonly RawMaterialController RawMaterialCtrl = new RawMaterialController();
        private static readonly SupplierController SupplierCtrl = new SupplierController();
        private static readonly CurrencyController CurrencyCtrl = new CurrencyController();

        public static bool ShowFormDialog(Control owner, RawMaterial existing = null, Action onSaved = null)
        {
            bool isEdit = existing != null;
            using (var dlg = new Form())
            {
                dlg.Text = isEdit ? $"Edit Raw Material — {existing.RawMaterialCode}" : "New Raw Material";
                dlg.Size = new Size(920, 680);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MinimumSize = new Size(800, 560);
                dlg.BackColor = UITheme.Background;

                var split = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,
                    SplitterDistance = 220
                };

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 6,
                    Padding = new Padding(16)
                };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtCode = new TextBox { Text = existing?.RawMaterialCode ?? "", Width = 340 };
                var txtCategory = new TextBox { Text = existing?.Category ?? "", Width = 340 };
                var txtSize = new TextBox { Text = existing?.Size ?? "", Width = 340 };
                var txtColor = new TextBox { Text = existing?.Color ?? "", Width = 340 };
                var txtMinStock = new TextBox { Text = existing?.MinimumStockLevel.ToString() ?? "0", Width = 340 };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
                cmbStatus.Items.AddRange(new object[] { "0 - Inactive", "1 - Active" });
                cmbStatus.SelectedIndex = existing != null ? Math.Max(0, Math.Min(existing.Status, 1)) : 1;

                UITheme.AddFormField(layout, 0, "Code *", txtCode);
                UITheme.AddFormField(layout, 1, "Category", txtCategory);
                UITheme.AddFormField(layout, 2, "Size", txtSize);
                UITheme.AddFormField(layout, 3, "Color", txtColor);
                UITheme.AddFormField(layout, 4, "Min Stock Level", txtMinStock);
                UITheme.AddFormField(layout, 5, "Status", cmbStatus);
                split.Panel1.Controls.Add(layout);

                var supplierTable = SupplierComboHelper.BuildPickerTable(SupplierCtrl);
                var currencyTable = CurrencyCtrl.GetAllForCombo();
                var quoteGrid = CreateSupplierQuoteGrid(supplierTable, currencyTable);
                SupplierComboHelper.WireGridSupplierComboColumn(quoteGrid, "Supplier", SupplierCtrl);
                if (isEdit)
                    LoadQuoteGrid(quoteGrid, RawMaterialCtrl.GetSupplierQuoteLines(existing.RawMaterialID));

                var lineHeader = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(8, 6, 8, 0) };
                var lblQuotes = new Label
                {
                    Text = "Supplier quotes (used for PO pricing and shortage purchase orders)",
                    AutoSize = true,
                    ForeColor = UITheme.TextGray,
                    Location = new Point(8, 10)
                };
                var btnAddLine = UITheme.CreateSecondaryButton("+ Add Quote Line");
                btnAddLine.Location = new Point(520, 4);
                var btnRemoveLine = UITheme.CreateSecondaryButton("Remove Line");
                btnRemoveLine.Location = new Point(btnAddLine.Right + 8, 4);
                btnAddLine.Click += (s, e) => AddDefaultQuoteRow(quoteGrid);
                btnRemoveLine.Click += (s, e) => RemoveSelectedQuoteRow(quoteGrid);
                lineHeader.Controls.Add(lblQuotes);
                lineHeader.Controls.Add(btnAddLine);
                lineHeader.Controls.Add(btnRemoveLine);

                split.Panel2.Controls.Add(quoteGrid);
                split.Panel2.Controls.Add(lineHeader);

                var btnSave = UITheme.CreatePrimaryButton(isEdit ? "Update" : "Save");
                var btnCancel = UITheme.CreateSecondaryButton("Cancel");
                btnCancel.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtCode.Text))
                    {
                        UITheme.ShowWarning("Code is required.");
                        return;
                    }
                    if (!int.TryParse(txtMinStock.Text.Trim(), out int minStock) || minStock < 0)
                    {
                        UITheme.ShowWarning("Min Stock Level must be a non-negative number.");
                        return;
                    }
                    if (!TryReadQuoteLinesFromGrid(quoteGrid, out var quoteLines, out string quoteError))
                    {
                        UITheme.ShowWarning(quoteError);
                        return;
                    }

                    try
                    {
                        var rm = new RawMaterial
                        {
                            RawMaterialCode = txtCode.Text.Trim(),
                            Category = txtCategory.Text.Trim(),
                            Size = txtSize.Text.Trim(),
                            Color = txtColor.Text.Trim(),
                            MinimumStockLevel = minStock,
                            Status = cmbStatus.SelectedIndex
                        };

                        if (isEdit)
                        {
                            rm.RawMaterialID = existing.RawMaterialID;
                            RawMaterialCtrl.UpdateWithSupplierQuotes(rm, quoteLines);
                            UITheme.ShowSuccess("Raw material updated.");
                        }
                        else
                        {
                            long id = RawMaterialCtrl.InsertWithSupplierQuotes(rm, quoteLines);
                            UITheme.ShowSuccess($"Raw material created (RM-{id}).");
                        }

                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex)
                    {
                        UITheme.ShowError(ex.Message);
                    }
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
                dlg.Controls.Add(split);
                dlg.Controls.Add(btnPanel);

                if (dlg.ShowDialog(owner) == DialogResult.OK)
                {
                    onSaved?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public static void ShowDetailDialog(Control owner, long rawMaterialId, DataGridViewRow listRow = null)
        {
            if (rawMaterialId <= 0) return;

            var material = RawMaterialCtrl.GetById(rawMaterialId);
            DataTable quoteTable = null;
            try { quoteTable = RawMaterialCtrl.GetSupplierQuotesByMaterial(rawMaterialId); } catch { }

            if (quoteTable != null)
            {
                try
                {
                    quoteTable = GridHelper.DecorateStatusTable(quoteTable, "Status", DictionaryService.Categories.RawMaterial);
                }
                catch { }
            }

            var header = BuildHeaderTable(material, listRow);

            using (var dlg = new Form())
            {
                string titleCode = material?.RawMaterialCode
                    ?? listRow?.Cells["Raw Material Code"]?.Value?.ToString()
                    ?? rawMaterialId.ToString();
                dlg.Text = $"Raw Material — {titleCode}";
                dlg.Size = new Size(920, 620);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UITheme.Background;

                var split = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = Orientation.Horizontal,
                    SplitterDistance = 240
                };

                var headGrid = GridHelper.CreateStyledGrid();
                headGrid.DataSource = header;
                GridHelper.StyleGrid(headGrid);
                headGrid.Dock = DockStyle.Fill;
                split.Panel1.Controls.Add(headGrid);

                var quoteGrid = GridHelper.CreateStyledGrid();
                quoteGrid.DataSource = quoteTable;
                GridHelper.StyleGrid(quoteGrid);
                quoteGrid.Dock = DockStyle.Fill;
                if (quoteGrid.Columns.Contains("Supplier ID"))
                    quoteGrid.Columns["Supplier ID"].Visible = false;

                var quoteHeader = new Label
                {
                    Text = "Supplier Quotes",
                    Dock = DockStyle.Top,
                    Height = 28,
                    Padding = new Padding(8, 6, 0, 0),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = UITheme.TextDark
                };
                split.Panel2.Controls.Add(quoteGrid);
                split.Panel2.Controls.Add(quoteHeader);
                dlg.Controls.Add(split);

                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                var btnPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(8)
                };
                btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(btnPanel);
                dlg.ShowDialog(owner);
            }
        }

        private static DataTable BuildHeaderTable(RawMaterial material, DataGridViewRow listRow)
        {
            if (material != null)
            {
                var dt = new DataTable();
                dt.Columns.Add("Field");
                dt.Columns.Add("Value");
                dt.Rows.Add("Raw Material Code", material.RawMaterialCode ?? "");
                dt.Rows.Add("Category", material.Category ?? "");
                dt.Rows.Add("Size", material.Size ?? "");
                dt.Rows.Add("Color", material.Color ?? "");
                dt.Rows.Add("Min Stock Level", material.MinimumStockLevel.ToString());
                dt.Rows.Add("Status", DictionaryService.GetDisplayName(DictionaryService.Categories.RawMaterial, material.Status));
                return dt;
            }

            if (listRow != null)
            {
                var dt = new DataTable();
                dt.Columns.Add("Field");
                dt.Columns.Add("Value");
                foreach (DataGridViewCell cell in listRow.Cells)
                {
                    if (cell.OwningColumn == null || !cell.OwningColumn.Visible) continue;
                    string header = cell.OwningColumn.HeaderText ?? "";
                    if (header.Equals("Raw Material ID", StringComparison.OrdinalIgnoreCase)) continue;
                    string value = cell.Value?.ToString() ?? "";
                    if (header.Equals("Status", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(value, out int statusCode))
                        value = DictionaryService.GetDisplayName(DictionaryService.Categories.RawMaterial, statusCode);
                    dt.Rows.Add(header, value);
                }
                return dt;
            }

            return DetailViewHelper.SingleRowToFieldValueTable(null);
        }

        private static DataGridView CreateSupplierQuoteGrid(DataTable supplierTable, DataTable currencyTable)
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EditMode = DataGridViewEditMode.EditOnEnter
            };

            var supplierCol = new DataGridViewComboBoxColumn
            {
                Name = "Supplier",
                HeaderText = "Supplier *",
                DataSource = supplierTable?.Copy(),
                DisplayMember = "DisplayText",
                ValueMember = "Supplier ID",
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            };
            grid.Columns.Add(supplierCol);
            grid.Columns.Add("SupplierStyle", "Supplier Style #");
            grid.Columns.Add("BasePrice", "Base Price *");
            grid.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = "Currency",
                HeaderText = "Currency *",
                DataSource = currencyTable?.Copy(),
                DisplayMember = "Code",
                ValueMember = "Currency ID",
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            });
            grid.Columns.Add("Unit", "Unit *");
            grid.Columns.Add("MinOrderQty", "Min Order Qty *");
            grid.Columns.Add("QuoteDate", "Quote Date");
            var statusCol = new DataGridViewComboBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            };
            statusCol.Items.AddRange("0 - Inactive", "1 - Active");
            grid.Columns.Add(statusCol);
            GridHelper.ApplyStyle(grid);
            return grid;
        }

        private static void LoadQuoteGrid(DataGridView grid, IList<RawMaterialSupplierLine> lines)
        {
            grid.Rows.Clear();
            if (lines == null) return;
            foreach (var line in lines)
            {
                int idx = grid.Rows.Add();
                var row = grid.Rows[idx];
                row.Cells["Supplier"].Value = line.SupplierId;
                row.Cells["SupplierStyle"].Value = line.SupplierStyleNumber ?? "";
                row.Cells["BasePrice"].Value = line.BasePrice;
                row.Cells["Currency"].Value = line.CurrencyId > 0 ? line.CurrencyId : 1L;
                row.Cells["Unit"].Value = string.IsNullOrWhiteSpace(line.Unit) ? "piece" : line.Unit;
                row.Cells["MinOrderQty"].Value = line.MinimumOrderQuantity > 0 ? line.MinimumOrderQuantity : 1;
                row.Cells["QuoteDate"].Value = line.QuoteDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
                row.Cells["Status"].Value = line.Status >= 1 ? "1 - Active" : "0 - Inactive";
            }
        }

        private static void AddDefaultQuoteRow(DataGridView grid)
        {
            int idx = grid.Rows.Add();
            var row = grid.Rows[idx];
            if (grid.Columns["Currency"] is DataGridViewComboBoxColumn currencyCol
                && currencyCol.Items.Count == 0
                && currencyCol.DataSource is DataTable currencyDt
                && currencyDt.Rows.Count > 0)
                row.Cells["Currency"].Value = currencyDt.Rows[0]["Currency ID"];
            else
                row.Cells["Currency"].Value = 1L;
            row.Cells["Unit"].Value = "piece";
            row.Cells["MinOrderQty"].Value = 1;
            row.Cells["QuoteDate"].Value = DateTime.Today.ToString("yyyy-MM-dd");
            row.Cells["Status"].Value = "1 - Active";
        }

        private static void RemoveSelectedQuoteRow(DataGridView grid)
        {
            if (grid.CurrentRow == null || grid.CurrentRow.IsNewRow) return;
            grid.Rows.Remove(grid.CurrentRow);
        }

        private static bool TryReadQuoteLinesFromGrid(
            DataGridView grid,
            out List<RawMaterialSupplierLine> lines,
            out string error)
        {
            lines = new List<RawMaterialSupplierLine>();
            error = null;
            var seenSuppliers = new HashSet<long>();

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue;

                long supplierId = ParseCellLong(row.Cells["Supplier"]?.Value);
                string style = row.Cells["SupplierStyle"]?.Value?.ToString()?.Trim();
                string priceText = row.Cells["BasePrice"]?.Value?.ToString()?.Trim();
                long currencyId = ParseCellLong(row.Cells["Currency"]?.Value);
                string unit = row.Cells["Unit"]?.Value?.ToString()?.Trim();
                string minQtyText = row.Cells["MinOrderQty"]?.Value?.ToString()?.Trim();
                string quoteDateText = row.Cells["QuoteDate"]?.Value?.ToString()?.Trim();
                string statusText = row.Cells["Status"]?.Value?.ToString() ?? "1 - Active";

                bool rowEmpty = supplierId <= 0
                    && string.IsNullOrWhiteSpace(style)
                    && string.IsNullOrWhiteSpace(priceText)
                    && string.IsNullOrWhiteSpace(unit);
                if (rowEmpty) continue;

                if (supplierId <= 0)
                {
                    error = "Each quote line must have a supplier.";
                    return false;
                }
                if (!seenSuppliers.Add(supplierId))
                {
                    error = "Each supplier can only appear once on the quote list.";
                    return false;
                }
                if (!decimal.TryParse(priceText, out decimal basePrice) || basePrice <= 0)
                {
                    error = "Each quote line must have a base price greater than zero.";
                    return false;
                }
                if (currencyId <= 0) currencyId = 1;
                if (string.IsNullOrWhiteSpace(unit))
                {
                    error = "Each quote line must have a unit.";
                    return false;
                }
                if (!int.TryParse(minQtyText, out int minQty) || minQty <= 0)
                {
                    error = "Minimum order quantity must be at least 1.";
                    return false;
                }

                DateTime? quoteDate = null;
                if (!string.IsNullOrWhiteSpace(quoteDateText))
                {
                    if (!DateTime.TryParse(quoteDateText, out DateTime parsed))
                    {
                        error = "Quote date must be a valid date (e.g. 2026-06-18).";
                        return false;
                    }
                    quoteDate = parsed.Date;
                }

                int status = statusText.StartsWith("0", StringComparison.Ordinal) ? 0 : 1;
                lines.Add(new RawMaterialSupplierLine
                {
                    SupplierId = supplierId,
                    SupplierStyleNumber = style,
                    BasePrice = basePrice,
                    CurrencyId = currencyId,
                    Unit = unit,
                    MinimumOrderQuantity = minQty,
                    QuoteDate = quoteDate ?? DateTime.Today,
                    Status = status
                });
            }

            return true;
        }

        private static long ParseCellLong(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            if (value is long l) return l;
            if (value is int i) return i;
            return long.TryParse(value.ToString(), out long parsed) ? parsed : 0;
        }
    }
}
