using Microsoft.Identity.Client;

namespace APILPNPicking.models
{
    public class WaveRelease
    {
        public int Id { get; set; }
        public string CodMastr { get; set; }
        public string CodInr { get; set; }
        public int CantMastr { get; set; }
        public int CantInr { get; set; }
        public int Cantidad { get; set; }
        public string Familia { get; set; }
        public string NumOrden { get; set; }
        public string CodProducto { get; set; }
        public string Wave { get; set; }
        public string Tienda { get; set; }

        public bool EstadoWave { get; set; }

    }
}
