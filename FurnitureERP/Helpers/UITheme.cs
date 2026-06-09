using System;
using System.Drawing;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class UITheme
    {
        // Color palette
        public static readonly Color Primary = Color.FromArgb(30, 80, 160);
        public static readonly Color PrimaryLight = Color.FromArgb(60, 120, 210);
        public static readonly Color PrimaryDark = Color.FromArgb(15, 50, 110);
        public static readonly Color Background = Color.FromArgb(245, 247, 252);
        public static readonly Color NavDark = Color.FromArgb(22, 40, 70);
        public static readonly Color NavDarkest = Color.FromArgb(12, 25, 50);
        public static readonly Color NavHover = Color.FromArgb(40, 70, 120);
        public static readonly Color NavActive = Color.FromArgb(30, 80, 160);
        public static readonly Color TextDark = Color.FromArgb(30, 40, 60);
        public static readonly Color TextGray = Color.FromArgb(110, 120, 140);
        public static readonly Color Success = Color.FromArgb(0, 168, 120);
        public static readonly Color Warning = Color.FromArgb(230, 150, 20);
        public static readonly Color Danger = Color.FromArgb(210, 50, 50);
        public static readonly Color CardBorder = Color.FromArgb(220, 225, 240);

        // Button factories
        public static Button CreatePrimaryButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Height = 32,
                AutoSize = false,
                Width = 140,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        public static Button CreateSecondaryButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = Color.White,
                ForeColor = Primary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Height = 32,
                AutoSize = false,
                Width = 110,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Primary;
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        // Card panel
        public static Panel CreateCard(string title)
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Padding = new Padding(10),
                Margin = new Padding(6)
            };
            card.Paint += (s, e) =>
            {
                using (var pen = new System.Drawing.Pen(Color.FromArgb(220, 228, 240)))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            if (!string.IsNullOrEmpty(title))
            {
                var titleLabel = new Label
                {
                    Text = title,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = TextDark,
                    Dock = DockStyle.Top,
                    Height = 32,
                    Padding = new Padding(4, 4, 0, 0)
                };
                card.Controls.Add(titleLabel);
            }
            return card;
        }

        // Form field helpers
        public static void AddFormRow(TableLayoutPanel layout, int row, string labelText, Control control)
        {
            const int topInset = 8;
            bool useFill = (control is TextBox textBox && textBox.Multiline)
                || control is Panel
                || control is FlowLayoutPanel
                || control is TableLayoutPanel;

            var lbl = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 9),
                ForeColor = TextDark,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(0, topInset, 0, 0),
                Margin = Padding.Empty
            };

            if (useFill)
            {
                control.Dock = DockStyle.Fill;
                control.Margin = new Padding(0, topInset, 0, 0);
            }
            else
            {
                control.Height = control is ComboBox ? 24 : 23;
                control.Dock = DockStyle.None;
                control.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
                control.Margin = new Padding(0, topInset, 0, 0);
            }

            layout.Controls.Add(lbl, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        public static void AddFormField(TableLayoutPanel layout, int row, string labelText, Control control)
        {
            AddFormRow(layout, row, labelText, control);
        }

        // Generic input dialog
        public static Form BuildInputDialog(string title, string[] fieldNames)
        {
            var dlg = new Form
            {
                Text = title,
                Size = new Size(420, 80 + fieldNames.Length * 46 + 60),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                BackColor = Background
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = fieldNames.Length + 1,
                Padding = new Padding(16)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            for (int i = 0; i < fieldNames.Length; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
                var lbl = new Label
                {
                    Text = fieldNames[i],
                    Font = new Font("Segoe UI", 9),
                    ForeColor = TextDark,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                var txt = new TextBox { Dock = DockStyle.Fill, Tag = fieldNames[i] };
                layout.Controls.Add(lbl, 0, i);
                layout.Controls.Add(txt, 1, i);
            }

            var btnSave = CreatePrimaryButton("OK");
            var btnCancel = CreateSecondaryButton("Cancel");

            btnSave.Click += (s, e) => { dlg.DialogResult = DialogResult.OK; dlg.Close(); };
            btnCancel.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8)
            };
            btnPanel.Controls.Add(btnSave);
            btnPanel.Controls.Add(btnCancel);

            dlg.Controls.Add(layout);
            dlg.Controls.Add(btnPanel);
            dlg.Tag = layout;  // store layout for value retrieval

            return dlg;
        }

        // Message helpers
        public static void ShowWarning(string message) =>
            MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        public static void ShowSuccess(string message) =>
            MessageBox.Show(message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

        public static void ShowError(string message) =>
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        public static string[] GetDialogValues(Form dlg)
        {
            var layout = dlg.Tag as TableLayoutPanel;
            if (layout == null) return new string[0];

            var values = new System.Collections.Generic.List<string>();
            foreach (Control ctrl in layout.Controls)
            {
                if (ctrl is TextBox txt)
                    values.Add(txt.Text.Trim());
                else if (ctrl is ComboBox cmb)
                    values.Add(cmb.SelectedItem?.ToString() ?? "");
            }
            return values.ToArray();
        }
    }
}
