
using MACSAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace MACSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private static List<TokenRequest> _tokens = new List<TokenRequest>();

        [HttpGet]
        public ActionResult<IEnumerable<TokenRequest>> GetAllTokens()
        {
            return Ok(_tokens);
        }

        [HttpGet("{id}")]
        public ActionResult<TokenRequest> GetTokenById(int id)
        {
            var token = _tokens.FirstOrDefault(t => t.Id == id);
            if (token == null)
            {
                return NotFound($"Token with Id {id} not found.");
            }
            return Ok(token);
        }

        [HttpPost]
        public ActionResult<TokenRequest> CreateToken([FromBody] TokenRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Token))
            {
                return BadRequest("Invalid token data.");
            }

            _tokens.Add(request);

            return CreatedAtAction(nameof(GetTokenById), new { id = request.Id }, request);
        }

        [HttpPut("{id}")]
        public ActionResult UpdateToken(int id, [FromBody] TokenRequest request)
        {
            var existingToken = _tokens.FirstOrDefault(t => t.Id == id);
            if (existingToken == null)
            {
                return NotFound($"Token with Id {id} not found.");
            }

            if (string.IsNullOrEmpty(request.Token))
            {
                return BadRequest("Token value cannot be null or empty.");
            }

            existingToken.Token = request.Token;
            existingToken.Role = request.Role;

            return NoContent();
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteToken(int id)
        {
            var token = _tokens.FirstOrDefault(t => t.Id == id);
            if (token == null)
            {
                return NotFound($"Token with Id {id} not found.");
            }

            _tokens.Remove(token);
            return NoContent();
        }
    }
}
