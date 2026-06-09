namespace FurnitureERP.Helpers
{
    public static class RawMaterialRequestNoteConstants
    {
        public const int StatusDraft = 0;
        public const int StatusPartiallyIssued = 1;
        public const int StatusCompleted = 2;
        public const int StatusCancelled = 3;

        public static string GetStatusLabel(int status)
        {
            switch (status)
            {
                case StatusDraft: return "Draft";
                case StatusPartiallyIssued: return "Partially Issued";
                case StatusCompleted: return "Completed";
                case StatusCancelled: return "Cancelled";
                default: return status.ToString();
            }
        }
    }
}
