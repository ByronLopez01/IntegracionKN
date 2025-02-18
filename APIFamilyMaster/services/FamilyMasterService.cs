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

        public async Task<(int? NumTanda, string? Familia, string Message)> ActivarSiguienteTandaAsync(int numTandaActual)
        {
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




    }
}
