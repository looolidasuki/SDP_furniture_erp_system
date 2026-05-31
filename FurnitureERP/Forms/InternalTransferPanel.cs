using System.Drawing;
using System.Windows.Forms;
using FurnitureERP.Helpers;

namespace FurnitureERP.Forms
{
    public class InternalTransferPanel : UserControl
    {
        public InternalTransferPanel()
        {
            Dock = DockStyle.Fill;
            BackColor = UITheme.Background;

            var title = new Label
            {
                Text = "Internal Transfer Form",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = UITheme.Primary,
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(16, 10, 0, 0)
            };

            var info = new Label
            {
                Text = "Record stock transfers between warehouses. Create and edit transfers according to your department permissions.",
                Font = new Font("Segoe UI", 10),
                ForeColor = UITheme.TextGray,
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(16, 0, 16, 0)
            };

            var placeholder = new Label
            {
                Text = AppSession.CanCreate(PermissionModule.InternalTransferForm)
                    ? "Internal transfer workflow will be available here."
                    : "You have view-only access to internal transfers.",
                Font = new Font("Segoe UI", 10),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopCenter,
                Padding = new Padding(0, 40, 0, 0)
            };

            Controls.Add(placeholder);
            Controls.Add(info);
            Controls.Add(title);
        }
    }
}
