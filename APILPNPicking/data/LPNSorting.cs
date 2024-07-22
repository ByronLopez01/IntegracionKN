using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace APILPNPicking.data
{
    public class LPNSorting
    {
     
        public int idLPNSorting { get; set; }
        public string Wave { get; set; }
        public string IdOrdenTrabajo { get; set; } 
        public string CodProducto { get; set; }
        public int CantidadUnidades { get; set; }
        public string DtlNumber { get; set; }
    }
}
