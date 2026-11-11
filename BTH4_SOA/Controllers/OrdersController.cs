using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTH4_SOA.Data;
using BTH4_SOA.Models;
using BTH4_SOA.DTOs;
using BTH4_SOA.Services;

namespace BTH4_SOA.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly ProductService _productService;

        public OrdersController(
            OrderDbContext context,
            ProductService productService)
        {
            _context = context;
            _productService = productService;
        }

        // GET: /orders
        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ToListAsync();

            return Ok(orders);
        }

        // GET: /orders/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            return Ok(order);
        }

        // POST: /orders
        [HttpPost]
        public async Task<IActionResult> CreateOrder(CreateOrderDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CustomerName) ||
                string.IsNullOrWhiteSpace(dto.CustomerEmail) ||
                dto.Items == null || !dto.Items.Any())
            {
                return BadRequest(new { message = "Thông tin đơn hàng không hợp lệ." });
            }

            var order = new Order
            {
                CustomerName = dto.CustomerName.Trim(),
                CustomerEmail = dto.CustomerEmail.Trim(),
                Status = "pending"
            };

            foreach (var item in dto.Items)
            {
                if (item.ProductId <= 0 || item.Quantity <= 0)
                    return BadRequest(new { message = "Sản phẩm hoặc số lượng không hợp lệ." });

                ProductDto? product;
                try
                {
                    product = await _productService.GetProduct(item.ProductId);
                }
                catch (InvalidOperationException ex)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
                }

                if (product == null)
                    return BadRequest(new { message = $"Sản phẩm với ID {item.ProductId} không tồn tại." });

                if (product.Quantity < item.Quantity)
                    return BadRequest(new { message = $"Số lượng tồn kho không đủ cho sản phẩm '{product.Name}'. (Hiện còn: {product.Quantity}, yêu cầu: {item.Quantity})" });

                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * item.Quantity
                };

                order.OrderItems.Add(orderItem);

                order.TotalAmount += orderItem.TotalPrice;
            }

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetOrder),
                new { id = order.Id },
                order
            );
        }

        // PUT: /orders/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(
            int id, UpdateOrderStatusDto dto)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            if (string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest("Trạng thái không được để trống.");

            var status = dto.Status.Trim().ToLower();
            var statuses = new[] { "pending", "completed", "cancelled" };

            if (!statuses.Contains(status))
                return BadRequest("Trạng thái không hợp lệ (các trạng thái hợp lệ: pending, completed, cancelled).");

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(order);
        }

        // DELETE: /orders/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();

            return Ok("Xóa đơn hàng thành công.");
        }
    }
}
