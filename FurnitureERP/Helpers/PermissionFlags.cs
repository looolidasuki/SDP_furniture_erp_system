using System;

namespace FurnitureERP.Helpers
{
    [Flags]
    public enum PermissionFlags
    {
        None = 0,
        View = 1,
        Create = 2,
        Edit = 4,
        All = View | Create | Edit
    }

    public enum PermissionAction
    {
        View,
        Create,
        Edit
    }
}
