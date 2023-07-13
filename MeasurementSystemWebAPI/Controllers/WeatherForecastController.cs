using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MeasurementSystemWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly InfluxDBClient influxDBClient;
        const string token = "pnntaleyQKKxWHpDovBPe3H1QLP-lnVcF3s1GCoD3NZxo5nabkzpRIyUcAF1DguEaMdarWHtXQ_lPFtHGhJvXg==";
        const string bucket = "measurements-bucket";
        const string org = "org";

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
            influxDBClient = new("http://host.docker.internal:8086", token);
        }

        [HttpGet]
        public async Task<IEnumerable<WeatherForecast>> GetWeatherForecastAsync()
        {
            Console.WriteLine("get");

            var query = "from(bucket: \"measurements-bucket\") |> range(start: 0)";
            var tables = await influxDBClient.GetQueryApi().QueryAsync(query, org);

            List<Record> records = new();

            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    var measurement = record.Values["_measurement"].ToString();

                    var time = record.Values["_time"];
                    var utcTime = DateTime.Parse(time.ToString()).AddHours(3);

                    var field = record.Values["_field"].ToString();
                    var value = record.Values["_value"];

                    records.Add(new Record(measurement, utcTime, field, value));
                }
            }

            var groupByDevices = records.GroupBy(r => r.Measurement).Select(ob => new { DeviceName = ob.Key, Records = ob.Select(r => new { r.Time, r.Field, r.Value }).GroupBy(r => r.Time) });


            var fields = records.GroupBy(r => r.Field).Select(f => new { Sensor = f.Key, Records = f.Select(r => new { r.Time, r.Value }) });

            string json = JsonConvert.SerializeObject(groupByDevices);

            Console.WriteLine(json);

            return null;
        }

        [HttpGet("{measurement}")]
        public async Task<IEnumerable<WeatherForecast>> GetWeatherForecastAsync(string measurement)
        {
            Console.WriteLine("get by measurement");

            var query = $"from(bucket: \"measurements-bucket\") |> range(start: 0) |> filter(fn: (r) => r[\"_measurement\"] == \"{measurement}\")";

            var tables = await influxDBClient.GetQueryApi().QueryAsync(query, org);

            List<Record> records = new();

            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    var measur = record.Values["_measurement"].ToString();

                    var time = record.Values["_time"];
                    var utcTime = DateTime.Parse(time.ToString()).AddHours(3);

                    var field = record.Values["_field"].ToString();
                    var value = record.Values["_value"];

                    records.Add(new Record(measur, utcTime, field, value));

                    //Console.WriteLine($"time: {utcTime}, field: {field}, value: {value}");
                }
            }

            var fields = records.GroupBy(r => r.Field).Select(f => new { Sensor =  f.Key, Records = f.Select(r => new {r.Time, r.Value})});

            string json = JsonConvert.SerializeObject(fields);

            Console.WriteLine(json);

            /*foreach (var field in fields)
            {
                Console.WriteLine(field.Key);

                foreach(var record in field)
                {
                    Console.WriteLine($"time: {record.Time}, value: {record.Value}");
                }

                Console.WriteLine();
            }*/

            /*return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                IsSaturated = Random.Shared.Next(1, 5),
                Red = Random.Shared.Next(200, 20000),
                Green = Random.Shared.Next(200, 20000),
                Blue = Random.Shared.Next(200, 20000)
            })
            .ToArray();*/

            return null;
        }

        [HttpPost]
        public string PostWeatherForecast(object json)
        {
            Console.WriteLine("post");

            if (json == null)
                return "Empty json";

            JsonTextReader reader = new(new StringReader(json.ToString()));

            Dictionary<string, string> keyValuePairs = new();

            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    string nodeName = reader.Value.ToString();
                    reader.Read();
                    while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                    {
                        string propertyName = reader.Value.ToString();
                        reader.Read();
                        keyValuePairs.Add($"{nodeName}_{propertyName}", reader.Value.ToString());
                    }
                }
            }

            var point = PointData.Measurement("DeviceName");

            foreach (var pair in keyValuePairs)
            {
                point = double.TryParse(pair.Value, out double value) ? point.Field(pair.Key, value)
                    : point = point.Field(pair.Key, pair.Value);
            }

            point = point.Timestamp(DateTime.UtcNow, WritePrecision.Ns);

            using (var writeApi = influxDBClient.GetWriteApi())
            {
                writeApi.WritePoint(point, bucket, org);
            }

            string result = JsonConvert.SerializeObject(keyValuePairs);

            return result;

            /*return CreatedAtAction(nameof(PostWeatherForecast),
                result);*/
        }
    }
}