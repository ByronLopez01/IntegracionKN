using System.ComponentModel.DataAnnotations;

namespace APIOrderConfirmation.data
{
    public class Confirmada
    {

        public int Id { get; set; }
        public string WcsId { get; set; } 
        public string WhId { get; set; } 
        public string MsgId { get; set; } 
        public string TranDt { get; set; }
        public string LodNum { get; set; }
        public string SubNum { get; set; } 
        public string DtlNum { get; set; }
        public string StoLoc { get; set; }
        public int Qty { get; set; } 
    }
}
