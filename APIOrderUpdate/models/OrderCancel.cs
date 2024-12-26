namespace APIOrderUpdate.models
{
    public class OrderCancel
    {
        public string wcs_id { get; set; }
        public string wh_id { get; set; }
        public string msg_id { get; set; }
        public string trandt {  get; set; }
        public List<OrderCancelSeg> ORDER_CANCEL_SEG {  get; set; }

    }
}
