namespace APILPNPicking.data
{
    public class OrdenEnProceso
    {

        public int id { get; set; }
        public string codMastr { get; set; }
        public string codInr { get; set; }
        public int cantMastr { get; set; }
        public int cantInr { get; set; }
        public int cantidad { get; set; }
        public string familia { get; set; }
        public string numOrden { get; set; }
        public string codProducto { get; set; }
        public string wave { get; set; }
        public int cantidadProcesada { get; set; }
        public int cantidadLPN { get; set; }
        public int numSalida { get; set; }
        public int numTanda { get; set; }
        public string dtlNumber {  get; set; }
        public string subnum { get; set; }
        public bool estado { get; set; }
        public string tienda { get; set; }
        public bool estadoLuca { get; set; }

    }
}
