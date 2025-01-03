﻿using MACSAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace MACSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private readonly List<UserAccount> _users = new()
        {
            new UserAccount
            {
                AccountId = 1,
                EmployeeId = 123,
                Username = "admin",
                Password = HashPassword("123"),
                FullName = "Admin User",
                IsActivated = true,
                Admin = true,
                Quote = "Welcome to the system!",
                IsWebApp = true
            },
            new UserAccount
            {
                AccountId = 2,
                EmployeeId = 456,
                Username = "admin2",
                Password = HashPassword("123"),
                FullName = "John Doe",
                IsActivated = true,
                Admin = false,
                Quote = "Strive for greatness!",
                IsWebApp = false
            }
        };


        [HttpGet("login")]
        public IActionResult Login([FromQuery] string user, [FromQuery] string pass)
        {
            try
            {
                var hashedPassword = HashPassword(pass);

                var account = _users.FirstOrDefault(u =>
                    u.Username.Equals(user, StringComparison.OrdinalIgnoreCase) &&
                    u.Password == hashedPassword);

                if (account == null)
                {
                    return Unauthorized(new { message = "Username hoặc password không đúng" });
                }

                // Lấy khóa bí mật từ cấu hình
                var key = _configuration["JwtSettings:Key"];
                if (string.IsNullOrEmpty(key) || key.Length < 32)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new
                    {
                        message = "Khóa bí mật không hợp lệ. Vui lòng kiểm tra cấu hình JWT."
                    });
                }

                var issuer = _configuration["JwtSettings:Issuer"];
                var audience = _configuration["JwtSettings:Audience"];

                // Tạo JWT token
                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim("AccountId", account.AccountId.ToString()),
                        new Claim("Username", account.Username),
                        new Claim("FullName", account.FullName),
                        new Claim("Admin", account.Admin.ToString())
                    }),
                    Expires = DateTime.UtcNow.AddHours(1), // Thời gian sống của token
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = issuer,
                    Audience = audience
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwt = tokenHandler.WriteToken(token);

                return Ok(new { token = jwt });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Có lỗi xảy ra trong quá trình xử lý yêu cầu",
                    error = ex.Message
                });
            }
        }


        public static string HashPassword(string password)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = md5.ComputeHash(inputBytes);

                // Chuyển đổi mảng byte thành chuỗi hex
                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("X2")); // Hexadecimal format
                }
                return sb.ToString();
            }
        }
    }
}
