using System;
using System.Text.RegularExpressions;

namespace Sales_user.Controllers
{
    /// <summary>Input validation helpers for SQL parameters and dynamic identifiers.</summary>
    public static class SqlGuard
    {
        public const int MaxLoginCredentialLength = 128;

        private static readonly Regex IdentifierPattern = new Regex(
            @"^[A-Za-z_][A-Za-z0-9_]*$",
            RegexOptions.Compiled);

        public static string SanitizeLoginCredential(string value, int maxLength = MaxLoginCredentialLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Trim();
            if (value.Length > maxLength)
                value = value.Substring(0, maxLength);
            return value;
        }

        public static string EscapeLikeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
        }

        public static string ValidateIdentifier(string name, string paramName = "identifier")
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("SQL identifier cannot be empty.", paramName);

            string trimmed = name.Trim();
            if (!IdentifierPattern.IsMatch(trimmed))
                throw new ArgumentException("Invalid SQL identifier: " + name, paramName);

            return trimmed;
        }

        public static int ClampLimit(int value, int max = 500)
        {
            if (value < 1) return 1;
            return value > max ? max : value;
        }
    }
}
