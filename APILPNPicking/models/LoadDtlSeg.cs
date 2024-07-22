namespace APILPNPicking.models
{
    public class LoadDtlSeg
    {
        public string Prtnum { get; set; }
        public string AltPrtnum { get; set; }
        public List<SubnumSeg> SubnumSeg { get; set; }
        public string Ordnum { get; set; }
        public int LodCasCnt { get; set; }
    }
}
