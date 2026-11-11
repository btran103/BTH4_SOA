namespace BTH4_SOA.DTOs
{
    public class CreateOrderItemDto
    {
        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }
    }

    public class UpdateOrderItemDto
    {
        public int Quantity { get; set; }
    }
}
