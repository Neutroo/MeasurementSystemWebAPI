namespace MeasurementSystemWebAPI
{
    public class Record
    {
        public string Measurement { get; set; }
        public DateTime Time { get; }
        public string Field { get; }
        public object Value { get; }

        public Record(string measurement, DateTime time, string field, object value)
        {
            Measurement = measurement;
            Time = time;
            Field = field;
            Value = value;
        }
    }

    public class WeatherForecast
    {
        public string DeviceName { get; set; }
        public Dictionary<string, List<Record>> Data { get; set; } = new Dictionary<string, List<Record>>();
    }
}