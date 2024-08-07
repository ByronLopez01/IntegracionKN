
using Newtonsoft.Json;

namespace APIWaveRelease.models
{
    public class OrderTransSeg
    {
        //[JsonProperty("schbat")]
        public string schbat { get; set; }

        //[JsonProperty("ORDER_SEG")]
        public OrderSeg ORDER_SEG { get; set; }
    }
}
