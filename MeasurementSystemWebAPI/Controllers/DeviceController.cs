using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using MeasurementSystemWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Dynamic;

namespace MeasurementSystemWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly ILogger<DeviceController> _logger;
        private readonly InfluxDBClient influxDBClient;
        const string token = "pnntaleyQKKxWHpDovBPe3H1QLP-lnVcF3s1GCoD3NZxo5nabkzpRIyUcAF1DguEaMdarWHtXQ_lPFtHGhJvXg==";
        const string bucket = "measurements-bucket";
        const string org = "org";

        public DeviceController(ILogger<DeviceController> logger)
        {
            _logger = logger;
            influxDBClient = new("http://influxdb:8086", token);
        }

        [HttpGet]
        public async Task<IEnumerable<Device>?> GetWeatherForecastAsync(DateTime from, DateTime to)
        {
            Console.WriteLine("get");

            if (from > to)
            {
                return null;
            }

            var query = $"from(bucket: \"measurements-bucket\") |> range(start: {from.Subtract(DateTime.UnixEpoch).TotalSeconds}, " +
                $"stop: {to.Subtract(DateTime.UnixEpoch).TotalSeconds})";
            var tables = await influxDBClient.GetQueryApi().QueryAsync(query, org);

            Dictionary<string, Dictionary<DateTime, IDictionary<string, object>>> data = new();

            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    var measurement = record.Values["_measurement"].ToString();
                    var time = record.Values["_time"];
                    var utcTime = DateTime.Parse(time.ToString()).AddHours(3);
                    var field = record.Values["_field"].ToString();
                    var value = record.Values["_value"];

                    data.TryAdd(measurement, new Dictionary<DateTime, IDictionary<string, object>>());

                    data[measurement].TryAdd(utcTime, new ExpandoObject());

                    data[measurement][utcTime].Add(field, value);
                }
            }

            List<Device> devices = new();

            foreach (var pair in data)
            {
                List<Record> records = new();

                foreach (var record in pair.Value)
                {
                    records.Add(new Record(record.Key, record.Value));
                }

                devices.Add(new Device()
                {
                    Name = pair.Key,
                    Records = records
                });
            }

            return devices;
        }

        [HttpPost]
        public IActionResult PostWeatherForecast(object json)
        {
            Console.WriteLine("post");

            if (json == null)
            {
                return BadRequest("Empty json");
            }

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

                        if (reader.Value != null && reader.TokenType != JsonToken.StartObject)
                        {
                            keyValuePairs.Add($"{nodeName}_{propertyName}", reader.Value.ToString());
                        }
                        else
                        {
                            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                            {
                                string additionalProperty = reader.Value.ToString();
                                reader.Read();

                                if (reader.Value != null && reader.TokenType != JsonToken.StartObject)
                                {
                                    keyValuePairs.Add($"{nodeName}_{propertyName}_{additionalProperty}", reader.Value.ToString());
                                }
                            }
                        }
                    }
                }
            }

            if (!keyValuePairs.ContainsKey("system_Akey"))
            {
                return BadRequest("No authentication key");              
            }

            var authKey = keyValuePairs["system_Akey"];
            keyValuePairs.Remove("system_Akey");

            var point = PointData.Measurement(authKey);

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
            Console.WriteLine(result);

            return Ok();
        }
    }
}