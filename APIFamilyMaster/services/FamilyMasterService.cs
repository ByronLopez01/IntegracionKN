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
        /*
                public async Task<List<int>> ActivarTandasAsync(int salidasDisponibles)
                {
                    await _context.Set<FamilyMaster>()
                    .Where(f => f.estado == true)
                    .ForEachAsync(f => f.estado = false);

                    await _context.SaveChangesAsync();

                    var waveFamilies = await _context.WaveReleases
                        .Select(w => w.Familia)
                        .Distinct()
                        .ToListAsync();


                    var tandas = await _context.Set<FamilyMaster>()
                        .OrderBy(f => f.NumSalida)
                        .GroupBy(f => f.NumTanda)
                        .Select(g => new
                        {
                            NumTanda = g.Key,
                            familia = g.First().Familia, 
                            SalidasRequeridas = g.Select(f => f.NumSalida).Distinct().Count()
                        })
                        .ToListAsync();

                    var tandasActivadas = new List<int>();
                    var salidasRestantes = salidasDisponibles;

                    foreach (var tanda in tandas)
                    {

                        if (!waveFamilies.Contains(tanda.familia))
                        {
                            continue; 
                        }


                        if (tanda.SalidasRequeridas <= salidasRestantes)
                        {
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


                        if (salidasRestantes <= 0)
                        {
                            break;
                        }
                    }

                    await _context.SaveChangesAsync();
                    return tandasActivadas;
                }

                */


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


        /*   public async Task<List<int>> ActivarTandasAsync(int salidasDisponibles)
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
           }*/

        //test
        /*  public async Task<int?> ActivarSiguienteTandaAsync(int numTandaActual)
          {
              // Obtener las salidas asociadas a la tanda actual
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

              // Desactivar la tanda actual
              var tandaActual = await _context.Set<FamilyMaster>()
                  .Where(f => f.NumTanda == numTandaActual)
                  .ToListAsync();

              foreach (var registro in tandaActual)
              {
                  registro.estado = false; // Desactivar la tanda actual
              }

              // Obtener todas las posibles tandas posteriores
              var posiblesTandas = await _context.Set<FamilyMaster>()
                  .Where(f => f.NumTanda > numTandaActual) // Solo tandas posteriores
                  .ToListAsync(); // Trae todas las tandas siguientes

              // Buscar la siguiente tanda con todas las salidas
              var siguienteTanda = posiblesTandas
                  .GroupBy(f => f.NumTanda) // Agrupa por número de tanda
                  .OrderBy(g => g.Key) // Ordena las tandas por número
                  .FirstOrDefault(g =>
                      g.Select(f => f.NumSalida).Distinct() // Selecciona las salidas de cada tanda
                      .All(salida => salidasAsociadas.Contains(salida))); // Verifica si todas las salidas coinciden

              if (siguienteTanda == null)
              {

                  var primerasTandas = await _context.Set<FamilyMaster>()
                      .Where(f => f.NumTanda > 0) 
                      .ToListAsync(); 

                  siguienteTanda = primerasTandas
                      .GroupBy(f => f.NumTanda) 
                      .OrderBy(g => g.Key) 
                      .FirstOrDefault(g =>
                          g.Select(f => f.NumSalida).Distinct() 
                          .All(salida => salidasAsociadas.Contains(salida))); 
              }

              if (siguienteTanda == null)
              {

                  await _context.SaveChangesAsync();
                  return null;
              }


              foreach (var registro in siguienteTanda)
              {
                  registro.estado = true; 
              }

              await _context.SaveChangesAsync();

              return siguienteTanda.Key; 
          }
        */

        /*

        public async Task<int?> ActivarSiguienteTandaAsync(int numTandaActual)
        {
            // Obtener las salidas asociadas a la tanda actual
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

            // Desactivar la tanda actual
            var tandaActual = await _context.Set<FamilyMaster>()
                .Where(f => f.NumTanda == numTandaActual)
                .ToListAsync();

            foreach (var registro in tandaActual)
            {
                registro.estado = false; // Desactivar la tanda actual
            }

            // Obtener todas las posibles tandas siguientes
            var posiblesTandas = await _context.Set<FamilyMaster>()
                .Where(f => f.NumTanda > numTandaActual) // Solo tandas posteriores
                .ToListAsync(); // Trae todas las tandas siguientes

            


            // Buscar la siguiente tanda con todas las salidas
            var siguienteTanda = posiblesTandas
                .GroupBy(f => f.NumTanda) // Agrupa por número de tanda
                .OrderBy(g => g.Key) // Ordena las tandas por número
                .FirstOrDefault(g =>
                    g.Select(f => f.NumSalida).Distinct() // Selecciona las salidas de cada tanda
                    .All(salida => salidasAsociadas.Contains(salida))); // Verifica si todas las salidas coinciden

            if (siguienteTanda == null)
            {
                // No hay tanda siguiente que cumpla con las salidas
                await _context.SaveChangesAsync();
                return null;
            }

            // Activar la siguiente tanda
            foreach (var registro in siguienteTanda)
            {
                registro.estado = true; // Activar la tanda siguiente
            }

         
            await _context.SaveChangesAsync();

            return siguienteTanda.Key; // Devuelve la tanda activada
        }
        */


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
                    // Verificar que alguna familia esté presente en WaveRelease
                    bool familiaEnWaveRelease = await _context.WaveReleases
                        .AnyAsync(wr => tanda.Familias.Contains(wr.Familia));

                    if (familiaEnWaveRelease)
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







        /*  public async Task<(int? NumTanda, string? Familia, string Message)> ActivarSiguienteTandaAsync(int numTandaActual)
          {

              bool tandaActualInactiva = await _context.Familias
                  .AnyAsync(f => f.NumTanda == numTandaActual && f.estado == false);

              if (tandaActualInactiva)
              {
                  return (null, null, $"La tanda {numTandaActual} ya está inactiva, no se puede activar otra tanda.");
              }

              // Obtener las salidas de la tanda actual
              var salidasActuales = await _context.Familias
                  .Where(f => f.NumTanda == numTandaActual)
                  .Select(f => f.NumSalida)
                  .Distinct()
                  .ToListAsync();

              if (!salidasActuales.Any())
              {
                  return (null, null, "No hay datos en la tanda actual.");
              }

              // Desactivar la tanda actual
              await _context.Familias
                 .Where(f => f.NumTanda == numTandaActual)
                 .ExecuteUpdateAsync(s => s.SetProperty(f => f.estado, false));

              await _context.SaveChangesAsync(); // Guardar cambios

              // !!!! Se desactivan los registros activos en WaveRelease para la familia de la tanda actual !!!!
              // Se obtiene la familia a partir de la tanda actual
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
              // FIN DE DESACTIVACIÓN DE REGISTROS EN WaveRelease


              // Obtener las siguientes tandas inactivas (Estado == 0)
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

              bool tandaActivada = false; 


              foreach (var tanda in siguientesTandas)
              {
                  if (!salidasActuales.Except(tanda.Salidas).Any() && !tanda.Salidas.Except(salidasActuales).Any())
                  {
                     bool familiaEnWaveRelease = await _context.WaveReleases
                          .AnyAsync(wr => tanda.Familias.Contains(wr.Familia));

                      if (familiaEnWaveRelease)
                      {
                          await _context.Familias
                              .Where(f => f.NumTanda == tanda.NumTanda)
                              .ExecuteUpdateAsync(s => s.SetProperty(f => f.estado, true));

                           await _context.Familias
                              .Where(f => f.NumTanda == numTandaActual)
                              .ExecuteUpdateAsync(s => s.SetProperty(f => f.estado, false));

                          await _context.SaveChangesAsync();


                          tandaActivada = true;

                          Console.WriteLine("EStado tandaActivada " + tandaActivada);

                          return (tanda.NumTanda, tanda.Familias.FirstOrDefault(),
                             $"Se activó la tanda {tanda.NumTanda} y se desactivó la tanda {numTandaActual}.");
                      }
                  }
              }

              Console.WriteLine("Estado tandaActivada fuera del for " + tandaActivada);

              return (null, null, "Error inesperado.");
          }

          */










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
                    // Verificamos si hay al menos una familia en WaveRelease
                    bool familiaEnWaveRelease = await _context.WaveReleases
                        .AnyAsync(wr => tanda.Familias.Contains(wr.Familia));

                    if (!familiaEnWaveRelease)
                    {
                        Console.WriteLine($"Familias de la tanda {tanda.NumTanda} no tienen registros en WaveRelease. Se salta tanda.");
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
