using APIFamilyMaster.data;
using Microsoft.EntityFrameworkCore;

namespace APIFamilyMaster.services
{
    public class FamilyMasterService
    {
        private readonly FamilyMasterContext _context;


        public FamilyMasterService(FamilyMasterContext context)
        {
            _context = context;

        }

        public async Task<List<int>> ActivarTandasAsync(int salidasDisponibles)
        {
            // Obtener todas las tandas ordenadas por número de tanda
            var tandas = await _context.Set<FamilyMaster>()
                .GroupBy(f => f.NumTanda)
                .Select(g => new
                {
                    NumTanda = g.Key,
                    SalidasRequeridas = g.Select(f => f.NumSalida).Distinct().Count()
                })
                .OrderBy(t => t.NumTanda)
                .ToListAsync();

            var tandasActivadas = new List<int>();
            var salidasRestantes = salidasDisponibles;

            foreach (var tanda in tandas)
            {
                if (tanda.SalidasRequeridas <= salidasRestantes)
                {
                    // Activar la tanda
                    var registros = await _context.Set<FamilyMaster>()
                        .Where(f => f.NumTanda == tanda.NumTanda)
                        .ToListAsync();

                    foreach (var registro in registros)
                    {
                        registro.estado = true;
                    }

                    tandasActivadas.Add(tanda.NumTanda);
                    salidasRestantes -= tanda.SalidasRequeridas;
                }
                else
                {
                    break; // No hay más salidas disponibles
                }
            }

            await _context.SaveChangesAsync();
            return tandasActivadas;
        }
    }
}
