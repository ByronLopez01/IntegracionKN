using APIWaveRelease.data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

// Asegúrate de que el namespace coincida con la estructura de carpetas.
namespace APIWaveRelease.Pages.Shared.Components.WaveReleaseStatus
{
    public class WaveReleaseStatusViewComponent : ViewComponent
    {
        private readonly WaveReleaseContext _context;

        public WaveReleaseStatusViewComponent(WaveReleaseContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var waveActiva = await _context.WaveRelease
                .AsNoTracking()
                .Where(wr => wr.estadoWave == true)
                .Select(wr => wr.Wave)
                .FirstOrDefaultAsync();

            var model = new WaveReleaseStatusViewModel
            {
                NombreWaveActiva = waveActiva,
                ExisteWaveActiva = !string.IsNullOrEmpty(waveActiva)
            };

            return View(model);
        }
    }

    // El ViewModel se declara aquí, dentro del mismo namespace.
    public class WaveReleaseStatusViewModel
    {
        public string? NombreWaveActiva { get; set; }
        public bool ExisteWaveActiva { get; set; }
    }
}