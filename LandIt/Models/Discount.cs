namespace LandIt.Models
{
    public class Discount
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public double Percentage { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }
    }
}
