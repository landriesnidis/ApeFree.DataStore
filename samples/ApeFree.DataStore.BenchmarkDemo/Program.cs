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
            if (args != null && args.Length > 0 && (args[0] == "--smoke-test" || args[0] == "-t"))
            {
                RunSmokeTestAsync().GetAwaiter().GetResult();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
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
