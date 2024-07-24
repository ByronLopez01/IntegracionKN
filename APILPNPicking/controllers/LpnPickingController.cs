using APILPNPicking.data;
using APILPNPicking.models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;

namespace APILPNPicking.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LpnPickingController :ControllerBase
    {
        private readonly LPNPickingContext _context;
        private readonly HttpClient _httpClient;

        

        public LpnPickingController(LPNPickingContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
            //_httpClient.BaseAddress = new Uri("http://apiwaverelease:8080"); 

        }

        [HttpPost]
        public async Task<IActionResult> PostLpnPicking([FromBody] LPNPickingKN request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            var lpnSortings = new List<LPNSorting>();

            foreach (var loadDtlSeg in request.LoadHdrSeg.LoadDtlSeg)
            {
                foreach (var subnumSeg in loadDtlSeg.SubnumSeg)
                {
                    var lpnSorting = new LPNSorting
                    {
                        Wave = request.WcsId,
                        IdOrdenTrabajo = loadDtlSeg.Ordnum,
                        CodProducto = loadDtlSeg.Prtnum,
                        CantidadUnidades = loadDtlSeg.LodCasCnt,
                        DtlNumber = subnumSeg.Dtlnum
                    };

                    lpnSortings.Add(lpnSorting);
                }
            }

            _context.LPNSorting.AddRange(lpnSortings);
            await _context.SaveChangesAsync();

            return Ok("Datos guardados Correctamente.");
        }
    }
}
