using Newtonsoft.Json;

namespace MeasurementSystemWebAPI.Models
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
}
