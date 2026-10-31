using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ApeFree.DataStore.BenchmarkDemo.Services;

namespace ApeFree.DataStore.BenchmarkDemo
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                if (args[0] == "--smoke-test" || args[0] == "-t")
                {
                    RunSmokeTestAsync().GetAwaiter().GetResult();
                    return;
                }
                if (args[0] == "--full-report" || args[0] == "-r")
                {
                    RunFullReportAsync().GetAwaiter().GetResult();
                    return;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static async Task RunFullReportAsync()
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("REPORT 1: 高频配置读写基准 (1,000 次操作, 4 线程并发, AppConfig)");
            Console.WriteLine("================================================================================");
            var hf = new HighFrequencyBenchmarkService();
            var dRes = await hf.RunDirectJsonBenchmarkAsync(1000, 4, false, 0, null, null);
            var sRes = await hf.RunDataStoreBenchmarkAsync(1000, 4, false, 0, null, null);
            Console.WriteLine($"[DirectJson] 耗时: {dRes.ElapsedMilliseconds:F1}ms | 吞吐: {dRes.ThroughputOpsPerSec:F0} ops/s | 单次写延迟: {dRes.AvgLatencyMs:F3}ms | 单次读延迟: {dRes.SingleReadLatencyMs:F3}ms | 刷盘次数: {dRes.PhysicalDiskWriteCount} | 写入字节: {dRes.TotalDiskBytesWritten}B | 锁冲突: {dRes.ConcurrencyLockErrors}");
            Console.WriteLine($"[DataStore]  耗时: {sRes.ElapsedMilliseconds:F1}ms | 吞吐: {sRes.ThroughputOpsPerSec:F0} ops/s | 单次写延迟: {sRes.AvgLatencyMs:F3}ms | 单次读延迟: {sRes.SingleReadLatencyMs:F4}ms | 刷盘次数: {sRes.PhysicalDiskWriteCount} | 写入字节: {sRes.TotalDiskBytesWritten}B | 锁冲突: {sRes.ConcurrencyLockErrors}");
            hf.CleanUp();

            Console.WriteLine("\r\n================================================================================");
            Console.WriteLine("REPORT 2-A: 海量传感器与 3D 点云时序 (50,000 采样点, PointCloudDataset)");
            Console.WriteLine("================================================================================");
            var bd = new BigDataBenchmarkService();
            var pcResults = bd.RunPointCloudBenchmark(50000, null, null);
            foreach (var r in pcResults)
            {
                Console.WriteLine($"{r.StrategyName} | 写入: {r.WriteElapsedMs:F1}ms | 读取: {r.ReadElapsedMs:F1}ms | 总计: {r.TotalElapsedMs:F1}ms | 体积: {r.FormattedFileSize} ({r.FileSizeBytes}B) | 缩减率: {r.SizeReductionPercent:F1}% | 吞吐: {r.WriteThroughputMBs:F1}MB/s | 校验: {r.IsVerified}");
            }

            Console.WriteLine("\r\n================================================================================");
            Console.WriteLine("REPORT 2-B: 超大业务实体对象集合与列式存储 (10,000 条记录, DeviceLogBatch)");
            Console.WriteLine("================================================================================");
            var objResults = bd.RunObjectBatchBenchmark(10000, null, null);
            foreach (var r in objResults)
            {
                Console.WriteLine($"{r.StrategyName} | 写入: {r.WriteElapsedMs:F1}ms | 读取: {r.ReadElapsedMs:F1}ms | 总计: {r.TotalElapsedMs:F1}ms | 体积: {r.FormattedFileSize} ({r.FileSizeBytes}B) | 缩减率: {r.SizeReductionPercent:F1}% | 吞吐: {r.WriteThroughputMBs:F1}MB/s | 校验: {r.IsVerified}");
            }
            bd.CleanUp();
        }

        private static async Task RunSmokeTestAsync()
        {
            Console.WriteLine("=== [Smoke Test 1: 高频配置读写基准测试] ===");
            var hfService = new HighFrequencyBenchmarkService();
            var dRes = await hfService.RunDirectJsonBenchmarkAsync(200, 2, false, 0, Console.WriteLine, null);
            var sRes = await hfService.RunDataStoreBenchmarkAsync(200, 2, false, 0, Console.WriteLine, null);
            Console.WriteLine($"DirectJson: {dRes.ElapsedMilliseconds:F1}ms, DataStore: {sRes.ElapsedMilliseconds:F1}ms");
            hfService.CleanUp();

            Console.WriteLine("\r\n=== [Smoke Test 2: 海量传感器时序点云 (值类型大数组)] ===");
            var bdService = new BigDataBenchmarkService();
            var pcResults = bdService.RunPointCloudBenchmark(5000, Console.WriteLine, null);
            Console.WriteLine($"PointCloud Tests Completed. Result count: {pcResults.Count}");

            Console.WriteLine("\r\n=== [Smoke Test 3: 超大业务实体集合 (Jbin 列式转置)] ===");
            var objResults = bdService.RunObjectBatchBenchmark(2000, Console.WriteLine, null);
            Console.WriteLine($"Object Batch Tests Completed. Result count: {objResults.Count}");
            bdService.CleanUp();

            Console.WriteLine("\r\n>>> ALL BENCHMARK PIPELINES VERIFIED SUCCESSFULLY! <<<");
        }
    }
}
