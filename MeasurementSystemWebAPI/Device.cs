using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace MeasurementSystemWebAPI
{
    public class Record
    {
        public DateTime Time { get; }
        [JsonExtensionData]
        public IDictionary<string, object> Fields { get; set; }

        public Record(DateTime time, IDictionary<string, object> fields)
        {
            Time = time;
            Fields = fields;
        }
    }

    public class Device
    {
        [Required]
        public string? Name { get; set; }
        [Required]
        public List<Record> Records { get; set; } = new List<Record>();
    }
}