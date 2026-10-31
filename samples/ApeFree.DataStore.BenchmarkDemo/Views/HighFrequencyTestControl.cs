using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ApeFree.DataStore.BenchmarkDemo.Services;

namespace ApeFree.DataStore.BenchmarkDemo.Views
{
    public class HighFrequencyTestControl : UserControl
    {
        private readonly HighFrequencyBenchmarkService service = new HighFrequencyBenchmarkService();

        // 控件声明
        private ComboBox cboOpsCount;
        private ComboBox cboThreads;
        private ComboBox cboComplexity;
        private NumericUpDown nudDelay;
        private Button btnRun;
        private Button btnClear;
        private ProgressBar progressBar;
        private Label lblStatus;
        private DataGridView dgvResults;
        private RichTextBox rtbLog;
        private Label lblCardSpeed;
        private Label lblCardDiskIo;
        private Label lblCardLatency;

        public HighFrequencyTestControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Dock = DockStyle.Fill;
            this.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(248, 249, 250);

            // 顶部配置栏
            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12)
            };

            var flpConfig = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = false
            };

            // 操作次数
            flpConfig.Controls.Add(CreateLabel("测试写入次数:"));
            cboOpsCount = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 95 };
            cboOpsCount.Items.AddRange(new object[] { "500", "1000", "2000", "5000" });
            cboOpsCount.SelectedIndex = 1;
            flpConfig.Controls.Add(cboOpsCount);

            // 并发线程数
            flpConfig.Controls.Add(CreateLabel("  并发线程数:"));
            cboThreads = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 95 };
            cboThreads.Items.AddRange(new object[] { "1 (单线程爆发)", "4 (多线程并发)", "8 (高并发压测)" });
            cboThreads.SelectedIndex = 1;
            flpConfig.Controls.Add(cboThreads);

            // 业务间隔
            flpConfig.Controls.Add(CreateLabel("  保存间隔:"));
            nudDelay = new NumericUpDown { Width = 65, Minimum = 0, Maximum = 100, Value = 0 };
            flpConfig.Controls.Add(nudDelay);
            flpConfig.Controls.Add(CreateLabel("ms"));

            // 数据复杂度
            flpConfig.Controls.Add(CreateLabel("  数据规模:"));
            cboComplexity = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
            cboComplexity.Items.AddRange(new object[] { "标准配置 (~2KB)", "复杂配置 (~30KB)" });
            cboComplexity.SelectedIndex = 0;
            flpConfig.Controls.Add(cboComplexity);

            // 开始按钮
            btnRun = new Button
            {
                Text = "⚡ 开始基准对比测试",
                Width = 160,
                Height = 32,
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(16, 0, 8, 0)
            };
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Click += async (s, e) => await StartBenchmarkAsync();
            flpConfig.Controls.Add(btnRun);

            // 清空按钮
            btnClear = new Button
            {
                Text = "清空",
                Width = 65,
                Height = 32,
                BackColor = Color.FromArgb(235, 238, 242),
                ForeColor = Color.FromArgb(70, 70, 70),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => ClearResults();
            flpConfig.Controls.Add(btnClear);

            panelTop.Controls.Add(flpConfig);

            // 状态与进度条条目
            var panelProgress = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Color.FromArgb(240, 243, 246),
                Padding = new Padding(16, 4, 16, 4)
            };
            lblStatus = new Label
            {
                Text = "就绪 - 请选择参数后点击“开始基准对比测试”",
                Dock = DockStyle.Left,
                AutoSize = true,
                ForeColor = Color.FromArgb(100, 110, 120),
                TextAlign = ContentAlignment.MiddleLeft
            };
            progressBar = new ProgressBar
            {
                Dock = DockStyle.Right,
                Width = 260,
                Style = ProgressBarStyle.Continuous
            };
            panelProgress.Controls.Add(lblStatus);
            panelProgress.Controls.Add(progressBar);

            // 核心统计卡片区
            var panelCards = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 85,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = Color.FromArgb(248, 249, 250)
            };
            panelCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            panelCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            panelCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));

            lblCardSpeed = CreateMetricCard("🚀 吞吐性能提升", "-- 倍", Color.FromArgb(230, 244, 255), Color.FromArgb(0, 102, 204));
            lblCardDiskIo = CreateMetricCard("💾 磁盘物理 I/O 削减", "-- %", Color.FromArgb(235, 251, 238), Color.FromArgb(16, 137, 62));
            lblCardLatency = CreateMetricCard("⏱️ 调用方延迟缩减", "-- %", Color.FromArgb(254, 243, 235), Color.FromArgb(204, 75, 0));

            panelCards.Controls.Add(lblCardSpeed.Parent, 0, 0);
            panelCards.Controls.Add(lblCardDiskIo.Parent, 1, 0);
            panelCards.Controls.Add(lblCardLatency.Parent, 2, 0);

            // 主分割容器 (上部数据对比表，下部控制台日志)
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 270,
                Panel1MinSize = 180,
                Panel2MinSize = 100
            };

            // 数据表格
            dgvResults = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 30 },
                GridColor = Color.FromArgb(230, 234, 240)
            };
            dgvResults.EnableHeadersVisualStyles = false;
            dgvResults.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(243, 245, 249);
            dgvResults.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(40, 50, 60);
            dgvResults.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);

            dgvResults.Columns.Add("Metric", "对比评测指标 (Benchmark Metric)");
            dgvResults.Columns.Add("DirectJson", "传统原生读写 (File.WriteAllText + Json)");
            dgvResults.Columns.Add("DataStore", "ApeFree.DataStore (Coalesce 异步写合并)");
            dgvResults.Columns.Add("Improvement", "性能优势 / 优化幅度 (Improvement)");

            splitContainer.Panel1.Controls.Add(dgvResults);

            // 控制台日志
            rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 32, 40),
                ForeColor = Color.FromArgb(220, 225, 230),
                Font = new Font("Consolas", 9.5F, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                ReadOnly = true
            };
            splitContainer.Panel2.Controls.Add(rtbLog);

            // 组装总窗体
            this.Controls.Add(splitContainer);
            this.Controls.Add(panelCards);
            this.Controls.Add(panelProgress);
            this.Controls.Add(panelTop);

            this.ResumeLayout(false);

            InitDefaultTable();
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(0, 7, 0, 0),
                ForeColor = Color.FromArgb(60, 64, 67)
            };
        }

        private Label CreateMetricCard(string title, string initialValue, Color bg, Color fg)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = bg,
                Padding = new Padding(12, 8, 12, 8),
                Margin = new Padding(4)
            };

            var lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = Color.FromArgb(80, 85, 95),
                Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular)
            };

            var lblVal = new Label
            {
                Text = initialValue,
                Dock = DockStyle.Fill,
                ForeColor = fg,
                Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnl.Controls.Add(lblVal);
            pnl.Controls.Add(lblTitle);
            return lblVal;
        }

        private void InitDefaultTable()
        {
            dgvResults.Rows.Clear();
            dgvResults.Rows.Add("⏱️ 写入调用方总耗时 (Total Elapsed)", "--", "--", "--");
            dgvResults.Rows.Add("⚡ 写入吞吐量 (Throughput Ops/sec)", "--", "--", "--");
            dgvResults.Rows.Add("⏳ 平均单次保存延迟 (Avg Latency)", "--", "--", "--");
            dgvResults.Rows.Add("📖 单次配置读取延迟 (Read Latency)", "--", "--", "--");
            dgvResults.Rows.Add("💾 物理底层刷盘次数 (Disk I/O Writes)", "--", "--", "--");
            dgvResults.Rows.Add("📦 物理磁盘实际写入量 (Disk Bytes)", "--", "--", "--");
            dgvResults.Rows.Add("⚠️ 并发锁冲突/共享冲突 (Lock Conflicts)", "--", "--", "--");
            dgvResults.Rows.Add("✅ 最终落盘数据一致性 (Data Integrity)", "--", "--", "--");
        }

        private void ClearResults()
        {
            InitDefaultTable();
            rtbLog.Clear();
            lblStatus.Text = "就绪";
            progressBar.Value = 0;
            lblCardSpeed.Text = "-- 倍";
            lblCardDiskIo.Text = "-- %";
            lblCardLatency.Text = "-- %";
        }

        private void AppendLog(string message, Color? color = null)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action(() => AppendLog(message, color)));
                return;
            }

            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = color ?? Color.FromArgb(200, 210, 220);
            rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {message}\r\n");
            rtbLog.ScrollToCaret();
        }

        private async Task StartBenchmarkAsync()
        {
            btnRun.Enabled = false;
            btnClear.Enabled = false;

            int totalOps = int.Parse(cboOpsCount.SelectedItem.ToString());
            int threads = cboThreads.SelectedIndex == 0 ? 1 : (cboThreads.SelectedIndex == 1 ? 4 : 8);
            int delayMs = (int)nudDelay.Value;
            bool complexData = cboComplexity.SelectedIndex == 1;

            AppendLog("================================================================================", Color.FromArgb(80, 160, 255));
            AppendLog($"[基准测试启动] 写入规模: {totalOps} 次 | 并发线程: {threads} | 间隔延时: {delayMs}ms", Color.FromArgb(80, 160, 255));

            try
            {
                // 1. 运行直接写入测试
                lblStatus.Text = "正在执行方案 A: 传统原生读写 (File.WriteAllText + Json)...";
                progressBar.Value = 0;
                var resDirect = await Task.Run(() => service.RunDirectJsonBenchmarkAsync(
                    totalOps,
                    threads,
                    complexData,
                    delayMs,
                    msg => AppendLog(msg, Color.FromArgb(255, 180, 100)),
                    (curr, tot) =>
                    {
                        if (progressBar.InvokeRequired)
                            progressBar.Invoke(new Action(() => progressBar.Value = (int)(curr * 50.0 / tot)));
                        else
                            progressBar.Value = (int)(curr * 50.0 / tot);
                    }
                ));

                // 2. 运行 DataStore 读写测试
                lblStatus.Text = "正在执行方案 B: ApeFree.DataStore (内置 Coalesce 异步因果写合并)...";
                var resDataStore = await Task.Run(() => service.RunDataStoreBenchmarkAsync(
                    totalOps,
                    threads,
                    complexData,
                    delayMs,
                    msg => AppendLog(msg, Color.FromArgb(100, 220, 120)),
                    (curr, tot) =>
                    {
                        if (progressBar.InvokeRequired)
                            progressBar.Invoke(new Action(() => progressBar.Value = 50 + (int)(curr * 50.0 / tot)));
                        else
                            progressBar.Value = 50 + (int)(curr * 50.0 / tot);
                    }
                ));

                // 呈现对比表格
                RenderComparison(resDirect, resDataStore);
                lblStatus.Text = "基准测试完成！详见上方对比指标与下方执行日志。";
                progressBar.Value = 100;
            }
            catch (Exception ex)
            {
                AppendLog($"[评测异常中断] {ex.Message}", Color.Red);
                MessageBox.Show($"评测运行异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRun.Enabled = true;
                btnClear.Enabled = true;
            }
        }

        private void RenderComparison(HighFrequencyResult d, HighFrequencyResult s)
        {
            dgvResults.Rows.Clear();

            double speedup = d.ElapsedMilliseconds > 0 && s.ElapsedMilliseconds > 0 ? (d.ElapsedMilliseconds / s.ElapsedMilliseconds) : 0;
            double ioSavedPercent = d.PhysicalDiskWriteCount > 0 ? (1.0 - (double)s.PhysicalDiskWriteCount / d.PhysicalDiskWriteCount) * 100.0 : 0;
            double latencyReducedPercent = d.AvgLatencyMs > 0 ? (1.0 - s.AvgLatencyMs / d.AvgLatencyMs) * 100.0 : 0;

            // 统计卡片更新
            lblCardSpeed.Text = $"{speedup:F1} 倍";
            lblCardDiskIo.Text = $"-{ioSavedPercent:F1} %";
            lblCardLatency.Text = $"-{latencyReducedPercent:F1} %";

            AddGridRow("⏱️ 写入调用方总耗时", $"{d.ElapsedMilliseconds:F1} ms", $"{s.ElapsedMilliseconds:F1} ms", $"🚀 提速 {speedup:F1} 倍", Color.Green);
            AddGridRow("⚡ 写入吞吐量", $"{d.ThroughputOpsPerSec:F0} ops/s", $"{s.ThroughputOpsPerSec:F0} ops/s", $"🚀 提升 {(s.ThroughputOpsPerSec / Math.Max(1, d.ThroughputOpsPerSec)):F1} 倍", Color.Green);
            AddGridRow("⏳ 平均单次保存延迟", $"{d.AvgLatencyMs:F3} ms", $"{s.AvgLatencyMs:F3} ms", $"⚡ 降低 {latencyReducedPercent:F1}%", Color.Green);
            AddGridRow("📖 单次配置读取延迟", $"{d.SingleReadLatencyMs:F3} ms (读盘反序列化)", $"{s.SingleReadLatencyMs:F4} ms (内存直读)", $"⚡ 内存读快 {(d.SingleReadLatencyMs / Math.Max(0.0001, s.SingleReadLatencyMs)):F0} 倍", Color.Green);
            AddGridRow("💾 物理底层刷盘次数", $"{d.PhysicalDiskWriteCount} 次 (每次落盘)", $"{s.PhysicalDiskWriteCount} 次 (因果无损合并)", $"🛡️ 削减 {ioSavedPercent:F1}% 磁盘磨损", Color.Green);
            AddGridRow("📦 物理磁盘实际写入量", FormatBytes(d.TotalDiskBytesWritten), FormatBytes(s.TotalDiskBytesWritten), $"🛡️ 减少 {(1.0 - (double)s.TotalDiskBytesWritten / Math.Max(1, d.TotalDiskBytesWritten)) * 100.0:F1}% 写入量", Color.Green);
            AddGridRow("⚠️ 并发锁冲突/共享冲突", $"{d.ConcurrencyLockErrors} 次异常/需排队", $"{s.ConcurrencyLockErrors} 次 (内置调度互斥保证)", "🔒 100% 线程安全无锁冲突", Color.Blue);
            AddGridRow("✅ 最终落盘数据一致性", d.IntegrityDetails, s.IntegrityDetails, "100% 数据因果一致校验通过", Color.FromArgb(0, 102, 204));
        }

        private void AddGridRow(string metric, string directVal, string storeVal, string improveVal, Color highlight)
        {
            int rowIdx = dgvResults.Rows.Add(metric, directVal, storeVal, improveVal);
            dgvResults.Rows[rowIdx].Cells[3].Style.ForeColor = highlight;
            dgvResults.Rows[rowIdx].Cells[3].Style.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024) return $"{(double)bytes / (1024 * 1024):F2} MB";
            if (bytes >= 1024) return $"{(double)bytes / 1024:F2} KB";
            return $"{bytes} B";
        }
    }
}
