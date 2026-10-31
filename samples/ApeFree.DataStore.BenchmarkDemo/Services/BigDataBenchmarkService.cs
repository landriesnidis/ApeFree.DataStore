using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ApeFree.DataStore.Adapters;
using ApeFree.DataStore.BenchmarkDemo.Models;
using ApeFree.DataStore.Core;
using ApeFree.DataStore.Filters;
using ApeFree.DataStore.Local;
using ApeFree.Protocols.Json.Jbin;
using Newtonsoft.Json;

namespace ApeFree.DataStore.BenchmarkDemo.Services
{
    /// <summary>
    /// 超大数据单次读写基准测试结果
    /// </summary>
    public class BigDataBenchmarkResult
    {
        public string StrategyName { get; set; }
        public string DatasetType { get; set; }
        public int ElementCount { get; set; }
        public double WriteElapsedMs { get; set; }
        public double ReadElapsedMs { get; set; }
        public double TotalElapsedMs => WriteElapsedMs + ReadElapsedMs;
        public long FileSizeBytes { get; set; }
        public string FormattedFileSize => FormatBytes(FileSizeBytes);
        public double SizeReductionPercent { get; set; }
        public double WriteThroughputMBs { get; set; }
        public bool IsVerified { get; set; }
        public string Notes { get; set; }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024)
            {
                return $"{(double)bytes / (1024 * 1024):F2} MB";
            }
            if (bytes >= 1024)
            {
                return $"{(double)bytes / 1024:F2} KB";
            }
            return $"{bytes} B";
        }
    }

    /// <summary>
    /// 超大数据读写吞吐与容量压缩基准测试执行服务
    /// </summary>
    public class BigDataBenchmarkService
    {
        private readonly string tempDir;

        private class BenchmarkStep
        {
            public string Name { get; }
            public Func<BigDataBenchmarkResult> Runner { get; }

            public BenchmarkStep(string name, Func<BigDataBenchmarkResult> runner)
            {
                Name = name;
                Runner = runner;
            }
        }

        public BigDataBenchmarkService()
        {
            tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Benchmark_Temp_BigData");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
        }

        #region 数据集 1: 海量传感器与 3D 点云时序数据 (连续基元值类型大数组)

        /// <summary>
        /// 运行海量传感器/点云数据多方案横向评测
        /// </summary>
        public List<BigDataBenchmarkResult> RunPointCloudBenchmark(
            int pointCount,
            Action<string> logAction,
            Action<int, int, string> progressAction)
        {
            var results = new List<BigDataBenchmarkResult>();
            logAction?.Invoke($"\r\n================ 开始评测: 海量传感器/点云时序数据集 ({pointCount:N0} 点) ================");
            logAction?.Invoke("正在内存中构建高精度 3D 点云与时序采样数组...");

            var dataset = PointCloudDataset.Generate(pointCount);
            long baselineJsonSize = 0;

            var steps = new List<BenchmarkStep>
            {
                // 1. 原生 Newtonsoft.Json
                new BenchmarkStep("原生 Newtonsoft.Json (纯文本格式)", () =>
                {
                    string path = Path.Combine(tempDir, "pointcloud_raw.json");
                    var sw = Stopwatch.StartNew();
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                    using (var swWriter = new StreamWriter(fs, Encoding.UTF8))
                    using (var jWriter = new JsonTextWriter(swWriter))
                    {
                        var serializer = new JsonSerializer();
                        serializer.Serialize(jWriter, dataset);
                    }
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;
                    baselineJsonSize = size;

                    sw.Restart();
                    PointCloudDataset loaded;
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
                    using (var srReader = new StreamReader(fs, Encoding.UTF8))
                    using (var jReader = new JsonTextReader(srReader))
                    {
                        var serializer = new JsonSerializer();
                        loaded = serializer.Deserialize<PointCloudDataset>(jReader);
                    }
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = loaded != null && loaded.TotalPoints == pointCount &&
                                    Math.Abs(loaded.PositionsX[0] - dataset.PositionsX[0]) < 0.001f &&
                                    Math.Abs(loaded.PositionsZ[pointCount - 1] - dataset.PositionsZ[pointCount - 1]) < 0.001f;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "原生 Newtonsoft.Json (纯文本格式)",
                        DatasetType = "海量点云基元数组",
                        ElementCount = pointCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = 0.0,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "基准方案：大数组转换为数十万浮点数字符串，存在巨大文本膨胀"
                    };
                }),

                // 2. 原生 XmlSerializer
                new BenchmarkStep("原生 XmlSerializer (标准 XML 格式)", () =>
                {
                    string path = Path.Combine(tempDir, "pointcloud_raw.xml");
                    var xmlSerializer = new XmlSerializer(typeof(PointCloudDataset));

                    var sw = Stopwatch.StartNew();
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                    {
                        xmlSerializer.Serialize(fs, dataset);
                    }
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    sw.Restart();
                    PointCloudDataset loaded;
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
                    {
                        loaded = (PointCloudDataset)xmlSerializer.Deserialize(fs);
                    }
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = loaded != null && loaded.TotalPoints == pointCount &&
                                    Math.Abs(loaded.PositionsX[0] - dataset.PositionsX[0]) < 0.001f;

                    double diff = baselineJsonSize > 0 ? (double)(size - baselineJsonSize) / baselineJsonSize * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "原生 XmlSerializer (标准 XML 格式)",
                        DatasetType = "海量点云基元数组",
                        ElementCount = pointCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = -diff,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = $"XML 标签包装开销较大，体积相对 JSON 变化: {(diff > 0 ? "+" : "")}{diff:F1}%"
                    };
                }),

                // 3. ApeFree.DataStore + Json + GZip 压缩管道
                new BenchmarkStep("ApeFree.DataStore (Json + GZip 流压缩)", () =>
                {
                    string path = Path.Combine(tempDir, "pointcloud_datastore_json_gzip.dat");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = new JsonSerializationAdapter()
                    };
                    settings.StreamFilters.Add(new GZipStreamFilter());

                    var store = StoreFactory.Factory.CreateLocalStore<PointCloudDataset>(settings, () => dataset);

                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<PointCloudDataset>(settings);
                    sw.Restart();
                    readStore.Load();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = readStore.Value != null && readStore.Value.TotalPoints == pointCount &&
                                    Math.Abs(readStore.Value.PositionsX[0] - dataset.PositionsX[0]) < 0.001f;

                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Json + GZip 流压缩)",
                        DatasetType = "海量点云基元数组",
                        ElementCount = pointCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "文本序列化后经 GZip 管道即时流式压缩，体积大幅降低"
                    };
                }),

                // 4. ApeFree.DataStore + Jbin (二进制列式 / 连续基元零拷贝)
                new BenchmarkStep("ApeFree.DataStore (Jbin 混合二进制零拷贝)", () =>
                {
                    string path = Path.Combine(tempDir, "pointcloud_datastore_jbin.jbin");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = new JbinSerializationAdapter()
                    };

                    var store = StoreFactory.Factory.CreateLocalStore<PointCloudDataset>(settings, () => dataset);

                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<PointCloudDataset>(settings);
                    sw.Restart();
                    readStore.Load();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = readStore.Value != null && readStore.Value.TotalPoints == pointCount &&
                                    Math.Abs(readStore.Value.PositionsX[0] - dataset.PositionsX[0]) < 0.001f &&
                                    Math.Abs(readStore.Value.PositionsZ[pointCount - 1] - dataset.PositionsZ[pointCount - 1]) < 0.001f;

                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Jbin 原生连续内存 Raw)",
                        DatasetType = "海量点云基元数组",
                        ElementCount = pointCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "原生基元零拷贝内存映射：无字符串开销，纯连续内存块快速写盘"
                    };
                }),

                // 5. ApeFree.DataStore + Jbin (紧凑数值编码)
                new BenchmarkStep("ApeFree.DataStore (Jbin 紧凑模式)", () =>
                {
                    string path = Path.Combine(tempDir, "pointcloud_datastore_jbin_alg.jbin");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = JbinSerializationAdapter.CreateWithPrimitiveCompression(
                            singleMode: SingleArrayCompressMode.Gorilla,
                            doubleMode: DoubleArrayCompressMode.Gorilla,
                            int32Mode: Int32ArrayCompressMode.FrameOfReference)
                    };

                    var store = StoreFactory.Factory.CreateLocalStore<PointCloudDataset>(settings, () => dataset);

                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<PointCloudDataset>(settings);
                    sw.Restart();
                    readStore.Load();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = readStore.Value != null && readStore.Value.TotalPoints == pointCount &&
                                    Math.Abs(readStore.Value.PositionsX[0] - dataset.PositionsX[0]) < 0.001f &&
                                    Math.Abs(readStore.Value.PositionsZ[pointCount - 1] - dataset.PositionsZ[pointCount - 1]) < 0.001f;

                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Jbin 紧凑模式)",
                        DatasetType = "海量点云基元数组",
                        ElementCount = pointCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "内置紧凑数值编码（无外层流过滤器）：针对连续数值数组执行紧凑编码，自描述还原"
                    };
                }),

                // 6. ApeFree.DataStore + Jbin + Deflate 流压缩
                new BenchmarkStep("ApeFree.DataStore (Jbin + Deflate 流压缩)", () =>
                {
                    string path = Path.Combine(tempDir, "pointcloud_datastore_jbin_deflate.dat");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = new JbinSerializationAdapter()
                    };
                    settings.StreamFilters.Add(new DeflateStreamFilter());

                    var store = StoreFactory.Factory.CreateLocalStore<PointCloudDataset>(settings, () => dataset);

                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<PointCloudDataset>(settings);
                    sw.Restart();
                    readStore.Load();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = readStore.Value != null && readStore.Value.TotalPoints == pointCount &&
                                    Math.Abs(readStore.Value.PositionsX[0] - dataset.PositionsX[0]) < 0.001f;

                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Jbin + Deflate 流压缩)",
                        DatasetType = "海量点云基元数组",
                        ElementCount = pointCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "基于 Deflate 无损压缩流管道，略去头部元数据封装，体积紧凑"
                    };
                }),

                // 7. ApeFree.DataStore + Jbin + GZip 流压缩
                new BenchmarkStep("ApeFree.DataStore (Jbin + GZip 流压缩)", () =>
                {
                    string path = Path.Combine(tempDir, "pointcloud_datastore_jbin_gzip.dat");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = new JbinSerializationAdapter()
                    };
                    settings.StreamFilters.Add(new GZipStreamFilter());

                    var store = StoreFactory.Factory.CreateLocalStore<PointCloudDataset>(settings, () => dataset);

                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<PointCloudDataset>(settings);
                    sw.Restart();
                    readStore.Load();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = readStore.Value != null && readStore.Value.TotalPoints == pointCount &&
                                    Math.Abs(readStore.Value.PositionsX[0] - dataset.PositionsX[0]) < 0.001f;

                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Jbin + GZip 流压缩)",
                        DatasetType = "海量点云基元数组",
                        ElementCount = pointCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "连续二进制数据块经 GZip 流管道压缩，兼顾存储体积与反序列化读取速度"
                    };
                }),

                // 8. ApeFree.DataStore + Jbin 紧凑模式 + GZip 复合管道
                new BenchmarkStep("ApeFree.DataStore (Jbin 紧凑模式 + GZip 复合管道)", () =>
                {
                    string path = Path.Combine(tempDir, "pointcloud_datastore_jbin_alg_gzip.dat");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = JbinSerializationAdapter.CreateWithPrimitiveCompression(
                            singleMode: SingleArrayCompressMode.Gorilla,
                            doubleMode: DoubleArrayCompressMode.Gorilla,
                            int32Mode: Int32ArrayCompressMode.FrameOfReference)
                    };
                    settings.StreamFilters.Add(new GZipStreamFilter());

                    var store = StoreFactory.Factory.CreateLocalStore<PointCloudDataset>(settings, () => dataset);

                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<PointCloudDataset>(settings);
                    sw.Restart();
                    readStore.Load();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = readStore.Value != null && readStore.Value.TotalPoints == pointCount &&
                                    Math.Abs(readStore.Value.PositionsX[0] - dataset.PositionsX[0]) < 0.001f;

                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Jbin 紧凑模式 + GZip 复合管道)",
                        DatasetType = "海量点云基元数组",
                        ElementCount = pointCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "双重压缩：内置紧凑编码配合外层通用 GZip 管道二次压缩"
                    };
                })
            };

            for (int i = 0; i < steps.Count; i++)
            {
                progressAction?.Invoke(i + 1, steps.Count, steps[i].Name);
                logAction?.Invoke($"[{i + 1}/{steps.Count}] 正在评测: {steps[i].Name}...");
                var r = steps[i].Runner();
                results.Add(r);
                logAction?.Invoke($"  -> 完成: 写入={r.WriteElapsedMs:F1}ms, 读取={r.ReadElapsedMs:F1}ms, 体积={r.FormattedFileSize}, 相对JSON缩减={r.SizeReductionPercent:F1}%, 校验={r.IsVerified}");
            }

            return results;
        }

        #endregion

        #region 数据集 2: 超大业务实体对象集合 (包含大量实体对象，重点展示 Jbin 列式存储)

        /// <summary>
        /// 运行超大业务实体对象集合多方案横向评测
        /// </summary>
        public List<BigDataBenchmarkResult> RunObjectBatchBenchmark(
            int recordCount,
            Action<string> logAction,
            Action<int, int, string> progressAction)
        {
            var results = new List<BigDataBenchmarkResult>();
            logAction?.Invoke($"\r\n================ 开始评测: 超大业务实体对象集合 ({recordCount:N0} 条记录) ================");
            logAction?.Invoke("正在内存中构建实体对象集合...");

            var batch = DeviceLogBatch.Generate(recordCount);
            long baselineJsonSize = 0;

            var steps = new List<BenchmarkStep>
            {
                // 1. 原生 Newtonsoft.Json (行式存储)
                new BenchmarkStep("原生 Newtonsoft.Json (行式对象数组)", () =>
                {
                    string path = Path.Combine(tempDir, "objects_raw.json");
                    var sw = Stopwatch.StartNew();
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                    using (var swWriter = new StreamWriter(fs, Encoding.UTF8))
                    using (var jWriter = new JsonTextWriter(swWriter))
                    {
                        var serializer = new JsonSerializer();
                        serializer.Serialize(jWriter, batch);
                    }
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;
                    baselineJsonSize = size;

                    sw.Restart();
                    DeviceLogBatch loaded;
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
                    using (var srReader = new StreamReader(fs, Encoding.UTF8))
                    using (var jReader = new JsonTextReader(srReader))
                    {
                        var serializer = new JsonSerializer();
                        loaded = serializer.Deserialize<DeviceLogBatch>(jReader);
                    }
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = loaded != null && loaded.Records.Count == recordCount &&
                                    loaded.Records[0].DeviceId == batch.Records[0].DeviceId;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "原生 Newtonsoft.Json (行式对象数组)",
                        DatasetType = "超大对象集合",
                        ElementCount = recordCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = 0.0,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "基准方案：每个对象均重复输出键名属性，产生海量字符串冗余"
                    };
                }),

                // 2. 原生 XmlSerializer (行式存储)
                new BenchmarkStep("原生 XmlSerializer (行式 XML 对象集合)", () =>
                {
                    string path = Path.Combine(tempDir, "objects_raw.xml");
                    var xmlSerializer = new XmlSerializer(typeof(DeviceLogBatch));

                    var sw = Stopwatch.StartNew();
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                    {
                        xmlSerializer.Serialize(fs, batch);
                    }
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    sw.Restart();
                    DeviceLogBatch loaded;
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
                    {
                        loaded = (DeviceLogBatch)xmlSerializer.Deserialize(fs);
                    }
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = loaded != null && loaded.Records.Count == recordCount;
                    double diff = baselineJsonSize > 0 ? (double)(size - baselineJsonSize) / baselineJsonSize * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "原生 XmlSerializer (行式 XML 对象集合)",
                        DatasetType = "超大对象集合",
                        ElementCount = recordCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = -diff,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = $"XML 节点开销庞大，体积相对 JSON 变化: {(diff > 0 ? "+" : "")}{diff:F1}%"
                    };
                }),

                // 3. ApeFree.DataStore + Json + GZip 压缩
                new BenchmarkStep("ApeFree.DataStore (Json + GZip 流压缩)", () =>
                {
                    string path = Path.Combine(tempDir, "objects_datastore_json_gzip.dat");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = new JsonSerializationAdapter()
                    };
                    settings.StreamFilters.Add(new GZipStreamFilter());

                    var store = StoreFactory.Factory.CreateLocalStore<DeviceLogBatch>(settings, () => batch);

                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<DeviceLogBatch>(settings);
                    sw.Restart();
                    readStore.Load();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = readStore.Value != null && readStore.Value.Records.Count == recordCount;
                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Json + GZip 流压缩)",
                        DatasetType = "超大对象集合",
                        ElementCount = recordCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "行式 JSON 经 GZip 流式压缩管道，体积显著减小"
                    };
                }),

                // 4. ApeFree.DataStore + Jbin (行式存储)
                new BenchmarkStep("ApeFree.DataStore (Jbin 原生行式存储)", () =>
                {
                    string path = Path.Combine(tempDir, "objects_datastore_jbin_row.jbin");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = new JbinSerializationAdapter()
                    };

                    var store = StoreFactory.Factory.CreateLocalStore<DeviceLogBatch>(settings, () => batch);

                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<DeviceLogBatch>(settings);
                    sw.Restart();
                    readStore.Load();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = readStore.Value != null && readStore.Value.Records.Count == recordCount;
                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Jbin 原生行式存储)",
                        DatasetType = "超大对象集合",
                        ElementCount = recordCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "Jbin 原生反射与缓存契约直接存储"
                    };
                }),

                // 5. ApeFree.DataStore + Jbin 列式存储 (TransposeToDictionary 表达式树转置)
                new BenchmarkStep("ApeFree.DataStore (Jbin 列式存储 Transposition)", () =>
                {
                    string path = Path.Combine(tempDir, "objects_datastore_jbin_columnar.jbin");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = new JbinSerializationAdapter()
                    };

                    // 执行表达式树预编译转置：行转列
                    var columnarBatch = ColumnarDeviceLogBatch.FromRowBatch(batch);
                    var store = StoreFactory.Factory.CreateLocalStore<ColumnarDeviceLogBatch>(settings, () => columnarBatch);

                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<ColumnarDeviceLogBatch>(settings);
                    sw.Restart();
                    readStore.Load();
                    // 列转行还原
                    var restoredRecords = readStore.Value.RestoreToRecords();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = restoredRecords != null && restoredRecords.Count == recordCount &&
                                    restoredRecords[0].DeviceId == batch.Records[0].DeviceId &&
                                    restoredRecords[recordCount - 1].Id == batch.Records[recordCount - 1].Id;

                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Jbin 列式存储 Transposition)",
                        DatasetType = "超大对象集合",
                        ElementCount = recordCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "通过 Jbin TransposeToDictionary 表达式树转置按列聚合为连续数组，消除键名冗余"
                    };
                }),

                // 6. ApeFree.DataStore + Jbin 列式存储 + 紧凑数值编码
                new BenchmarkStep("ApeFree.DataStore (Jbin 列式紧凑编码)", () =>
                {
                    string path = Path.Combine(tempDir, "objects_datastore_jbin_columnar_alg.jbin");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = JbinSerializationAdapter.CreateWithPrimitiveCompression(
                            singleMode: SingleArrayCompressMode.Gorilla,
                            doubleMode: DoubleArrayCompressMode.Gorilla,
                            int32Mode: Int32ArrayCompressMode.FrameOfReference,
                            int64Mode: Int64ArrayCompressMode.Simple8b)
                    };

                    var columnarBatch = ColumnarDeviceLogBatch.FromRowBatch(batch);
                    var store = StoreFactory.Factory.CreateLocalStore<ColumnarDeviceLogBatch>(settings, () => columnarBatch);

                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<ColumnarDeviceLogBatch>(settings);
                    sw.Restart();
                    readStore.Load();
                    var restoredRecords = readStore.Value.RestoreToRecords();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = restoredRecords != null && restoredRecords.Count == recordCount &&
                                    restoredRecords[0].DeviceId == batch.Records[0].DeviceId;

                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Jbin 列式紧凑编码)",
                        DatasetType = "超大对象集合",
                        ElementCount = recordCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "列式属性数组应用紧凑编码，消除冗余并提升读取效率"
                    };
                }),

                // 7. ApeFree.DataStore + Jbin 列式存储 + Deflate 流压缩管道
                new BenchmarkStep("ApeFree.DataStore (Jbin 列式存储 + Deflate 流压缩)", () =>
                {
                    string path = Path.Combine(tempDir, "objects_datastore_jbin_columnar_deflate.dat");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = new JbinSerializationAdapter()
                    };
                    settings.StreamFilters.Add(new DeflateStreamFilter());

                    var columnarBatch = ColumnarDeviceLogBatch.FromRowBatch(batch);
                    var store = StoreFactory.Factory.CreateLocalStore<ColumnarDeviceLogBatch>(settings, () => columnarBatch);

                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<ColumnarDeviceLogBatch>(settings);
                    sw.Restart();
                    readStore.Load();
                    var restoredRecords = readStore.Value.RestoreToRecords();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = restoredRecords != null && restoredRecords.Count == recordCount &&
                                    restoredRecords[0].DeviceId == batch.Records[0].DeviceId;

                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Jbin 列式存储 + Deflate 流压缩)",
                        DatasetType = "超大对象集合",
                        ElementCount = recordCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "列式连续内存块经 Deflate 管道流式压缩，体积精简"
                    };
                }),

                // 8. ApeFree.DataStore + Jbin 列式存储 + GZip 流压缩管道
                new BenchmarkStep("ApeFree.DataStore (Jbin 列式存储 + GZip 极限压缩)", () =>
                {
                    string path = Path.Combine(tempDir, "objects_datastore_jbin_columnar_gzip.dat");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = new JbinSerializationAdapter()
                    };
                    settings.StreamFilters.Add(new GZipStreamFilter());

                    var columnarBatch = ColumnarDeviceLogBatch.FromRowBatch(batch);
                    var store = StoreFactory.Factory.CreateLocalStore<ColumnarDeviceLogBatch>(settings, () => columnarBatch);

                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<ColumnarDeviceLogBatch>(settings);
                    sw.Restart();
                    readStore.Load();
                    var restoredRecords = readStore.Value.RestoreToRecords();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = restoredRecords != null && restoredRecords.Count == recordCount &&
                                    restoredRecords[0].DeviceId == batch.Records[0].DeviceId;

                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Jbin 列式存储 + GZip 极限压缩)",
                        DatasetType = "超大对象集合",
                        ElementCount = recordCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "列式连续内存块经 GZip 管道压缩，大幅降低存储体积并保持快速还原能力"
                    };
                })
            };

            for (int i = 0; i < steps.Count; i++)
            {
                progressAction?.Invoke(i + 1, steps.Count, steps[i].Name);
                logAction?.Invoke($"[{i + 1}/{steps.Count}] 正在评测: {steps[i].Name}...");
                var r = steps[i].Runner();
                results.Add(r);
                logAction?.Invoke($"  -> 完成: 写入={r.WriteElapsedMs:F1}ms, 读取={r.ReadElapsedMs:F1}ms, 体积={r.FormattedFileSize}, 相对JSON缩减={r.SizeReductionPercent:F1}%, 校验={r.IsVerified}");
            }

            return results;
        }

        #endregion

        #region 3. 多通道时序数据高效持久化评测 (工业物联网多通道遥测时序数据集)

        /// <summary>
        /// 运行时序数据存储与流压缩开销对比评测
        /// 数据结构：IndustrialTelemetryDataset (包含递增 long[]、分组 int[]、连续平滑 float[]/double[]、枚举 string[])
        /// </summary>
        public List<BigDataBenchmarkResult> RunJbinExclusiveTelemetryBenchmark(
            int recordCount,
            Action<string> logAction = null,
            Action<int, int, string> progressAction = null)
        {
            var results = new List<BigDataBenchmarkResult>();
            logAction?.Invoke($"\r\n================ 开始评测: 工业遥测多通道时序数据集 ({recordCount:N0} 记录) ================");
            logAction?.Invoke("正在内存中构建工业遥测多通道时序数据 (Timestamps/Voltages/Temps/Pressures/Alarms)...");

            var dataset = IndustrialTelemetryDataset.Generate(recordCount);
            long baselineJsonSize = 0;
            double jbinAloneTotalMs = 0;
            long jbinAloneSizeBytes = 0;

            // 预热 JIT 编译器与反射类型缓存，消除冷启动编译偏差
            {
                var warmupDs = IndustrialTelemetryDataset.Generate(50);
                using (var ms = new MemoryStream())
                {
                    var warmupAdapter = JbinSerializationAdapter.CreateWithPrimitiveCompression(
                        singleMode: SingleArrayCompressMode.Gorilla,
                        doubleMode: DoubleArrayCompressMode.Gorilla,
                        int32Mode: Int32ArrayCompressMode.FrameOfReference,
                        int64Mode: Int64ArrayCompressMode.Simple8b,
                        stringMode: StringArrayCompressMode.Dictionary);
                    warmupAdapter.Serialize(warmupDs, ms);
                    ms.Position = 0;
                    warmupAdapter.Deserialize<IndustrialTelemetryDataset>(ms);
                }
            }

            var steps = new List<BenchmarkStep>
            {
                // 1. 原生 Newtonsoft.Json
                new BenchmarkStep("原生 Newtonsoft.Json (纯文本格式)", () =>
                {
                    string path = Path.Combine(tempDir, "telemetry_native.json");
                    var sw = Stopwatch.StartNew();
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                    using (var swWriter = new StreamWriter(fs, Encoding.UTF8))
                    using (var jw = new JsonTextWriter(swWriter))
                    {
                        var serializer = new JsonSerializer();
                        serializer.Serialize(jw, dataset);
                    }
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;
                    baselineJsonSize = size;

                    sw.Restart();
                    IndustrialTelemetryDataset restored;
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
                    using (var sr = new StreamReader(fs, Encoding.UTF8))
                    using (var jr = new JsonTextReader(sr))
                    {
                        var serializer = new JsonSerializer();
                        restored = serializer.Deserialize<IndustrialTelemetryDataset>(jr);
                    }
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = restored != null && restored.RecordCount == recordCount &&
                                    restored.Timestamps.Length == recordCount &&
                                    restored.StatusDescriptions[0] == dataset.StatusDescriptions[0];

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "原生 Newtonsoft.Json (纯文本格式)",
                        DatasetType = "工业遥测时序",
                        ElementCount = recordCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = 0.0,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "文本基准：浮点与时间戳均序列化为文本字符，无压缩"
                    };
                }),

                // 2. 原生 XmlSerializer
                new BenchmarkStep("原生 XmlSerializer (标准 XML 格式)", () =>
                {
                    string path = Path.Combine(tempDir, "telemetry_native.xml");
                    var xmlSerializer = new XmlSerializer(typeof(IndustrialTelemetryDataset));
                    var sw = Stopwatch.StartNew();
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 65536))
                    {
                        xmlSerializer.Serialize(fs, dataset);
                    }
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    sw.Restart();
                    IndustrialTelemetryDataset restored;
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
                    {
                        restored = (IndustrialTelemetryDataset)xmlSerializer.Deserialize(fs);
                    }
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = restored != null && restored.RecordCount == recordCount;
                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "原生 XmlSerializer (标准 XML 格式)",
                        DatasetType = "工业遥测时序",
                        ElementCount = recordCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "XML 基准：具有冗长 XML 节点和属性标签"
                    };
                }),

                // 3. DataStore (Json + GZip 流压缩)
                new BenchmarkStep("ApeFree.DataStore (Json + GZip 流压缩)", () =>
                {
                    string path = Path.Combine(tempDir, "telemetry_json_gzip.dat");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = new JsonSerializationAdapter()
                    };
                    settings.StreamFilters.Add(new GZipStreamFilter(System.IO.Compression.CompressionLevel.Optimal));

                    var store = StoreFactory.Factory.CreateLocalStore<IndustrialTelemetryDataset>(settings, () => dataset);
                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<IndustrialTelemetryDataset>(settings);
                    sw.Restart();
                    readStore.Load();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = readStore.Value != null && readStore.Value.RecordCount == recordCount &&
                                    readStore.Value.StatusDescriptions[0] == dataset.StatusDescriptions[0];

                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Json + GZip 流压缩)",
                        DatasetType = "工业遥测时序",
                        ElementCount = recordCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "文本经 GZip 压缩：体积虽缩小，但文本格式化与流状态机导致耗时较高"
                    };
                }),

                // 4. DataStore (Jbin 原生二进制零拷贝 Raw)
                new BenchmarkStep("ApeFree.DataStore (Jbin 原生二进制零拷贝 Raw)", () =>
                {
                    string path = Path.Combine(tempDir, "telemetry_jbin_raw.dat");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = new JbinSerializationAdapter()
                    };

                    var store = StoreFactory.Factory.CreateLocalStore<IndustrialTelemetryDataset>(settings, () => dataset);
                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<IndustrialTelemetryDataset>(settings);
                    sw.Restart();
                    readStore.Load();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = readStore.Value != null && readStore.Value.RecordCount == recordCount;
                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Jbin 原生二进制零拷贝 Raw)",
                        DatasetType = "工业遥测时序",
                        ElementCount = recordCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "原生内存映射：仅执行 Buffer.BlockCopy 零拷贝，未启用算法压缩"
                    };
                }),

                // 5. DataStore (Jbin 紧凑模式) ⭐
                new BenchmarkStep("ApeFree.DataStore (Jbin 紧凑模式)", () =>
                {
                    string path = Path.Combine(tempDir, "telemetry_jbin_exclusive.dat");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = JbinSerializationAdapter.CreateWithPrimitiveCompression(
                            singleMode: SingleArrayCompressMode.Gorilla,
                            doubleMode: DoubleArrayCompressMode.Gorilla,
                            int32Mode: Int32ArrayCompressMode.FrameOfReference,
                            int64Mode: Int64ArrayCompressMode.Simple8b,
                            stringMode: StringArrayCompressMode.Dictionary)
                    };

                    var store = StoreFactory.Factory.CreateLocalStore<IndustrialTelemetryDataset>(settings, () => dataset);
                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<IndustrialTelemetryDataset>(settings);
                    sw.Restart();
                    readStore.Load();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = readStore.Value != null && readStore.Value.RecordCount == recordCount &&
                                    Math.Abs(readStore.Value.Temperatures[0] - dataset.Temperatures[0]) < 0.001f &&
                                    readStore.Value.StatusDescriptions[0] == dataset.StatusDescriptions[0];

                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;

                    jbinAloneTotalMs = writeMs + readMs;
                    jbinAloneSizeBytes = size;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Jbin 紧凑模式)",
                        DatasetType = "工业遥测时序",
                        ElementCount = recordCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = "⭐ 黄金平衡点：数值与字符串紧凑编码，无需外层流过滤器"
                    };
                }),

                // 6. DataStore (Jbin 紧凑模式 + GZip 复合流压缩)
                new BenchmarkStep("ApeFree.DataStore (Jbin 紧凑模式 + GZip 流压缩)", () =>
                {
                    string path = Path.Combine(tempDir, "telemetry_jbin_gzip_redundant.dat");
                    var settings = new LocalStoreAccessSettings(path)
                    {
                        SerializationAdapter = JbinSerializationAdapter.CreateWithPrimitiveCompression(
                            singleMode: SingleArrayCompressMode.Gorilla,
                            doubleMode: DoubleArrayCompressMode.Gorilla,
                            int32Mode: Int32ArrayCompressMode.FrameOfReference,
                            int64Mode: Int64ArrayCompressMode.Simple8b,
                            stringMode: StringArrayCompressMode.Dictionary)
                    };
                    settings.StreamFilters.Add(new GZipStreamFilter(System.IO.Compression.CompressionLevel.Optimal));

                    var store = StoreFactory.Factory.CreateLocalStore<IndustrialTelemetryDataset>(settings, () => dataset);
                    var sw = Stopwatch.StartNew();
                    store.Load();
                    sw.Stop();
                    double writeMs = sw.Elapsed.TotalMilliseconds;
                    long size = new FileInfo(path).Length;

                    var readStore = StoreFactory.Factory.CreateLocalStore<IndustrialTelemetryDataset>(settings);
                    sw.Restart();
                    readStore.Load();
                    sw.Stop();
                    double readMs = sw.Elapsed.TotalMilliseconds;

                    bool verified = readStore.Value != null && readStore.Value.RecordCount == recordCount &&
                                    Math.Abs(readStore.Value.Temperatures[0] - dataset.Temperatures[0]) < 0.001f &&
                                    readStore.Value.StatusDescriptions[0] == dataset.StatusDescriptions[0];

                    double reduction = baselineJsonSize > 0 ? (1.0 - (double)size / baselineJsonSize) * 100.0 : 0;
                    double totalMs = writeMs + readMs;

                    double extraTimePercent = jbinAloneTotalMs > 0 ? ((totalMs - jbinAloneTotalMs) / jbinAloneTotalMs) * 100.0 : 0;
                    double extraSavedBytes = jbinAloneSizeBytes - size;
                    double extraSavedPercent = jbinAloneSizeBytes > 0 ? ((double)extraSavedBytes / jbinAloneSizeBytes) * 100.0 : 0;

                    return new BigDataBenchmarkResult
                    {
                        StrategyName = "ApeFree.DataStore (Jbin 紧凑模式 + GZip 流压缩)",
                        DatasetType = "工业遥测时序",
                        ElementCount = recordCount,
                        WriteElapsedMs = writeMs,
                        ReadElapsedMs = readMs,
                        FileSizeBytes = size,
                        SizeReductionPercent = reduction,
                        WriteThroughputMBs = (size / (1024.0 * 1024.0)) / (writeMs / 1000.0),
                        IsVerified = verified,
                        Notes = $"开销对比：叠加 GZip 额外耗时增加 {extraTimePercent:F1}%，但体积仅微缩 {extraSavedPercent:F1}%，边际收益较低"
                    };
                })
            };

            for (int i = 0; i < steps.Count; i++)
            {
                progressAction?.Invoke(i + 1, steps.Count, steps[i].Name);
                logAction?.Invoke($"[{i + 1}/{steps.Count}] 正在评测: {steps[i].Name}...");
                var r = steps[i].Runner();
                results.Add(r);
                logAction?.Invoke($"  -> 完成: 写入={r.WriteElapsedMs:F1}ms, 读取={r.ReadElapsedMs:F1}ms, 体积={r.FormattedFileSize}, 相对JSON缩减={r.SizeReductionPercent:F1}%, 校验={r.IsVerified}");
            }

            return results;
        }

        #endregion

        public void CleanUp()
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch { }
        }
    }
}
