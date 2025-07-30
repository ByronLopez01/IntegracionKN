using APIFamilyMaster.data;
using Microsoft.AspNetCore.Http.HttpResults;
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

        public async Task<int> ObtenerTotalSalidasAsync()
        {
            var totalSalidas = await _context.Familias
                .Select(f => f.NumSalida)
                .Distinct()
                .CountAsync();

            return totalSalidas;
        }


        public async Task<List<int>> ActivarTandasAsync(int salidasDisponibles)
        {
            await _context.Set<FamilyMaster>()
                .Where(f => f.estado == true)
                .ForEachAsync(f => f.estado = false);

            await _context.SaveChangesAsync();

            // Obtener la wave activa actual
            var waveActivaActual = await _context.WaveReleases
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.estadoWave == true);


            if (waveActivaActual == null)
            {
                throw new InvalidOperationException("Error. FamilyMaster no encontró una Wave activa.");
            }


            // Conociendo la wave activa, obtener las familias asociadas a esa wave
            var waveFamilies = await _context.WaveReleases
                .Where(w => w.Wave == waveActivaActual.Wave)
                .Select(w => w.Familia)
                .Distinct()
                .ToListAsync();

            var tandas = await _context.Set<FamilyMaster>()
                .OrderBy(f => f.NumSalida)
                .GroupBy(f => f.NumTanda)
                .Select(g => new
                {
                    NumTanda = g.Key,
                    Familia = g.First().Familia,
                    Salidas = g.Select(f => f.NumSalida).Distinct().ToList()
                })
                .ToListAsync();

            var tandasActivadas = new List<int>();
            var salidasRestantes = salidasDisponibles;
            var salidasUsadas = new HashSet<int>();

            foreach (var tanda in tandas)
            {
                if (!waveFamilies.Contains(tanda.Familia))
                    continue;

                // Verifica si alguna salida ya fue usada
                if (tanda.Salidas.Any(s => salidasUsadas.Contains(s)))
                    continue;

                if (tanda.Salidas.Count <= salidasRestantes)
                {
                    var registros = await _context.Set<FamilyMaster>()
                        .Where(f => f.NumTanda == tanda.NumTanda)
                        .ToListAsync();

                    foreach (var registro in registros)
                    {
                        registro.estado = true;
                        salidasUsadas.Add(registro.NumSalida); // Marcar salida como usada
                    }

                    tandasActivadas.Add(tanda.NumTanda);
                    salidasRestantes -= tanda.Salidas.Count;
                }

                if (salidasRestantes <= 0)
                    break;
            }

            await _context.SaveChangesAsync();
            return tandasActivadas;
        }


        public async Task<(int? NumTanda, string? Familia, string Message)> ActivarSiguienteTandaAsyncFamilyConfirm(int numTandaActual)
        {
            // Obtener todas las salidas de la tanda actual (ignorar el estado)
            var salidasTandaActual = await _context.Familias
                .Where(f => f.NumTanda == numTandaActual)
                .Select(f => f.NumSalida)
                .Distinct()
                .ToListAsync();

            if (!salidasTandaActual.Any())
            {
                return (null, null, $"La tanda {numTandaActual} no tiene salidas definidas.");
            }

            // Obtener todas las familias de la tanda actual
            var familiasActuales = await _context.Familias
                .Where(f => f.NumTanda == numTandaActual)
                .Select(f => f.Familia)
                .Distinct()
                .ToListAsync();

            // Verificar si todavía quedan salidas activas para esas familias en la tanda actual
            bool quedanSalidasActivas = await _context.Familias
                .AnyAsync(f => f.NumTanda == numTandaActual && f.estado == true && familiasActuales.Contains(f.Familia));

            if (quedanSalidasActivas)
            {
                return (null, null, "Aún quedan salidas activas en la tanda actual. No se puede activar la siguiente.");
            }

            // Desactivar todas las filas de la tanda actual
            await _context.Familias
                .Where(f => f.NumTanda == numTandaActual)
                .ExecuteUpdateAsync(s => s.SetProperty(f => f.estado, false));

            await _context.SaveChangesAsync();


            // Obtener la wave activa actual
            var waveActivaActual = await _context.WaveReleases
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.estadoWave == true);

            if (waveActivaActual == null)
            {
                return (null, null, "Error. FamilyMaster no encontró una Wave activa.");
            }

            // Obtener las familias asociadas a la Wave Activa
            var waveFamilies = await _context.WaveReleases
                .Where(w => w.Wave == waveActivaActual.Wave)
                .Select(w => w.Familia)
                .Distinct()
                .ToListAsync();

            if (!waveFamilies.Any())
            {
                return (null, null, $"Error. La Wave activa {waveActivaActual.Wave} no tiene familias asociadas.");
            }


            // Obtener siguientes tandas que aún están inactivas
            var siguientesTandas = await _context.Familias
                .Where(f => f.NumTanda > numTandaActual)
                .GroupBy(f => f.NumTanda)
                .Select(g => new
                {
                    NumTanda = g.Key,
                    Familias = g.Select(f => f.Familia).Distinct().ToList(),
                    Salidas = g.Select(f => f.NumSalida).Distinct().ToList()
                })
                .OrderBy(g => g.NumTanda)
                .ToListAsync();

            foreach (var tanda in siguientesTandas)
            {
                // Comparar salidas sin importar estado: deben ser las mismas
                var salidasSiguiente = tanda.Salidas;

                bool mismasSalidas = !salidasTandaActual.Except(salidasSiguiente).Any() &&
                                     !salidasSiguiente.Except(salidasTandaActual).Any();

                if (mismasSalidas)
                {
                    // Verificar que alguna familia de la tanda esté presente en la wave activa
                    bool familiaEnWaveActiva = tanda.Familias.Any(familia => waveFamilies.Contains(familia));

                    if (familiaEnWaveActiva)
                    {
                        // Activar tanda
                        await _context.Familias
                            .Where(f => f.NumTanda == tanda.NumTanda)
                            .ExecuteUpdateAsync(s => s.SetProperty(f => f.estado, true));

                        await _context.SaveChangesAsync();

                        return (tanda.NumTanda, tanda.Familias.FirstOrDefault(),
                                $"Se activó la tanda {tanda.NumTanda} correctamente.");
                    }
                }
            }

            return (null, null, $"No se encontró una tanda siguiente o ya fue activada: {numTandaActual}");
        }


        public async Task<(int? NumTanda, string? Familia, string Message)> ActivarSiguienteTandaAsync(int numTandaActual)
        {
            bool tandaActualInactiva = await _context.Familias
                .AnyAsync(f => f.NumTanda == numTandaActual && f.estado == false);

            if (tandaActualInactiva)
            {
                return (null, null, $"La tanda {numTandaActual} ya está inactiva, no se puede activar otra tanda.");
            }

            var salidasActuales = await _context.Familias
                .Where(f => f.NumTanda == numTandaActual)
                .Select(f => f.NumSalida)
                .Distinct()
                .ToListAsync();

            if (!salidasActuales.Any())
            {
                return (null, null, "No hay datos en la tanda actual.");
            }

            // Obtener la wave activa actual
            var waveActivaActual = await _context.WaveReleases
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.estadoWave == true);
            if (waveActivaActual == null)
            {
                return (null, null, "Error. FamilyMaster no encontró una Wave activa.");
            }

            // Obtener las familias asociadas a la Wave Activa
            var waveFamilies = await _context.WaveReleases
                .Where(w => w.Wave == waveActivaActual.Wave)
                .Select(w => w.Familia)
                .Distinct()
                .ToListAsync();
            if (!waveFamilies.Any())
            {
                return (null, null, $"Error. La Wave activa {waveActivaActual.Wave} no tiene familias asociadas.");
            }

            var siguientesTandas = await _context.Familias
                .Where(f => f.NumTanda > numTandaActual && f.estado == false)
                .GroupBy(f => f.NumTanda)
                .Select(g => new
                {
                    NumTanda = g.Key,
                    Familias = g.Select(f => f.Familia).Distinct().ToList(),
                    Salidas = g.Select(f => f.NumSalida).Distinct().ToList()
                })
                .OrderBy(g => g.NumTanda)
                .ToListAsync();

            foreach (var tanda in siguientesTandas)
            {
                if (!salidasActuales.Except(tanda.Salidas).Any() && !tanda.Salidas.Except(salidasActuales).Any())
                {
                    // Verificamos si alguna familia de la tanda está en la wave activa
                    bool familiaEnWaveActiva = tanda.Familias.Any(familia => waveFamilies.Contains(familia));

                    if (!familiaEnWaveActiva)
                    {
                        Console.WriteLine($"Familias de la tanda {tanda.NumTanda} no están en la Wave activa {waveActivaActual.Wave}. Se salta tanda.");
                        continue; // Sigue buscando la siguiente tanda
                    }

                    // Activar la tanda válida
                    await _context.Familias
                        .Where(f => f.NumTanda == tanda.NumTanda)
                        .ExecuteUpdateAsync(s => s.SetProperty(f => f.estado, true));

                    // Desactivar la tanda actual
                    await _context.Familias
                        .Where(f => f.NumTanda == numTandaActual)
                        .ExecuteUpdateAsync(s => s.SetProperty(f => f.estado, false));

                    await _context.SaveChangesAsync();

                    // También desactivar wave release de la familia anterior
                    var familiaActual = await _context.Familias
                        .Where(f => f.NumTanda == numTandaActual)
                        .Select(f => f.Familia)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(familiaActual))
                    {
                        await _context.WaveReleases
                            .Where(wr => wr.Familia == familiaActual && wr.estadoWave == true)
                            .ExecuteUpdateAsync(wr => wr.SetProperty(w => w.estadoWave, false));
                    }

                    return (tanda.NumTanda, tanda.Familias.FirstOrDefault(),
                        $"Se activó la tanda {tanda.NumTanda} y se desactivó la tanda {numTandaActual}.");
                }
            }

            return (null, null, "No se encontró una tanda siguiente válida con familias activas en WaveRelease.");
        }
    }
}
