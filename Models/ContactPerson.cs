namespace Sales_user.Models
{
    public class ContactPerson
    {
        public long ContactPersonID { get; set; }
        public long CustomerID { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }
}
