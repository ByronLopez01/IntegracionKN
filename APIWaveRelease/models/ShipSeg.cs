namespace APIWaveRelease.models
{
    public class ShipSeg
    {
        public string ShipId { get; set; }
        public string Carcod { get; set; }
        public string Srvlvl { get; set; }
        public List<PickDtlSeg> PickDtlSeg { get; set; }
    }
}
