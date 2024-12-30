using Microsoft.AspNetCore.Mvc;
using MACSAPI.Models;
using Microsoft.EntityFrameworkCore;
using MACSAPI.Data;

namespace MACSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HistoryCarController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HistoryCarController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<HistoryCar>>> GetHistoryCars()
        {
            return await _context.HistoryCar.ToListAsync();
        }

        [HttpGet("GetHistoryCarsIn")]
        public async Task<ActionResult<IEnumerable<HistoryCar>>> GetHistoryCarsIn()
        {
            var historyCars = await _context.HistoryCar
                                              .Where(h => h.IsGetIn == true && h.IsGetOut == false)
                                              .ToListAsync();


            return Ok(historyCars);
        }

        [HttpGet("byCardno/{cardNo}")]
        public async Task<ActionResult<List<HistoryCar>>> GetHistoryCarByCardNo(string cardNo)
        {
            if (string.IsNullOrWhiteSpace(cardNo))
            {
                return BadRequest("CardNo cannot be null or empty.");
            }

            var historyCars = await _context.HistoryCar
                                             .Where(h => h.CardNo == cardNo)
                                             .ToListAsync();

            if (historyCars == null || !historyCars.Any())
            {
                return NotFound($"No HistoryCar records found with CardNo '{cardNo}'.");
            }

            return Ok(historyCars);
        }


        [HttpPost]
        public async Task<ActionResult<HistoryCar>> PostHistoryCar(HistoryCar historyCar)
        {
            // Generate a new VehicleLogId
            historyCar.VehicleLogId = Guid.NewGuid();
            historyCar.CreatedDate = DateTime.UtcNow.AddHours(7);
            historyCar.ModifiedDate = DateTime.UtcNow.AddHours(7);

            _context.HistoryCar.Add(historyCar);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHistoryCarByCardNo), new { cardNo = historyCar.CardNo }, historyCar);
        }

        [HttpPut("edit/{cardNo}")]
        public async Task<IActionResult> PutHistoryCar(string cardNo, HistoryCar updatedHistoryCar)
        {
            if (cardNo != updatedHistoryCar.CardNo)
            {
                return BadRequest("CardNo in the URL does not match the CardNo in the body.");
            }

            var existingHistoryCar = await _context.HistoryCar
                                                   .FirstOrDefaultAsync(h => h.CardNo == cardNo && !h.IsGetOut);

            if (existingHistoryCar == null)
            {
                return NotFound($"No HistoryCar with CardNo '{cardNo}' and IsGetOut = false found.");
            }

            // Cập nhật các trường cần thiết
            existingHistoryCar.DriverName = updatedHistoryCar.DriverName;
            existingHistoryCar.PersonalId = updatedHistoryCar.PersonalId;
            existingHistoryCar.PhoneNo = updatedHistoryCar.PhoneNo;
            existingHistoryCar.LicensePlate = updatedHistoryCar.LicensePlate;
            existingHistoryCar.VehicleType = updatedHistoryCar.VehicleType;
            existingHistoryCar.TypeCode = updatedHistoryCar.TypeCode;
            existingHistoryCar.Purpose = updatedHistoryCar.Purpose;
            existingHistoryCar.Remark = updatedHistoryCar.Remark;
            existingHistoryCar.IsCarried = updatedHistoryCar.IsCarried;
            existingHistoryCar.IsCarriedIn = updatedHistoryCar.IsCarriedIn;
            existingHistoryCar.IsGetIn = updatedHistoryCar.IsGetIn;
            existingHistoryCar.GetInDate = updatedHistoryCar.GetInDate;
            existingHistoryCar.GetInBy = updatedHistoryCar.GetInBy;
            existingHistoryCar.IsCarriedOut = updatedHistoryCar.IsCarriedOut;
            existingHistoryCar.IsGetOut = updatedHistoryCar.IsGetOut;
            existingHistoryCar.GetOutDate = updatedHistoryCar.GetOutDate;
            existingHistoryCar.GetOutBy = updatedHistoryCar.GetOutBy;
            existingHistoryCar.ModifiedDate = DateTime.UtcNow.AddHours(7);
            existingHistoryCar.ModifiedBy = updatedHistoryCar.ModifiedBy;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HistoryCarExists(cardNo))
                {
                    return NotFound($"HistoryCar with CardNo '{cardNo}' no longer exists.");
                }
                else
                {
                    throw;
                }
            }

            return Ok(existingHistoryCar);
        }


        [HttpDelete("{cardNo}")]
        public async Task<IActionResult> DeleteHistoryCar(string cardNo)
        {
            var historyCar = await _context.HistoryCar.FirstOrDefaultAsync(h => h.CardNo == cardNo);
            if (historyCar == null)
            {
                return NotFound($"HistoryCar with CardNo '{cardNo}' not found.");
            }

            _context.HistoryCar.Remove(historyCar);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool HistoryCarExists(string cardNo)
        {
            return _context.HistoryCar.Any(e => e.CardNo == cardNo);
        }
    }
}
