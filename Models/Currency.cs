namespace Sales_user.Models
{
    public class Currency
    {
        public long CurrencyID { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencySymbol { get; set; }
        public decimal RateToBase { get; set; }
        public bool IsBaseCurrency { get; set; }
        public int DecimalPlaces { get; set; } = 2;
        public bool IsEnabled { get; set; } = true;
    }
}
