using System;

namespace FurnitureERP.Helpers
{
    public static class OrderTotalCalculator
    {
        public static decimal ApplyHeaderDiscount(decimal lineSubtotal, string discountType, decimal discount)
        {
            if (lineSubtotal <= 0m || discount <= 0m) return lineSubtotal;

            if (string.Equals(discountType, "Percentage", StringComparison.OrdinalIgnoreCase))
                return Math.Round(lineSubtotal * (1m - discount / 100m), 2, MidpointRounding.AwayFromZero);

            if (string.Equals(discountType, "Fixed Amount", StringComparison.OrdinalIgnoreCase))
                return Math.Max(0m, Math.Round(lineSubtotal - discount, 2, MidpointRounding.AwayFromZero));

            return lineSubtotal;
        }
    }
}
