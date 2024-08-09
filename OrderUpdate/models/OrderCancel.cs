namespace OrderUpdate.models
{
    public class OrderCancel
    {
        public string wcs_id { get; set; }
        public string wh_id { get; set; }
        public string msg_id { get; set; }
        public string trandt {  get; set; }
        public OrderCancelSeg ORDER_CANCEL_SEG {  get; set; }

    }
}
