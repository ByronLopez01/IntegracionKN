using Newtonsoft.Json;

namespace APILPNPicking.models
{
    public class LoadHdrSeg
    { 
        public string lodnum { get; set; }

        public List<LoadDtlSeg> LOAD_DTL_SEG { get; set; }
    }
}
