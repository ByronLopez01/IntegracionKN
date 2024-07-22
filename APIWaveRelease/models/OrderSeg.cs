namespace APIWaveRelease.models
{
    public class OrderSeg
    {
        public string Ordnum { get; set; }
        public string Cponum { get; set; }
        public string Rtcust { get; set; }
        public string Stcust { get; set; }
        public string Ordtyp { get; set; }
        public AddressSeg AddressSeg { get; set; }
        public ShipSeg ShipSeg { get; set; }
    }
}
