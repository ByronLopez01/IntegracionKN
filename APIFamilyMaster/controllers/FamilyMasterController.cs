using APIFamilyMaster.data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace APIFamilyMaster.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FamilyMasterController :ControllerBase
    {
        private readonly FamilyMasterContext _context;

        public FamilyMasterController(FamilyMasterContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> PostFamilyMaster([FromBody] List<FamilyMaster> familyMasters)
        {
            if (familyMasters == null || familyMasters.Count == 0)
            {
                return BadRequest("Datos invalidos.");
            }

            var familyMasterEntities = new List<FamilyMaster>();

            foreach (var dto in familyMasters)
            {
                var familyMaster = new FamilyMaster
                {
                    Familia = dto.Familia,
                    NumSalida = dto.NumSalida,
                    NumTanda = dto.NumTanda,
                    Tienda1 = dto.Tienda1,
                    Tienda2 = dto.Tienda2,
                    Tienda3 = dto.Tienda3,
                    Tienda4 = dto.Tienda4,
                    Tienda5 = dto.Tienda5,
                    Tienda6 = dto.Tienda6,
                    Tienda7 = dto.Tienda7,
                    Tienda8 = dto.Tienda8,
                    Tienda9 = dto.Tienda9,
                    Tienda10 = dto.Tienda10
                    
                };

                familyMasterEntities.Add(familyMaster);
            }

            _context.Familias.AddRange(familyMasterEntities);
            await _context.SaveChangesAsync();

            return Ok("Datos guardados correctamente.");
        }

        // GET: api/FamilyMaster
        [HttpGet]
        public async Task<IActionResult> GetFamilyMasters([FromQuery] string tienda)
        {
            if (string.IsNullOrEmpty(tienda))
            {
                return BadRequest("El parámetro de búsqueda 'tienda' no puede ser nulo o vacío.");
            }

            var familyMasters = await _context.Familias
                .Where(f => f.Tienda1 == tienda ||
                            f.Tienda2 == tienda ||
                            f.Tienda3 == tienda ||
                            f.Tienda4 == tienda ||
                            f.Tienda5 == tienda ||
                            f.Tienda6 == tienda ||
                            f.Tienda7 == tienda ||
                            f.Tienda8 == tienda ||
                            f.Tienda9 == tienda ||
                            f.Tienda10 == tienda)
                .Select(f => new
                {
                    f.IdFamilyMaster,
                    f.Familia,
                    f.NumSalida,
                    f.NumTanda,
                    TiendaConsultada = tienda
                })
                .ToListAsync();

            if (familyMasters == null || !familyMasters.Any())
            {
                return NotFound("No se encontraron datos para la tienda proporcionada.");
            }

            return Ok(familyMasters);
        }

        // GET: api/FamilyMaster/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFamilyMasterById(int id)
        {
            var familyMaster = await _context.Familias.FindAsync(id);

            if (familyMaster == null)
            {
                return NotFound("No se encontró el dato con el id proporcionado.");
            }

            return Ok(familyMaster);
        }
    }
}
