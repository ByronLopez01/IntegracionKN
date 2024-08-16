namespace APIOrderConfirmation.models
{
    public class LoadHdrSeg
    {
        public string LODNUM { get; set; }
        public List<LoadDtlSeg> LOAD_DTL_SEG { get; set; }
    }
}
