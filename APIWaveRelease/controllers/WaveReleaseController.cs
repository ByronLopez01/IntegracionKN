using APIWaveRelease.data;
using APIWaveRelease.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace APIWaveRelease.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WaveReleaseController : ControllerBase
    {
        private readonly WaveReleaseContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public WaveReleaseController(WaveReleaseContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> PostOrderTransmission([FromBody] WaveReleaseKN waveKn)
        {
            if (waveKn?.ORDER_TRANSMISSION?.ORDER_TRANS_SEG?.ORDER_SEG == null || string.IsNullOrEmpty(waveKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.schbat))
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
                    // Busca si ya existe un WaveRelease con el mismo número de orden y producto
                    var existingWaveRelease = waveReleases
                        .FirstOrDefault(wr => wr.NumOrden == orderSeg.ordnum && wr.CodProducto == pickDtlSeg.prtnum);

                    if (existingWaveRelease != null)
                    {
                       
                       // existingWaveRelease.CantMastr = pickDtlSeg.qty_mscs;
                        //existingWaveRelease.CantInr = pickDtlSeg.qty_incs;
                        existingWaveRelease.Cantidad += pickDtlSeg.qty;

                        // Mensaje de depuración para indicar que se ha encontrado y actualizado un registro existente
                        Debug.WriteLine($"Cantidad actualizada para Orden: {orderSeg.ordnum}, Producto: {pickDtlSeg.prtnum}");
                    }
                    else
                    {
                        // Si no existe, crea un nuevo registro
                        var newWaveRelease = new WaveRelease
                        {
                            CodMastr = pickDtlSeg.mscs_ean,
                            CodInr = pickDtlSeg.incs_ean,
                            CantMastr = pickDtlSeg.qty_mscs,
                            CantInr = pickDtlSeg.qty_incs,
                            Cantidad = pickDtlSeg.qty,
                            Familia = pickDtlSeg.prtfam,
                            NumOrden = orderSeg.ordnum,
                            CodProducto = pickDtlSeg.prtnum,
                            Wave = waveKn.ORDER_TRANSMISSION.ORDER_TRANS_SEG.schbat,
                            tienda = orderSeg.rtcust
                        };
                        
                        waveReleases.Add(newWaveRelease);

                        // Mensaje de depuración para indicar que se ha creado un nuevo registro
                        Debug.WriteLine($"Nuevo registro creado para Orden: {orderSeg.ordnum}, Producto: {pickDtlSeg.prtnum}");
                    }
                }
            }

            _context.WaveRelease.AddRange(waveReleases);
            _context.SaveChanges();

            //enviar el JSON a Luca parametrizado 
            var urlLuca = _configuration["ServiceUrls:luca"];
            //Console.WriteLine(urlLuca);
            //Console.WriteLine(urlLuca);
            //Console.WriteLine(urlLuca);
            //Console.WriteLine(urlLuca);
            //Console.WriteLine(urlLuca);
            //Console.WriteLine(urlLuca);
            //enviando json a luca tal cual lo recibimos desde kn
            var jsonContent = JsonSerializer.Serialize(waveKn);
            var httpClient = _httpClientFactory.CreateClient();

            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(urlLuca, httpContent);

            if (response.IsSuccessStatusCode)
            {
                
                Debug.WriteLine("JSON enviado a Luca exitosamente.");
            }
            else
            {
                
                Debug.WriteLine($"Error al enviar JSON a Luca. Status code: {response.StatusCode}");
            }

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
