using System;
using System.Text.RegularExpressions;

namespace FurnitureERP.Helpers
{
    public static class DeliveryAddressDisplayHelper
    {
        // e.g. "northpoint (hokman / 3123123)"
        private static readonly Regex CombinedPattern = new Regex(
            @"^(?<addr>.+?)\s*\(\s*(?<contact>[^/()]+?)\s*/\s*(?<phone>[^)]+?)\s*\)\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static string FormatDisplay(string address, string contactPerson, string phone)
        {
            address = (address ?? "").Trim();
            contactPerson = (contactPerson ?? "").Trim();
            phone = (phone ?? "").Trim();
            if (string.IsNullOrWhiteSpace(address))
                return "";
            if (string.IsNullOrWhiteSpace(contactPerson) && string.IsNullOrWhiteSpace(phone))
                return address;
            return $"{address} ({contactPerson} / {phone})";
        }

        public static bool TryParseCombined(string value, out string address, out string contactPerson, out string phone)
        {
            address = null;
            contactPerson = null;
            phone = null;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var m = CombinedPattern.Match(value.Trim());
            if (!m.Success)
                return false;

            address = m.Groups["addr"].Value.Trim();
            contactPerson = m.Groups["contact"].Value.Trim();
            phone = m.Groups["phone"].Value.Trim();
            return !string.IsNullOrWhiteSpace(address);
        }

        public static string ResolveAddressOnly(string selectedValue, string text)
        {
            if (!string.IsNullOrWhiteSpace(selectedValue))
                return selectedValue.Trim();

            if (TryParseCombined(text, out string address, out _, out _))
                return address;

            return (text ?? "").Trim();
        }
    }
}
