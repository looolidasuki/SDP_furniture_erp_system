using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class ControlUiHelper
    {
        public static void CloseAllDropDowns(Control root)
        {
            if (root == null || root.IsDisposed) return;

            if (root is ComboBox combo && combo.DroppedDown)
                combo.DroppedDown = false;

            foreach (Control child in root.Controls)
                CloseAllDropDowns(child);
        }

        public static void DisposeChildControls(Control host)
        {
            if (host == null || host.IsDisposed) return;

            while (host.Controls.Count > 0)
            {
                Control child = host.Controls[0];
                CloseAllDropDowns(child);
                host.Controls.RemoveAt(0);
                child.Dispose();
            }
        }
    }
}
