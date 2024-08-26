using Newtonsoft.Json;

namespace APILPNPicking.models
{
    public class SortInduction
    {

        public string wcs_id { get; set; }
        public string wh_id { get; set; }
        public string msg_id { get; set; }
        public string trandt { get; set; }
        public LoadHdrSeg LOAD_HDR_SEG { get; set; }
    }
}
