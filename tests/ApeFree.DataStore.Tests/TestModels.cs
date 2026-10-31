using System;
using System.Collections.Generic;

namespace ApeFree.DataStore.Tests
{
    public class SampleSettings
    {
        public string AppName { get; set; } = "ApeFree.DataStore.Tests";
        public int Version { get; set; } = 3;
        public bool IsEnabled { get; set; } = true;
        public List<string> Tags { get; set; } = new List<string> { "Storage", "Persistence", "Pipeline" };
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>
        {
            ["Author"] = "Guijie Lee",
            ["Architecture"] = "StreamPipeline"
        };
    }

    public class XmlCompatibleSettings
    {
        public string Title { get; set; } = "XmlSettings";
        public int Id { get; set; } = 1001;
        public double Score { get; set; } = 99.5;
        public bool Active { get; set; } = true;
        public List<string> Items { get; set; } = new List<string>();
    }

    public class BigDataModel
    {
        public string Title { get; set; }
        public float[] SensorData { get; set; }
        public int[] MatrixIndices { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
