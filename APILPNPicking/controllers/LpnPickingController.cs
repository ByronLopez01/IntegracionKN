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
            if (request?.SORT_INDUCTION?.LOAD_HDR_SEG?.LOAD_DTL_SEG == null || string.IsNullOrEmpty(request.SORT_INDUCTION.wcs_id))
            {
                return BadRequest("Datos en formato incorrecto.");
            }

            var lpnSortings = new List<LPNSorting>();

            var loadHdrSeg = request.SORT_INDUCTION.LOAD_HDR_SEG;
            foreach (var loadDtlSeg in loadHdrSeg.LOAD_DTL_SEG)
            {
                foreach (var subnumSeg in loadDtlSeg.SUBNUM_SEG)
                {
                    var lpnSorting = new LPNSorting
                    {
                        Wave = request.SORT_INDUCTION.wcs_id,
                        IdOrdenTrabajo = loadDtlSeg.ordnum,
                        CodProducto = loadDtlSeg.prtnum,
                        CantidadUnidades = loadDtlSeg.lod_cas_cnt,
                        DtlNumber = subnumSeg.dtlnum
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
