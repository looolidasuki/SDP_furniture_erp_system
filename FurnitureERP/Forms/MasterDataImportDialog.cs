using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using FurnitureERP.Helpers;

namespace FurnitureERP.Forms
{
    public class MasterDataImportDialog : Form
    {
        private readonly ComboBox _cmbKind;
        private readonly Label _lblHint;
        private readonly Label _lblFile;
        private readonly DataGridView _grid;
        private readonly CheckBox _chkUpsert;
        private List<MasterDataImportPreviewRow> _preview = new List<MasterDataImportPreviewRow>();
        private string _filePath;

        public MasterDataImportDialog()
        {
            Text = "Import Master Data (CSV)";
            Size = new Size(960, 620);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UITheme.Background;
            MinimumSize = new Size(800, 500);

            var top = new Panel { Dock = DockStyle.Top, Height = 118, Padding = new Padding(12, 10, 12, 0) };

            _cmbKind = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Left = 12,
                Top = 10
            };
            _cmbKind.Items.AddRange(new object[]
            {
                MasterDataImportKind.Customer,
                MasterDataImportKind.Supplier,
                MasterDataImportKind.Product,
                MasterDataImportKind.RawMaterial
            });
            _cmbKind.SelectedIndex = 0;
            _cmbKind.SelectedIndexChanged += (s, e) =>
            {
                UpdateHint();
                if (!string.IsNullOrWhiteSpace(_filePath))
                    LoadFile(_filePath);
            };

            _lblHint = new Label
            {
                AutoSize = false,
                Left = 12,
                Top = 42,
                Width = 900,
                Height = 36,
                ForeColor = UITheme.TextGray,
                Font = new Font("Segoe UI", 9f)
            };

            _lblFile = new Label
            {
                AutoSize = false,
                Left = 12,
                Top = 78,
                Width = 900,
                Height = 20,
                ForeColor = UITheme.TextDark,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                Text = "No file selected"
            };

            var btnBrowse = UITheme.CreateSecondaryButton("Browse CSV...");
            btnBrowse.Width = 120;
            btnBrowse.Left = 220;
            btnBrowse.Top = 8;
            btnBrowse.Click += (s, e) => BrowseFile();

            var btnSample = UITheme.CreateSecondaryButton("Open Sample Folder");
            btnSample.Width = 150;
            btnSample.Left = 350;
            btnSample.Top = 8;
            btnSample.Click += (s, e) => CsvImportHelper.RevealSampleFolder(this);

            var btnLoadSample = UITheme.CreateSecondaryButton("Load Sample File");
            btnLoadSample.Width = 130;
            btnLoadSample.Left = 510;
            btnLoadSample.Top = 8;
            btnLoadSample.Click += (s, e) => LoadSampleForKind();

            top.Controls.Add(new Label { Text = "Data type", Left = 12, Top = 12, AutoSize = true });
            top.Controls.Add(_cmbKind);
            top.Controls.Add(btnBrowse);
            top.Controls.Add(btnSample);
            top.Controls.Add(btnLoadSample);
            top.Controls.Add(_lblHint);
            top.Controls.Add(_lblFile);

            _grid = GridHelper.CreateStyledGrid();
            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;

            _chkUpsert = new CheckBox
            {
                Text = "Update existing records when key matches (customer/supplier name, product/raw material code)",
                AutoSize = true,
                Checked = true,
                Padding = new Padding(12, 0, 0, 0)
            };

            var btnImport = UITheme.CreatePrimaryButton("Import Valid Rows");
            var btnClose = UITheme.CreateSecondaryButton("Close");
            btnClose.Click += (s, e) => Close();
            btnImport.Click += (s, e) => RunImport();

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 88, Padding = new Padding(12) };
            bottom.Controls.Add(_chkUpsert);
            _chkUpsert.Location = new Point(12, 8);

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 4, 0, 0)
            };
            btnPanel.Controls.Add(btnImport);
            btnPanel.Controls.Add(btnClose);
            bottom.Controls.Add(btnPanel);

            Controls.Add(_grid);
            Controls.Add(bottom);
            Controls.Add(top);

            UpdateHint();
        }

        public static void ShowImportDialog(IWin32Window owner)
        {
            using (var dlg = new MasterDataImportDialog())
                ((Form)dlg).ShowDialog(owner);
        }

        private MasterDataImportKind SelectedKind =>
            (MasterDataImportKind)_cmbKind.SelectedItem;

        private void UpdateHint()
        {
            _lblHint.Text = MasterDataCsvImportService.GetRequiredColumnsHint(SelectedKind);
        }

        private void BrowseFile()
        {
            if (!CsvImportHelper.TryPickCsvFile(this, out string path)) return;
            LoadFile(path);
        }

        private void LoadSampleForKind()
        {
            string path = MasterDataCsvImportService.GetSampleFilePath(SelectedKind);
            if (!System.IO.File.Exists(path))
            {
                UITheme.ShowWarning("Sample file not found:\n" + path);
                return;
            }
            LoadFile(path);
        }

        private void LoadFile(string path)
        {
            try
            {
                var table = CsvImportHelper.ReadCsvFile(path);
                _filePath = path;
                _lblFile.Text = "File: " + path + "  (" + table.Rows.Count + " data row(s))";
                _preview = MasterDataCsvImportService.BuildPreview(SelectedKind, table);
                _grid.DataSource = MasterDataCsvImportService.BuildPreviewGrid(_preview);
                GridHelper.StyleGrid(_grid);
                if (_grid.Columns.Contains("Status"))
                    GridHelper.StyleTextStatusColumn(_grid, "Status");
            }
            catch (Exception ex)
            {
                UITheme.ShowError("Failed to read CSV: " + ex.Message);
            }
        }

        private void RunImport()
        {
            if (_preview == null || _preview.Count == 0)
            {
                UITheme.ShowWarning("Load a CSV file first.");
                return;
            }

            int valid = 0;
            foreach (var row in _preview)
                if (row.IsValid) valid++;
            if (valid == 0)
            {
                UITheme.ShowWarning("No valid rows to import. Fix the CSV and reload.");
                return;
            }

            if (MessageBox.Show(this,
                    $"Import {valid} valid row(s) as {MasterDataCsvImportService.GetKindDisplayName(SelectedKind)}?",
                    "Confirm Import",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var result = MasterDataCsvImportService.Import(SelectedKind, _preview, _chkUpsert.Checked);
            string summary = $"Created: {result.Created}, Updated: {result.Updated}, Skipped: {result.Skipped}, Failed: {result.Failed}";
            if (result.Messages.Count > 0)
                summary += "\n\n" + string.Join("\n", result.Messages);
            if (result.Failed > 0)
                UITheme.ShowWarning(summary);
            else
                UITheme.ShowSuccess(summary);

            DialogResult = DialogResult.OK;
        }
    }
}
