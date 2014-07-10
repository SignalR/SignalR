using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using Newtonsoft.Json;

namespace BenchmarkServer
{
    public class PerformanceRecorder<T>
    {
        private struct MetricData
        {
            public string Metric;
            public string Unit;
            public dynamic[] Values;
        }

        private ConcurrentBag<T> _samples;
        private DateTime _startTime;

        public void Reset()
        {
            _samples = new ConcurrentBag<T>();
            _startTime = DateTime.Now;
        }

        public void AddSample(T sample)
        {
            _samples.Add(sample);
        }

        public void Record()
        {
            var startTime = _startTime;
            var endTime = DateTime.Now;
            var samples = _samples.ToList();

            Task.Run(
                () =>
                {
                    var metrics = new List<MetricData>();

                    var properties = typeof(T).GetProperties();
                    foreach (var property in properties)
                    {
                        metrics.Add(new MetricData()
                        {
                            Metric = property.Name,
                            Unit = "Numeric",
                            Values = samples.Select(s => property.GetValue(s)).ToArray()
                        });
                    }

                    var path = HostingEnvironment.MapPath("~/app_data/") + string.Format("Result_{0}.json", _startTime.ToString("yyyy-MM-dd-HH-mm-ss"));

                    var report = new
                        {
                            StartTime = startTime,
                            EndTime = endTime,
                            Metrics = metrics.ToArray()
                        };

                    File.WriteAllText(path, JsonConvert.SerializeObject(report, Formatting.Indented));
                });
        }
    }
}
