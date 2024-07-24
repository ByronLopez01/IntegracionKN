using APIWaveRelease.data;
using APIWaveRelease.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace APIWaveRelease.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WaveReleaseController : ControllerBase
    {
        private readonly WaveReleaseContext _context;

        public WaveReleaseController(WaveReleaseContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult PostOrderTransmission([FromBody] WaveReleaseKN waveKn)
        {
            if (waveKn?.OrderTransSeg?.OrderSeg == null || string.IsNullOrEmpty(waveKn.WcsId))
            {
                return BadRequest("Invalid data format.");
            }

            var waveReleases = new List<data.WaveRelease>();

            foreach (var orderSeg in waveKn.OrderTransSeg.OrderSeg)
            {
                if (orderSeg?.ShipSeg?.PickDtlSeg == null)
                {
                    continue;  // Skip orders without picking details
                }

                foreach (var pickDtlSeg in orderSeg.ShipSeg.PickDtlSeg)
                {
                    var waveRelease = new data.WaveRelease
                    {
                        CodMastr = pickDtlSeg.MscsEan,
                        CodInr = pickDtlSeg.IncsEan,
                        CantMastr = pickDtlSeg.QtyMscs,
                        CantInr = pickDtlSeg.QtyIncs,
                        Cantidad = pickDtlSeg.Qty,
                        Familia = pickDtlSeg.Prtfam,
                        NumOrden = orderSeg.Ordnum,
                        CodProducto = pickDtlSeg.Prtnum,
                        Wave = waveKn.OrderTransSeg.Schbat
                    };

                    waveReleases.Add(waveRelease);
                }
            }

            _context.WaveRelease.AddRange(waveReleases);
            _context.SaveChanges();
            return Ok();
        }

        [HttpGet("{idOrdenTrabajo}")]
        public async Task<IActionResult> GetWaveByIdOrdenTrabajo(string idOrdenTrabajo)
        {
            var waveReleases = await _context.WaveRelease
                .Where(w => w.NumOrden == idOrdenTrabajo)
                .ToListAsync();

            if (waveReleases == null || waveReleases.Count == 0)
            {
                return NotFound($"No Wave found for Order ID {idOrdenTrabajo}");
            }

            return Ok(waveReleases);
        }

        // PUT api/<WaveReleaseController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<WaveReleaseController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
