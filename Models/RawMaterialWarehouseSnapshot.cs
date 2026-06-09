namespace Sales_user.Models
{
    public class RawMaterialWarehouseSnapshot
    {
        public decimal Physical { get; set; }
        public decimal Reserved { get; set; }
        public decimal Purchased { get; set; }
        public decimal Available => Physical - Reserved < 0 ? 0 : Physical - Reserved;
        public decimal NetAvailable => Available + Purchased;
        public decimal MinimumStockLevel { get; set; }
    }
}
