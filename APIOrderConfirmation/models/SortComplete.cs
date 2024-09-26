namespace APIOrderConfirmation.models
{
    public class SortComplete
    {
        public string wcs_id { get; set; }
        public string wh_id { get; set; }
        public string msg_id { get; set; }
        public string trandt { get; set; }
        public SortCompSeg SORT_COMP_SEG { get; set; }
    }
}
