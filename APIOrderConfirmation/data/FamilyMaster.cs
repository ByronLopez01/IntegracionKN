namespace APIOrderConfirmation.data
{
    public class FamilyMaster
    {
        public int IdFamilyMaster { get; set; }
        public required string Familia { get; set; }
        public int NumSalida { get; set; }
        public int NumTanda { get; set; }
        public bool estado { get; set; }
    }
}
