using System.Text.Json.Serialization;

namespace BTH4_SOA.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; } = "";

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        [JsonIgnore]
        public Order? Order { get; set; }
    }
}
