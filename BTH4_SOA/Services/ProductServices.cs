using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BTH4_SOA.Services
{
    public class ProductService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public ProductService(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        private string GenerateInternalToken()
        {
            var key = _configuration["Jwt:Key"] ?? "ThisIsMyVerySecretKeyForJwtAuthentication123456";
            var issuer = _configuration["Jwt:Issuer"] ?? "JwtApi";
            var audience = _configuration["Jwt:Audience"] ?? "JwtApiUsers";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "BTH4_SOA_Service"),
                new Claim(ClaimTypes.Role, "Service")
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<ProductDto?> GetProduct(int id)
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

            // Nếu người dùng test trực tiếp trên Swagger chưa gắn Token,
            // tự động cấp Token nội bộ để gọi thành công sang ProductManagementAPI
            if (string.IsNullOrWhiteSpace(authHeader))
            {
                authHeader = "Bearer " + GenerateInternalToken();
            }

            try
            {
                // 1. Thử gọi route chuẩn /products/{id}
                var request = new HttpRequestMessage(HttpMethod.Get, $"products/{id}");
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);

                var response = await _httpClient.SendAsync(request);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // Thử route dự phòng /api/products/{id}
                    var fallbackReq = new HttpRequestMessage(HttpMethod.Get, $"api/products/{id}");
                    fallbackReq.Headers.TryAddWithoutValidation("Authorization", authHeader);

                    var fallbackRes = await _httpClient.SendAsync(fallbackReq);
                    if (fallbackRes.StatusCode == HttpStatusCode.NotFound)
                        return null;

                    if (fallbackRes.IsSuccessStatusCode)
                        return await fallbackRes.Content.ReadFromJsonAsync<ProductDto>();
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<ProductDto>();
            }
            catch (HttpRequestException ex)
            {
                // Dự phòng nếu ProductManagementAPI đang chạy trên cổng HTTP (5245) thay vì HTTPS (7241)
                try
                {
                    using var fallbackClient = new HttpClient(new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    })
                    {
                        BaseAddress = new Uri("http://localhost:5245/")
                    };

                    var fallbackReq = new HttpRequestMessage(HttpMethod.Get, $"products/{id}");
                    fallbackReq.Headers.TryAddWithoutValidation("Authorization", authHeader);

                    var fallbackRes = await fallbackClient.SendAsync(fallbackReq);
                    if (fallbackRes.IsSuccessStatusCode)
                    {
                        return await fallbackRes.Content.ReadFromJsonAsync<ProductDto>();
                    }
                }
                catch
                {
                    // Bỏ qua lỗi fallback
                }

                throw new InvalidOperationException(
                    "Không thể kết nối đến ProductManagementAPI (cổng 7241 hoặc 5245). " +
                    "Vui lòng đảm bảo rằng bạn đã khởi chạy dự án ProductManagementAPI!", ex);
            }
        }
    }

    public class ProductDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public decimal Price { get; set; }

        public int Quantity { get; set; }
    }
}
