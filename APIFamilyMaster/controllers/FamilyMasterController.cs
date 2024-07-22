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
                    NumTanda = dto.NumTanda
                };

                familyMasterEntities.Add(familyMaster);
            }

            _context.Familias.AddRange(familyMasterEntities);
            await _context.SaveChangesAsync();

            return Ok("Datos guardados correctamente.");
        }
    }
}
