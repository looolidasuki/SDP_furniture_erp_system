using System;

namespace FurnitureERP.Helpers
{
    /// <summary>
    /// HKD is the base currency (currencyID = 1, rateToBase = 1).
    /// Document exchangeRate stores the locked rateToBase value at save time.
    /// </summary>
    public static class CurrencyConversionService
    {
        public const long BaseCurrencyId = 1;

        public static decimal LockRate(decimal rateToBase)
        {
            if (rateToBase <= 0m) return 1m;
            return Math.Round(rateToBase, 4, MidpointRounding.AwayFromZero);
        }

        public static decimal ToBaseAmount(decimal foreignAmount, decimal exchangeRate)
        {
            if (exchangeRate <= 0m) exchangeRate = 1m;
            return Math.Round(foreignAmount * exchangeRate, 2, MidpointRounding.AwayFromZero);
        }

        public static decimal FromBaseAmount(decimal baseAmount, decimal exchangeRate)
        {
            if (exchangeRate <= 0m) exchangeRate = 1m;
            return Math.Round(baseAmount / exchangeRate, 2, MidpointRounding.AwayFromZero);
        }

        public static decimal ConvertPrice(decimal basePrice, decimal productRateToBase, decimal targetRateToBase)
        {
            return CurrencyPriceHelper.ConvertPrice(basePrice, productRateToBase, targetRateToBase);
        }
    }
}
