namespace FurnitureERP.Helpers
{
    /// <summary>receiptvoucherinvoice.type — maps to FINANCIAL_CLEARING_TYPE dictionary.</summary>
    public static class ReceiptVoucherConstants
    {
        public const int ClearingDeposit = 1;
        public const int ClearingPartial = 2;
        public const int ClearingFinal = 3;
        public const int ClearingExchangeLoss = 4;

        public static bool IsExchangeLoss(int clearingType) => clearingType == ClearingExchangeLoss;
    }
}
