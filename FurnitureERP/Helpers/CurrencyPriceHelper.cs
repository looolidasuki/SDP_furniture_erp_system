using System;

namespace FurnitureERP.Helpers
{
    public static class CurrencyPriceHelper
    {
        /// <summary>
        /// Converts a product base price from its pricing currency into the target order/quote currency
        /// using rateToBase (HKD = 1.00 benchmark).
        /// </summary>
        public static decimal ConvertPrice(decimal basePrice, decimal productRateToBase, decimal targetRateToBase)
        {
            if (targetRateToBase <= 0m) return basePrice;
            return Math.Round(basePrice * productRateToBase / targetRateToBase, 2, MidpointRounding.AwayFromZero);
        }
    }
}
