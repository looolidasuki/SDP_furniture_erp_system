using System;
using System.Drawing;
using System.Windows.Forms;
using Sales_user.Controllers;
using Sales_user.Models;
using FurnitureERP.Helpers;

namespace FurnitureERP.Forms
{
    public class StaffPanel : UserControl
    {
        private readonly StaffController _staffCtrl = new StaffController();
        private DataGridView _grid;

        public StaffPanel()
        {
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;
            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UITheme.Background };

            var btnNew = UITheme.CreatePrimaryButton("+ New Staff");
            btnNew.Location = new Point(8, 8);
            btnNew.Click += (s, e) => ShowStaffDialog(null);

            var btnRefresh = UITheme.CreateSecondaryButton("↻ Refresh");
            btnRefresh.Location = new Point(btnNew.Right + 10, 8);
            btnRefresh.Click += (s, e) => LoadData();

            var btnReset = UITheme.CreateSecondaryButton("Reset Password");
            btnReset.Location = new Point(btnRefresh.Right + 10, 8);
            btnReset.Click += (s, e) => ResetSelectedPassword();

            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnRefresh);
            toolbar.Controls.Add(btnReset);

            _grid = GridHelper.CreateStyledGrid();
            _grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || _grid.Rows[e.RowIndex].Cells[0].Value == null) return;
                long id = Convert.ToInt64(_grid.Rows[e.RowIndex].Cells[0].Value);
                ShowStaffDialog(_staffCtrl.GetById(id));
            };

            Controls.Add(_grid);
            Controls.Add(toolbar);
        }

        private void LoadData()
        {
            try
            {
                var dt = _staffCtrl.GetAllStaff();
                DictionaryService.DecorateStatusColumn(dt, "Status", DictionaryService.Categories.Staff);
                _grid.DataSource = dt;
                GridHelper.StyleGrid(_grid);
            }
            catch (Exception ex)
            {
                UITheme.ShowError(ex.Message);
            }
        }

        private void ResetSelectedPassword()
        {
            if (_grid.CurrentRow?.Cells[0].Value == null)
            {
                UITheme.ShowWarning("Please select a staff member first.");
                return;
            }

            long id = Convert.ToInt64(_grid.CurrentRow.Cells[0].Value);
            using (var dlg = UITheme.BuildInputDialog("Reset Password", new[] { "New Password *" }))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string password = UITheme.GetDialogValues(dlg)[0];
                if (string.IsNullOrWhiteSpace(password))
                {
                    UITheme.ShowWarning("Password is required.");
                    return;
                }
                if (_staffCtrl.ResetPassword(id, password))
                    UITheme.ShowSuccess("Password reset successfully.");
                else
                    UITheme.ShowError("Failed to reset password.");
            }
        }

        private void ShowStaffDialog(Staff existing)
        {
            bool isEdit = existing != null;
            using (var dlg = new Form())
            {
                dlg.Text = isEdit ? "Edit Staff" : "New Staff";
                dlg.Size = new Size(520, 460);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.BackColor = UITheme.Background;

                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8, Padding = new Padding(16) };
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                var txtUsername = new TextBox { Text = existing?.Username ?? "" };
                var txtFirst = new TextBox { Text = existing?.FirstName ?? "" };
                var txtLast = new TextBox { Text = existing?.LastName ?? "" };
                var txtTitle = new TextBox { Text = existing?.Title ?? "" };
                var txtDept = new TextBox { Text = existing?.Department ?? "" };
                var txtEmail = new TextBox { Text = existing?.Email ?? "" };
                var txtPhone = new TextBox { Text = existing?.Phone ?? "" };
                var txtPassword = new TextBox { UseSystemPasswordChar = true, Enabled = !isEdit };
                var cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
                DictionaryUIHelper.BindStatusCombo(cmbStatus, DictionaryService.Categories.Staff, existing?.Status ?? 1);

                UITheme.AddFormRow(layout, 0, "Username *", txtUsername);
                UITheme.AddFormRow(layout, 1, "First Name *", txtFirst);
                UITheme.AddFormRow(layout, 2, "Last Name *", txtLast);
                UITheme.AddFormRow(layout, 3, "Title *", txtTitle);
                UITheme.AddFormRow(layout, 4, "Department *", txtDept);
                UITheme.AddFormRow(layout, 5, "Email *", txtEmail);
                if (isEdit)
                {
                    UITheme.AddFormRow(layout, 6, "Status", cmbStatus);
                    UITheme.AddFormRow(layout, 7, "Phone", txtPhone);
                }
                else
                {
                    UITheme.AddFormRow(layout, 6, "Password *", txtPassword);
                    UITheme.AddFormRow(layout, 7, "Phone", txtPhone);
                }

                var btnSave = UITheme.CreatePrimaryButton(isEdit ? "Update" : "Save");
                var btnClose = UITheme.CreateSecondaryButton("Close");
                btnClose.Click += (s, e) => dlg.Close();
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtFirst.Text) ||
                        string.IsNullOrWhiteSpace(txtLast.Text) || string.IsNullOrWhiteSpace(txtEmail.Text))
                    {
                        UITheme.ShowWarning("Username, name and email are required.");
                        return;
                    }

                    try
                    {
                        if (isEdit)
                        {
                            existing.Username = txtUsername.Text.Trim();
                            existing.FirstName = txtFirst.Text.Trim();
                            existing.LastName = txtLast.Text.Trim();
                            existing.Title = txtTitle.Text.Trim();
                            existing.Department = txtDept.Text.Trim();
                            existing.Email = txtEmail.Text.Trim();
                            existing.Phone = txtPhone.Text.Trim();
                            existing.Status = DictionaryUIHelper.GetSelectedStatusCode(cmbStatus);
                            if (!_staffCtrl.Update(existing))
                            {
                                UITheme.ShowError("Failed to update staff.");
                                return;
                            }
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(txtPassword.Text))
                            {
                                UITheme.ShowWarning("Password is required for new staff.");
                                return;
                            }
                            _staffCtrl.Insert(new Staff
                            {
                                Username = txtUsername.Text.Trim(),
                                Password = txtPassword.Text,
                                FirstName = txtFirst.Text.Trim(),
                                LastName = txtLast.Text.Trim(),
                                Title = txtTitle.Text.Trim(),
                                Department = txtDept.Text.Trim(),
                                Email = txtEmail.Text.Trim(),
                                Phone = txtPhone.Text.Trim(),
                                EmployDate = DateTime.Today,
                                Status = 1
                            });
                        }

                        UITheme.ShowSuccess(isEdit ? "Staff updated." : "Staff created.");
                        dlg.DialogResult = DialogResult.OK;
                        dlg.Close();
                    }
                    catch (Exception ex)
                    {
                        UITheme.ShowError(ex.Message);
                    }
                };

                var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
                btnPanel.Controls.Add(btnSave);
                btnPanel.Controls.Add(btnClose);
                dlg.Controls.Add(layout);
                dlg.Controls.Add(btnPanel);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadData();
            }
        }
    }
}
