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
            if (waveKn?.ORDER_TRANSMISSION?.ORDER_TRANS_SEG?.ORDER_SEG == null || string.IsNullOrEmpty(waveKn.ORDER_TRANSMISSION.wcs_id))
            {
                return BadRequest("Datos en formato no válido.");
            }

            var waveReleases = new List<WaveRelease>();

            // Itera sobre cada ORDER_SEG en la lista
            foreach (var orderSeg in waveKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.ORDER_SEG)
            {
                if (orderSeg?.SHIP_SEG?.PICK_DTL_SEG == null)
                {
                    return BadRequest("El PICK_DTL_SEG viene null");
                }

                foreach (var pickDtlSeg in orderSeg.SHIP_SEG.PICK_DTL_SEG)
                {
                    var waveRelease = new WaveRelease
                    {
                        CodMastr = pickDtlSeg.mscs_ean,
                        CodInr = pickDtlSeg.incs_ean,
                        CantMastr = pickDtlSeg.qty_mscs,
                        CantInr = pickDtlSeg.qty_incs,
                        Cantidad = pickDtlSeg.qty,
                        Familia = pickDtlSeg.prtfam,
                        NumOrden = orderSeg.ordnum,
                        CodProducto = pickDtlSeg.prtnum,
                        Wave = waveKn.ORDER_TRANSMISSION.wcs_id,
                        tienda = orderSeg.rtcust
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
                return NotFound($"Orden no registrada en la wave {idOrdenTrabajo}");
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
