using APIFamilyMaster.data;
using Microsoft.AspNetCore.Mvc;


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
    }
}
