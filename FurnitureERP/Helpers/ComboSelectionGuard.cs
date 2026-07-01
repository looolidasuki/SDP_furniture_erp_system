using System;
using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    /// <summary>
    /// Safe managed SelectedIndex clamp for ComboBox after Items/DataSource changes.
    /// Does not hook the message pump — that breaks typing in editable (DropDown) combos.
    /// </summary>
    internal static class ComboSelectionGuard
    {
        public static void Clamp(ComboBox combo)
        {
            if (combo == null || combo.IsDisposed || !combo.IsHandleCreated) return;

            try
            {
                int count = combo.Items.Count;
                if (count == 0)
                {
                    if (combo.SelectedIndex != -1)
                        combo.SelectedIndex = -1;
                    return;
                }

                int idx = combo.SelectedIndex;
                if (idx >= count)
                    combo.SelectedIndex = -1;
            }
            catch (ArgumentOutOfRangeException)
            {
                try { combo.SelectedIndex = -1; } catch { }
            }
            catch
            {
                try { combo.SelectedIndex = -1; } catch { }
            }
        }

        public static void BeforeRebind(ComboBox combo, bool closeDropDown = true)
        {
            if (combo == null || combo.IsDisposed) return;
            if (closeDropDown)
            {
                try
                {
                    if (combo.DroppedDown) combo.DroppedDown = false;
                }
                catch { }
            }

            try { combo.SelectedIndex = -1; } catch { }
        }
    }
}
