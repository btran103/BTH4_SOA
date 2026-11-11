namespace BTH4_SOA.DTOs
{
    public class CreateOrderDto
    {
        public string CustomerName { get; set; } = "";

        public string CustomerEmail { get; set; } = "";

        public List<CreateOrderItemDto> Items { get; set; } = new();
    }
}