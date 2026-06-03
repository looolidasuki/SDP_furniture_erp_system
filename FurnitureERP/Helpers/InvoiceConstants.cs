namespace FurnitureERP.Helpers
{
    public static class InvoiceConstants
    {
        public const int TypeDeposit = 1;
        public const int TypeNormal = 2;

        /// <summary>Reserved ID for virtual deposit delivery note (see EnsureDepositDeliveryNoteId).</summary>
        public const long DepositDeliveryNoteReservedId = 999999;

        public const string DepositDeliveryNoteCode = "DN-DEPOSIT";

        public const string DepositProductCode = "DEPOSIT";
    }
}
