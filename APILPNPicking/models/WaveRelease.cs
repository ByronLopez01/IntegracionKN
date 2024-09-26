using Microsoft.Identity.Client;

namespace APILPNPicking.models
{
    public class WaveRelease
    {
        public string codMastr { get; set; }
        public string codInr { get; set; }
        public int cantMastr { get; set; }
        public int cantInr { get; set; }
        public int cantidad { get; set; }
        public string familia { get; set; }
        public string numOrden { get; set; }
        public string codProducto { get; set; }
        public string wave { get; set; }
        public string tienda { get; set; }

    }
}
