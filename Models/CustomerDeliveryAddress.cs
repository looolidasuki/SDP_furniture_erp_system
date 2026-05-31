namespace Sales_user.Models
{
    public class CustomerDeliveryAddress
    {
        public long AddressID { get; set; }
        public long CustomerID { get; set; }
        public string DeliveryAddress { get; set; }
        public string ContactPerson { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }
}
