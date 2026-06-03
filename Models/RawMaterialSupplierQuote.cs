namespace Sales_user.Models
{
    public class RawMaterialSupplierQuote
    {
        public decimal BasePrice { get; set; }
        public int MinimumOrderQuantity { get; set; }
        public string Unit { get; set; }
        public string CurrencyCode { get; set; }
    }
}
