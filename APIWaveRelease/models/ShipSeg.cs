using Newtonsoft.Json;

namespace APIWaveRelease.models
{
    public class ShipSeg
    {
        //[JsonProperty("ship_id")]
        public string ship_id { get; set; }

        //[JsonProperty("carcod")]
        public string carcod { get; set; }

        //[JsonProperty("srvlvl")]
        public string srvlvl { get; set; }

        //[JsonProperty("PICK_DTL_SEG")]
        public List<PickDtlSeg> PICK_DTL_SEG { get; set; }
    }
}
