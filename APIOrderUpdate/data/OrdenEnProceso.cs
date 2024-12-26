namespace APIOrderUpdate.data
{
    public class OrdenEnProceso
    {
        public int id { get; set; }
        public required string codMastr { get; set; }
        public required string codInr { get; set; }
        public int cantMastr { get; set; }
        public int cantInr { get; set; }
        public int cantidad { get; set; }
        public required string familia { get; set; }
        public required string numOrden { get; set; }
        public required string codProducto { get; set; }
        public required string wave { get; set; }
        public int cantidadProcesada { get; set; }
        public int cantidadLPN { get; set; }
        public int numSalida { get; set; }
        public int numTanda { get; set; }
        public required string dtlNumber { get; set; }
        public required string subnum { get; set; }
        public bool estado { get; set; }
        public required string tienda { get; set; }
        public bool estadoLuca { get; set; }
    }

}
