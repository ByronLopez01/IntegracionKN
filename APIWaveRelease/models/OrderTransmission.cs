using Newtonsoft.Json;

namespace APIWaveRelease.models
{
    public class OrderTransmission
    {
        //[JsonProperty("wcs_id")]
        public string wcs_id { get; set; }

        //[JsonProperty("wh_id")]
        public string wh_id { get; set; }

        //[JsonProperty("msg_id")]
        public string msg_id { get; set; }

        //[JsonProperty("trandt")]
        public string trandt { get; set; }

        //[JsonProperty("ORDER_TRANS_SEG")]
        public OrderTransSeg ORDER_TRANS_SEG { get; set; }
    }
}
