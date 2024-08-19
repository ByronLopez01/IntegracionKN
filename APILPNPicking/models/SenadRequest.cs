namespace APILPNPicking.models
{
    public class SenadRequest
    {
        public string BillCode { get; set; }
        public string BoxCode { get; set; }
        public float Weight { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public int Rectangle { get; set; }
        public string OrgCode { get; set; }
        public string WarehouseId { get; set; }
        public int ScanType { get; set; }
        public string DeviceId { get; set; }
        public string SendTime { get; set; }

    }
}
