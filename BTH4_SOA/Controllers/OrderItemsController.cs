using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BTH4_SOA.Data;
using BTH4_SOA.Models;
using BTH4_SOA.Services;
using BTH4_SOA.DTOs;

namespace BTH4_SOA.Controllers
{
    [ApiController]
    [Route("order_items")]
    public class OrderItemsController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly ProductService _productService;

        public OrderItemsController(
            OrderDbContext context,
            ProductService productService)
        {
            _context = context;
            _productService = productService;
        }

        // GET: /order_items
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _context.OrderItems.ToListAsync();

            return Ok(items);
        }

        // GET: /order_items/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.OrderItems.FindAsync(id);

            if (item == null)
                return NotFound("Không tìm thấy chi tiết đơn hàng.");

            return Ok(item);
        }

        // POST: /order_items
        [HttpPost]
        public async Task<IActionResult> Create(CreateOrderItemDto dto)
        {
            if (dto.Quantity <= 0 || dto.ProductId <= 0 || dto.OrderId <= 0)
                return BadRequest("Thông tin không hợp lệ.");

            var order = await _context.Orders.FindAsync(dto.OrderId);

            if (order == null)
                return NotFound("Đơn hàng không tồn tại.");

            ProductDto? product;
            try
            {
                product = await _productService.GetProduct(dto.ProductId);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
            }

            if (product == null)
                return BadRequest("Sản phẩm không tồn tại.");

            if (product.Quantity < dto.Quantity)
                return BadRequest("Số lượng tồn kho không đủ.");

            var item = new OrderItem
            {
                OrderId = dto.OrderId,
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = dto.Quantity,
                UnitPrice = product.Price,
                TotalPrice = product.Price * dto.Quantity
            };

            order.TotalAmount += item.TotalPrice;

            _context.OrderItems.Add(item);

            await _context.SaveChangesAsync();

            return Ok(item);
        }

        // PUT: /order_items/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id, UpdateOrderItemDto dto)
        {
            var item = await _context.OrderItems.FindAsync(id);

            if (item == null)
                return NotFound("Không tìm thấy chi tiết đơn hàng.");

            if (dto.Quantity <= 0)
                return BadRequest("Số lượng phải lớn hơn 0.");

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
                return BadRequest("Sản phẩm không tồn tại.");

            if (product.Quantity < dto.Quantity)
                return BadRequest("Số lượng tồn kho không đủ.");

            var order = await _context.Orders.FindAsync(item.OrderId);

            if (order == null)
                return NotFound("Đơn hàng không tồn tại.");

            order.TotalAmount -= item.TotalPrice;

            item.Quantity = dto.Quantity;
            item.TotalPrice = item.UnitPrice * dto.Quantity;

            order.TotalAmount += item.TotalPrice;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(item);
        }

        // DELETE: /order_items/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.OrderItems.FindAsync(id);

            if (item == null)
                return NotFound("Không tìm thấy chi tiết đơn hàng.");

            var order = await _context.Orders.FindAsync(item.OrderId);

            if (order != null)
            {
                order.TotalAmount -= item.TotalPrice;
                order.UpdatedAt = DateTime.UtcNow;
            }

            _context.OrderItems.Remove(item);

            await _context.SaveChangesAsync();

            return Ok("Xóa chi tiết đơn hàng thành công.");
        }
    }
}
