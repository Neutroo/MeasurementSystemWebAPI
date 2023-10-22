using System.ComponentModel.DataAnnotations;

namespace MeasurementSystemWebAPI.Models
{
    public class Device
    {
        [Required]
        public string? Name { get; set; }
        [Required]
        public List<Record> Records { get; set; } = new List<Record>();
    }
}