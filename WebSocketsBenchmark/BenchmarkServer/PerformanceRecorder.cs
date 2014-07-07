using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var endTime = DateTime.Now;

            var samples = _samples.ToList();
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

            var report = new
            {
                StartTime = _startTime,
                EndTime = endTime,
                Failures = new object[0],
                Metrics = metrics.ToArray()
            };

            var jsonWriter = new JsonTextWriter(new StreamWriter(File.Open("Result_" + _startTime.ToString("yyyy-MM-dd-HH-mm-ss"), FileMode.CreateNew)));

            var serializer = new JsonSerializer();
            serializer.Serialize(jsonWriter, report);
        }
    }
}
