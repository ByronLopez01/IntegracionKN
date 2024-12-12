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


        public async Task<List<int>> ActivarSiguientesTandasAsync(int salidasDisponibles)
        {
            // Desactivar todas las tandas activas en el momento
            var tandasActivas = await _context.Set<FamilyMaster>()
                .Where(f => f.estado == true)
                .ToListAsync();

            foreach (var tandaActiva in tandasActivas)
            {
                tandaActiva.estado = false; // Desactivar las tandas activas
            }

            // Ahora, obtener las tandas en orden ascendente de NumTanda
            var tandas = await _context.Set<FamilyMaster>()
                .GroupBy(f => f.NumTanda)
                .OrderBy(t => t.Key)
                .ToListAsync();

            var tandasActivadas = new List<int>();
            var salidasRestantes = salidasDisponibles;

            // Activar solo la siguiente tanda que se pueda activar
            foreach (var tanda in tandas)
            {
                var salidasRequeridas = tanda.Select(f => f.NumSalida).Distinct().Count();
                if (salidasRequeridas <= salidasRestantes)
                {
                    // Activar la siguiente tanda
                    var registros = await _context.Set<FamilyMaster>()
                        .Where(f => f.NumTanda == tanda.Key)
                        .ToListAsync();

                    foreach (var registro in registros)
                    {
                        registro.estado = true; // Activar el estado de la tanda
                    }

                    tandasActivadas.Add(tanda.Key);
                    salidasRestantes -= salidasRequeridas;

                    break; // Solo activamos una tanda, luego salimos del bucle
                }
                else
                {
                    break; // No hay suficientes salidas disponibles para esta tanda, salimos
                }
            }

            await _context.SaveChangesAsync();
            return tandasActivadas;
        }

    }
}
