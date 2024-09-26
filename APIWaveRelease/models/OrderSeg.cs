using Newtonsoft.Json;

namespace APIWaveRelease.models
{
    public class OrderSeg
    {
        //[JsonProperty("ordnum")]
        public string ordnum { get; set; }

        //[JsonProperty("cponum")]
        public string cponum { get; set; }

        //[JsonProperty("rtcust")]
        public string rtcust { get; set; }

        //[JsonProperty("stcust")]
        public string stcust { get; set; }

        //[JsonProperty("ordtyp")]
        public string ordtyp { get; set; }

        //[JsonProperty("ADDRESS_SEG")]
        public AddressSeg ADDRESS_SEG { get; set; }

        //[JsonProperty("SHIP_SEG")]
        public ShipSeg SHIP_SEG { get; set; }
    }
}
