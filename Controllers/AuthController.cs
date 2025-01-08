using MACSAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

namespace MACSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly List<UserAccount> _users;
        private readonly List<UserGroup> _userGroups;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;

            // Khởi tạo danh sách user
            _users = new List<UserAccount>
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
                    Role = "admin",
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
                    Admin = true,
                    Role = "admin",
                    Quote = "Strive for greatness!",
                    IsWebApp = false
                },
                new UserAccount
                {
                    AccountId = 3,
                    EmployeeId = 789,
                    Username = "sto",
                    Password = HashPassword("123"),
                    FullName = "Store",
                    IsActivated = true,
                    Admin = false,
                    Role = "store",
                    Quote = "Strive for greatness!",
                    IsWebApp = false
                },
                new UserAccount
                {
                    AccountId = 4,
                    EmployeeId = 789,
                    Username = "user",
                    Password = HashPassword("123"),
                    FullName = "user",
                    IsActivated = true,
                    Admin = false,
                    Role = "user",
                    Quote = "Strive for greatness!",
                    IsWebApp = false
                }
            };

            // Khởi tạo danh sách nhóm trong constructor
            _userGroups = new List<UserGroup>
            {
                new UserGroup
                {
                    GroupId = 1,
                    GroupName = "Nhóm Admin",
                    Description = "Group for system administrators.",
                    Members = new List<UserAccount>
                    {
                        _users.First(u => u.Username == "admin"),
                        _users.First(u => u.Username == "admin2")
                    }
                },
                new UserGroup
                {
                    GroupId = 2,
                    GroupName = "Nhóm kho",
                    Description = "Group for store managers.",
                    Members = new List<UserAccount>
                    {
                        _users.First(u => u.Username == "sto")
                    }
                },
                new UserGroup
                {
                    GroupId = 3,
                    GroupName = "Nhóm người dùng",
                    Description = "Group for regular users.",
                    Members = new List<UserAccount>
                    {
                        _users.First(u => u.Username == "user")
                    }
                }
            };
        }

        [HttpGet("GetAllUsers")]
        public ActionResult<IEnumerable<UserAccount>> GetAllUsers()
        {
            return Ok(_users);
        }
        [HttpGet("GetGroupUsers")]
        public ActionResult<IEnumerable<UserAccount>> GetGroupUsers()
        {
            return Ok(_userGroups);
        }

        [HttpGet("GetUserById/{userId}")]
        public IActionResult GetUserById(int userId)
        {
            // Tìm user trong danh sách theo AccountId
            var user = _users.FirstOrDefault(u => u.AccountId == userId);

            if (user == null)
                return NotFound(new { Message = "User not found" });

            return Ok(user);  // Trả về thông tin user nếu tìm thấy
        }

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
                        new Claim("Admin", account.Admin.ToString()),
                        new Claim("role", account.Role)
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
