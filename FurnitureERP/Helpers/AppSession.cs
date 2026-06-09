using System;
using Sales_user.Models;

namespace FurnitureERP.Helpers
{
    public static class AppSession
    {
        private static readonly TimeSpan SessionTimeout = TimeSpan.FromHours(8);

        public static Staff CurrentUser { get; set; }
        public static DateTime? LoginTime { get; private set; }
        public static DateTime LastActivity { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null && !IsSessionExpired;

        public static bool IsSuperUser => PermissionService.IsSuperUser(CurrentUser);

        public static bool IsSessionExpired =>
            LoginTime.HasValue && DateTime.Now - LastActivity > SessionTimeout;

        public static void StartSession(Staff user)
        {
            CurrentUser = user;
            LoginTime = DateTime.Now;
            TouchActivity();
        }

        public static void TouchActivity() => LastActivity = DateTime.Now;

        public static bool CanView(string module) => IsLoggedIn && PermissionService.Has(CurrentUser, module, PermissionAction.View);

        public static bool CanCreate(string module) => IsLoggedIn && PermissionService.Has(CurrentUser, module, PermissionAction.Create);

        public static bool CanEdit(string module) => IsLoggedIn && PermissionService.Has(CurrentUser, module, PermissionAction.Edit);

        /// <summary>When set, Internal Transfer opens on Issue RM Request with this note pre-selected.</summary>
        public static long? PendingRmRequestNoteId { get; set; }

        public static void Clear()
        {
            CurrentUser = null;
            LoginTime = null;
            PendingRmRequestNoteId = null;
        }
    }
}
