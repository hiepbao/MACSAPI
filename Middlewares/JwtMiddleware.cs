using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MACSAPI.Middlewares
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _key;

        public JwtMiddleware(RequestDelegate next, string key)
        {
            _next = next;
            _key = key;
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var keyBytes = Encoding.UTF8.GetBytes(_key);

                    tokenHandler.ValidateToken(token, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidIssuer = "YourIssuer",
                        ValidAudience = "YourAudience"
                    }, out SecurityToken validatedToken);

                    var jwtToken = (JwtSecurityToken)validatedToken;

                    // Gán các claim từ token vào HttpContext.User
                    var claims = jwtToken.Claims;
                    var identity = new ClaimsIdentity(claims, "jwt");
                    context.User = new ClaimsPrincipal(identity);
                }
                catch (Exception ex)
                {
                    // Log lỗi nếu token không hợp lệ
                    context.User = new ClaimsPrincipal(); // Đặt User thành rỗng nếu token sai
                    Console.WriteLine($"JWT Validation Failed: {ex.Message}");
                }
            }

            await _next(context);
        }
    }

}