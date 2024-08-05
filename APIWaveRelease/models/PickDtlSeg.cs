namespace APIWaveRelease.models
{
    public class PickDtlSeg
    {

        public string Wrkref { get; set; }
        public string Prtnum { get; set; }
        public string Prtfam { get; set; }
        public string AltPrtnum { get; set; }
        public string MscsEan { get; set; }
        public string IncsEan { get; set; }
        public int QtyMscs { get; set; }
        public int QtyIncs { get; set; }
        public int Qty { get; set; }
        public int OrdCasCnt { get; set; }
        public string Stgloc { get; set; }
        public string MovZoneCode { get; set; }
        public string Conveyable { get; set; }
        public decimal CubicVol { get; set; }
    }
}
