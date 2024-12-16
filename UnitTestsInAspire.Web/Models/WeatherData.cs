using System.ComponentModel.DataAnnotations;

namespace UnitTestsInAspire.Web.Models
{
    public class WeatherData
    {
        [Key]
        public int Id { get; set; }

        public DateOnly Date { get; set; }

        public double Temperature { get; set; }

        [MaxLength(100)]
        public required string Summary { get; set; }
    }
}
