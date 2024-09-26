using Newtonsoft.Json;

namespace APIWaveRelease.models
{
    public class PickDtlSeg
    {

       // [JsonProperty("wrkref")]
        public string wrkref { get; set; }

        //[JsonProperty("prtnum")]
        public string prtnum { get; set; }

        //[JsonProperty("prtfam")]
        public string prtfam { get; set; }

        //[JsonProperty("alt_prtnum")]
        public string alt_prtnum { get; set; }

        //[JsonProperty("mscs_ean")]
        public string mscs_ean { get; set; }

        //[JsonProperty("incs_ean")]
        public string incs_ean { get; set; }

        //[JsonProperty("qty_mscs")]
        public int qty_mscs { get; set; }

        //[JsonProperty("qty_incs")]
        public int qty_incs { get; set; }

        //[JsonProperty("qty")]
        public int qty { get; set; }

        //[JsonProperty("ord_cas_cnt")]
        public int ord_cas_cnt { get; set; }

        //[JsonProperty("stgloc")]
        public string stgloc { get; set; }

        //[JsonProperty("mov_zone_code")]
        public string mov_zone_code { get; set; }

        //[JsonProperty("conveyable")]
        public string conveyable { get; set; }

        //[JsonProperty("cubic_vol")]
        public decimal cubic_vol { get; set; }
    }
}
