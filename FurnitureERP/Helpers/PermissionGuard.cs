using System.Windows.Forms;

namespace FurnitureERP.Helpers
{
    public static class PermissionGuard
    {
        public static bool Ensure(string module, PermissionAction action, Control owner = null)
        {
            if (AppSession.IsSuperUser) return true;
            bool allowed = PermissionService.Has(AppSession.CurrentUser, module, action);
            if (!allowed)
                UITheme.ShowWarning(GetDeniedMessage(module, action));
            return allowed;
        }

        public static void ApplyCreateButton(Button button, string module)
        {
            bool allowed = AppSession.CanCreate(module);
            button.Visible = allowed;
            button.Enabled = allowed;
        }

        public static void ApplyEditButton(Button button, string module)
        {
            bool allowed = AppSession.CanEdit(module);
            button.Visible = allowed;
            button.Enabled = allowed;
        }

        private static string GetDeniedMessage(string module, PermissionAction action)
        {
            string verb = action == PermissionAction.Create ? "create" : action == PermissionAction.Edit ? "edit" : "view";
            return $"You do not have permission to {verb} {FormatModuleName(module)}.";
        }

        private static string FormatModuleName(string module)
        {
            if (string.IsNullOrEmpty(module)) return "this module";
            var spaced = System.Text.RegularExpressions.Regex.Replace(module, "([a-z])([A-Z])", "$1 $2");
            return spaced.ToLowerInvariant();
        }
    }
}
