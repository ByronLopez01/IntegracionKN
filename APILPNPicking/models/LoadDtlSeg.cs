using Newtonsoft.Json;

namespace APILPNPicking.models
{
    public class LoadDtlSeg
    {
        public string prtnum { get; set; }
        public string alt_prtnum { get; set; }
        public List<SubnumSeg> SUBNUM_SEG { get; set; }
        public string ordnum { get; set; }
        public int lod_cas_cnt { get; set; }
    }
}
