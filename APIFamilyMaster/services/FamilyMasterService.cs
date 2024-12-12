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

        public async Task<int?> ActivarSiguienteTandaAsync(int numTandaActual)
        {
            // 1. Obtener las salidas asociadas a la tanda actual
            var salidasAsociadas = await _context.Set<FamilyMaster>()
                .Where(f => f.NumTanda == numTandaActual)
                .Select(f => f.NumSalida)
                .Distinct()
                .ToListAsync();

            if (!salidasAsociadas.Any())
            {
                // No hay salidas asociadas a la tanda actual
                return null;
            }

            // 2. Desactivar la tanda actual
            var tandaActual = await _context.Set<FamilyMaster>()
                .Where(f => f.NumTanda == numTandaActual)
                .ToListAsync();

            foreach (var registro in tandaActual)
            {
                registro.estado = false; // Desactivar la tanda actual
            }

            // 3. Buscar la siguiente tanda que use las mismas salidas
            var siguienteTanda = await _context.Set<FamilyMaster>()
                .Where(f => f.NumTanda > numTandaActual) // Solo tandas siguientes
                .GroupBy(f => f.NumTanda)
                .OrderBy(g => g.Key)
                .FirstOrDefaultAsync(g =>
                    g.Select(f => f.NumSalida).Distinct().All(salida => salidasAsociadas.Contains(salida))
                );

            if (siguienteTanda == null)
            {
                // No hay tanda siguiente que cumpla con las salidas
                await _context.SaveChangesAsync();
                return null;
            }

            // 4. Activar la siguiente tanda
            foreach (var registro in siguienteTanda)
            {
                registro.estado = true; // Activar el estado
            }

            // 5. Guardar los cambios y devolver la nueva tanda activada
            await _context.SaveChangesAsync();

            return siguienteTanda.Key; // Devuelve el número de la tanda activada
        }
    }
}
