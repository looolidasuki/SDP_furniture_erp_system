using Sales_user.Models;

namespace FurnitureERP.Helpers
{
    public static class AppSession
    {
        public static Staff CurrentUser { get; set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static bool IsSuperUser => PermissionService.IsSuperUser(CurrentUser);

        public static bool CanView(string module) => PermissionService.Has(CurrentUser, module, PermissionAction.View);

        public static bool CanCreate(string module) => PermissionService.Has(CurrentUser, module, PermissionAction.Create);

        public static bool CanEdit(string module) => PermissionService.Has(CurrentUser, module, PermissionAction.Edit);

        public static void Clear() => CurrentUser = null;
    }
}
