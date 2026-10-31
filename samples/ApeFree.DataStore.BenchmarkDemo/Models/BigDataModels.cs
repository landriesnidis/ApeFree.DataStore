using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using ApeFree.Protocols.Json.Jbin.Attributes;
using ApeFree.Protocols.Json.Jbin.Extensions;
using Newtonsoft.Json;

namespace ApeFree.DataStore.BenchmarkDemo.Models
{
    /// <summary>
    /// 海量传感器与 3D 点云时序数据集（值类型连续大数组）
    /// 重点考察基元数组零拷贝内存映射与二进制流压缩能力
    /// </summary>
    [Serializable]
    [XmlRoot("PointCloudDataset")]
    public class PointCloudDataset
    {
        public string DatasetName { get; set; } = "Industrial 3D LiDAR & Multi-Sensor Telemetry";
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
        public int TotalPoints { get; set; }

        public float[] PositionsX { get; set; }
        public float[] PositionsY { get; set; }
        public float[] PositionsZ { get; set; }
        public double[] Timestamps { get; set; }
        public int[] Intensities { get; set; }
        public byte[] ReturnFlags { get; set; }

        public PointCloudDataset() { }

        /// <summary>
        /// 生成指定规模的点云时序测试数据
        /// </summary>
        public static PointCloudDataset Generate(int count)
        {
            var rand = new Random(42);
            var ds = new PointCloudDataset
            {
                TotalPoints = count,
                CapturedAt = DateTime.UtcNow,
                PositionsX = new float[count],
                PositionsY = new float[count],
                PositionsZ = new float[count],
                Timestamps = new double[count],
                Intensities = new int[count],
                ReturnFlags = new byte[count]
            };

            double baseTime = 1700000000.0;
            for (int i = 0; i < count; i++)
            {
                ds.PositionsX[i] = (float)(rand.NextDouble() * 200.0 - 100.0);
                ds.PositionsY[i] = (float)(rand.NextDouble() * 200.0 - 100.0);
                ds.PositionsZ[i] = (float)(rand.NextDouble() * 50.0);
                ds.Timestamps[i] = baseTime + (i * 0.001); // 1ms 间隔时序
                ds.Intensities[i] = rand.Next(0, 65535);
                ds.ReturnFlags[i] = (byte)(i % 4);
            }

            return ds;
        }
    }

    /// <summary>
    /// 业务设备监控/日志实体记录
    /// </summary>
    [Serializable]
    public class DeviceLogRecord
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; }
        public string LogLevel { get; set; }
        public int StatusCode { get; set; }
        public double MetricValue { get; set; }
        public string Message { get; set; }
        public string TraceId { get; set; }
    }

    /// <summary>
    /// 超大业务实体对象集合（行式存储）
    /// </summary>
    [Serializable]
    [XmlRoot("DeviceLogBatch")]
    public class DeviceLogBatch
    {
        public string BatchId { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
        public string Environment { get; set; } = "Production-Cluster-A";
        public List<DeviceLogRecord> Records { get; set; } = new List<DeviceLogRecord>();

        public DeviceLogBatch() { }

        /// <summary>
        /// 生成指定数量的设备监控与日志实体记录
        /// </summary>
        public static DeviceLogBatch Generate(int count)
        {
            var rand = new Random(100);
            var batch = new DeviceLogBatch
            {
                Records = new List<DeviceLogRecord>(count)
            };

            string[] levels = { "INFO", "WARN", "ERROR", "DEBUG" };
            string[] devices = { "DEV-EDGE-001", "DEV-EDGE-002", "DEV-GATEWAY-10", "DEV-PLC-808" };
            DateTime baseTime = DateTime.UtcNow.AddHours(-1);

            for (int i = 0; i < count; i++)
            {
                batch.Records.Add(new DeviceLogRecord
                {
                    Id = 1000000L + i,
                    Timestamp = baseTime.AddMilliseconds(i * 10),
                    DeviceId = devices[i % devices.Length],
                    LogLevel = levels[i % levels.Length],
                    StatusCode = (i % 20 == 0) ? 500 : 200,
                    MetricValue = Math.Round(rand.NextDouble() * 100.0, 3),
                    Message = $"Sensor telemetry frame successfully synchronized for channel {i % 16}.",
                    TraceId = $"TRC-{i:D8}-XYZ"
                });
            }

            return batch;
        }
    }

    /// <summary>
    /// 列式转置存储数据集（基于 Jbin TransposeToDictionary）
    /// 重点考察超大对象集合转置为列式结构后的极致压缩与吞吐提升
    /// </summary>
    [Serializable]
    public class ColumnarDeviceLogBatch
    {
        public string BatchId { get; set; }
        public DateTime CreatedTime { get; set; }
        public string Environment { get; set; }
        public int RecordCount { get; set; }

        /// <summary>
        /// 列式字典数据（属性名 -> 连续基元/字符串数组）
        /// </summary>
        public Dictionary<string, Array> Columns { get; set; }

        public ColumnarDeviceLogBatch() { }

        /// <summary>
        /// 将传统的行式实体集合通过表达式树预编译转置为列式字典存储
        /// </summary>
        public static ColumnarDeviceLogBatch FromRowBatch(DeviceLogBatch batch)
        {
            return new ColumnarDeviceLogBatch
            {
                BatchId = batch.BatchId,
                CreatedTime = batch.CreatedTime,
                Environment = batch.Environment,
                RecordCount = batch.Records.Count,
                Columns = batch.Records.TransposeToDictionary()
            };
        }

        /// <summary>
        /// 将列式字典数据瞬时还原回强类型行式对象实体集合
        /// </summary>
        public List<DeviceLogRecord> RestoreToRecords()
        {
            var arr = Columns.TransposeFromDictionary<DeviceLogRecord>();
            return new List<DeviceLogRecord>(arr);
        }
    }
}
