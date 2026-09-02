namespace TheAdamsParadigm.Api.Models
{
    public class Service
    {
        public int ServiceId { get; set; }

        public string Icon { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal CostPerHour { get; set; } = 0.00m;

        public decimal SetupFee { get; set; } = 0.00m;

        // Navigation property
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
