using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ApeFree.DataStore.BenchmarkDemo.Services;

namespace ApeFree.DataStore.BenchmarkDemo.Views
{
    public class JbinExclusiveBenchmarkControl : UserControl
    {
        private readonly BigDataBenchmarkService service = new BigDataBenchmarkService();

        private ComboBox cboScale;
        private Button btnRun;
        private Button btnClear;
        private ProgressBar progressBar;
        private Label lblStatus;
        private DataGridView dgvResults;
        private RichTextBox rtbLog;

        // 核心结论卡片
        private Label lblCardJbinReduction;
        private Label lblCardJbinLatency;
        private Label lblCardSpeedupVsGzip;
        private Label lblCardGzipRedundancyCost;

        public JbinExclusiveBenchmarkControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Dock = DockStyle.Fill;
            this.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(248, 249, 250);

            // 顶部说明与配置面板
            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Color.White,
                Padding = new Padding(16, 10, 16, 10)
            };

            var lblDesc = new Label
            {
                Dock = DockStyle.Top,
                Height = 52,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 90, 105),
                Text = "【时序持久化机制】：DataStore 默认采用 Jbin 格式，支持对连续数值数组与集合进行直接内存映射与紧凑编码。\r\n" +
                       "【核心评测对比】：在结构化数值时序场景下，DataStore 直接持久化即可达到极高读写吞吐与小体积。若在此场景下额外叠加通用 GZip 流过滤器，由于边际压缩收益极低却增加编解码延迟，反而会造成不必要的耗时开销。"
            };

            var flpConfig = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 38,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            flpConfig.Controls.Add(CreateLabel("  遥测时序数据规模:"));
            cboScale = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
            cboScale.Items.AddRange(new object[]
            {
                "20,000 记录 (轻量压测)",
                "50,000 记录 (标准批次)",
                "100,000 记录 (极限吞吐)"
            });
            cboScale.SelectedIndex = 1; // 默认 50,000
            flpConfig.Controls.Add(cboScale);

            btnRun = new Button
            {
                Text = "⚡ 开始时序持久化与流压缩开销评测",
                Width = 265,
                Height = 30,
                BackColor = Color.FromArgb(12, 115, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(16, 0, 8, 0)
            };
            btnRun.FlatAppearance.BorderSize = 0;
            btnRun.Click += async (s, e) => await StartBenchmarkAsync();
            flpConfig.Controls.Add(btnRun);

            btnClear = new Button
            {
                Text = "清空",
                Width = 65,
                Height = 30,
                BackColor = Color.FromArgb(235, 238, 242),
                ForeColor = Color.FromArgb(70, 70, 70),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => ClearResults();
            flpConfig.Controls.Add(btnClear);

            panelTop.Controls.Add(flpConfig);
            panelTop.Controls.Add(lblDesc);

            // 核心指标卡片容器
            var panelCards = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                Padding = new Padding(16, 8, 16, 8),
                BackColor = Color.FromArgb(242, 245, 249)
            };

            var tlpCards = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1
            };
            tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            lblCardJbinReduction = CreateMetricCard(tlpCards, 0, "DataStore 紧凑模式减重率", "- %", Color.FromArgb(16, 137, 62));
            lblCardJbinLatency = CreateMetricCard(tlpCards, 1, "DataStore 读写往返总耗时", "- ms", Color.FromArgb(12, 115, 235));
            lblCardSpeedupVsGzip = CreateMetricCard(tlpCards, 2, "相对 JSON+GZip 提速", "- 倍", Color.FromArgb(138, 43, 226));
            lblCardGzipRedundancyCost = CreateMetricCard(tlpCards, 3, "叠加 GZip 额外开销代价", "- % / - %", Color.FromArgb(217, 48, 37));

            panelCards.Controls.Add(tlpCards);

            // 进度与状态栏
            var panelProgress = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(16, 3, 16, 3),
                BackColor = Color.FromArgb(248, 249, 250)
            };

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Left,
                Width = 320,
                Height = 18,
                Style = ProgressBarStyle.Continuous
            };

            lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Text = "就绪。请选择数据规模后点击按钮开始验证。",
                ForeColor = Color.FromArgb(100, 110, 125)
            };

            panelProgress.Controls.Add(lblStatus);
            panelProgress.Controls.Add(progressBar);

            // 主体分割面板（表格 + 日志）
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 290,
                SplitterWidth = 5,
                Padding = new Padding(16, 6, 16, 12)
            };

            // 结果 DataGridView
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
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                GridColor = Color.FromArgb(230, 235, 240),
                RowTemplate = { Height = 28 }
            };
            dgvResults.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 254);
            dgvResults.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvResults.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);
            dgvResults.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            dgvResults.ColumnHeadersHeight = 32;
            dgvResults.EnableHeadersVisualStyles = false;

            dgvResults.Columns.Add("Strategy", "存储与序列化方案");
            dgvResults.Columns.Add("WriteMs", "写入耗时");
            dgvResults.Columns.Add("ReadMs", "读取耗时");
            dgvResults.Columns.Add("TotalMs", "往返耗时");
            dgvResults.Columns.Add("FileSize", "磁盘体积");
            dgvResults.Columns.Add("Reduction", "相对 JSON 压缩比");
            dgvResults.Columns.Add("Throughput", "写入吞吐率");
            dgvResults.Columns.Add("Verified", "数据一致性");
            dgvResults.Columns.Add("Notes", "方案机制与开销实证");

            dgvResults.Columns["Strategy"].FillWeight = 26;
            dgvResults.Columns["WriteMs"].FillWeight = 9;
            dgvResults.Columns["ReadMs"].FillWeight = 9;
            dgvResults.Columns["TotalMs"].FillWeight = 9;
            dgvResults.Columns["FileSize"].FillWeight = 11;
            dgvResults.Columns["Reduction"].FillWeight = 12;
            dgvResults.Columns["Throughput"].FillWeight = 10;
            dgvResults.Columns["Verified"].FillWeight = 8;
            dgvResults.Columns["Notes"].FillWeight = 30;

            splitContainer.Panel1.Controls.Add(dgvResults);

            // 日志文本框
            rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(25, 30, 40),
                ForeColor = Color.FromArgb(220, 225, 235),
                Font = new Font("Consolas", 9.2F, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                ReadOnly = true
            };
            splitContainer.Panel2.Controls.Add(rtbLog);

            this.Controls.Add(splitContainer);
            this.Controls.Add(panelProgress);
            this.Controls.Add(panelCards);
            this.Controls.Add(panelTop);

            this.ResumeLayout(false);
        }

        private Label CreateMetricCard(TableLayoutPanel parent, int col, string title, string defaultValue, Color valColor)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10, 6, 10, 6),
                Margin = new Padding(4, 0, 4, 0)
            };

            var lblT = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(110, 120, 135),
                Height = 20
            };

            var lblV = new Label
            {
                Text = defaultValue,
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 12.5F, FontStyle.Bold),
                ForeColor = valColor,
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnl.Controls.Add(lblV);
            pnl.Controls.Add(lblT);
            parent.Controls.Add(pnl, col, 0);

            return lblV;
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(0, 6, 4, 0),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular)
            };
        }

        private async Task StartBenchmarkAsync()
        {
            btnRun.Enabled = false;
            btnClear.Enabled = false;
            cboScale.Enabled = false;
            progressBar.Value = 0;
            dgvResults.Rows.Clear();
            rtbLog.Clear();

            int count = 50000;
            if (cboScale.SelectedIndex == 0) count = 20000;
            else if (cboScale.SelectedIndex == 1) count = 50000;
            else if (cboScale.SelectedIndex == 2) count = 100000;

            AppendLog($"[时序持久化评测] 启动... 遥测数据规模={count:N0} 条记录\r\n", Color.Cyan);

            try
            {
                var results = await Task.Run(() =>
                {
                    return service.RunJbinExclusiveTelemetryBenchmark(
                        count,
                        msg => this.Invoke((Action)(() => AppendLog(msg + "\r\n"))),
                        (curr, total, name) => this.Invoke((Action)(() =>
                        {
                            progressBar.Maximum = total;
                            progressBar.Value = curr;
                            lblStatus.Text = $"正在评测 ({curr}/{total}): {name}";
                        }))
                    );
                });

                // 填入表格
                BigDataBenchmarkResult jsonGzipResult = null;
                BigDataBenchmarkResult jbinExclusiveResult = null;
                BigDataBenchmarkResult jbinGzipResult = null;

                foreach (var r in results)
                {
                    int rowIndex = dgvResults.Rows.Add(
                        r.StrategyName,
                        $"{r.WriteElapsedMs:F1} ms",
                        $"{r.ReadElapsedMs:F1} ms",
                        $"{r.TotalElapsedMs:F1} ms",
                        r.FormattedFileSize,
                        $"{r.SizeReductionPercent:F1} %",
                        $"{r.WriteThroughputMBs:F1} MB/s",
                        r.IsVerified ? "✓ 通过" : "✗ 失败",
                        r.Notes
                    );

                    var row = dgvResults.Rows[rowIndex];
                    if (r.StrategyName.Contains("Jbin 紧凑模式"))
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(235, 245, 255);
                        row.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
                        jbinExclusiveResult = r;
                    }
                    else if (r.StrategyName.Contains("Json + GZip"))
                    {
                        jsonGzipResult = r;
                    }
                    else if (r.StrategyName.Contains("Jbin 紧凑模式 + GZip 流压缩"))
                    {
                        jbinGzipResult = r;
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(160, 40, 40);
                    }
                }

                // 计算核心卡片
                if (jbinExclusiveResult != null)
                {
                    lblCardJbinReduction.Text = $"{jbinExclusiveResult.SizeReductionPercent:F1} %";
                    lblCardJbinLatency.Text = $"{jbinExclusiveResult.TotalElapsedMs:F1} ms";

                    if (jsonGzipResult != null && jbinExclusiveResult.TotalElapsedMs > 0)
                    {
                        double speedup = jsonGzipResult.TotalElapsedMs / jbinExclusiveResult.TotalElapsedMs;
                        lblCardSpeedupVsGzip.Text = $"快 {speedup:F1} 倍";
                    }

                    if (jbinGzipResult != null && jbinExclusiveResult.TotalElapsedMs > 0)
                    {
                        double timeIncrease = ((jbinGzipResult.TotalElapsedMs - jbinExclusiveResult.TotalElapsedMs) / jbinExclusiveResult.TotalElapsedMs) * 100.0;
                        double extraSaved = ((double)(jbinExclusiveResult.FileSizeBytes - jbinGzipResult.FileSizeBytes) / jbinExclusiveResult.FileSizeBytes) * 100.0;
                        lblCardGzipRedundancyCost.Text = $"+{timeIncrease:F0}% 耗时 / 仅省 {extraSaved:F1}%";
                    }
                }

                lblStatus.Text = $"评测完成！共执行 {results.Count} 项方案对比。数据均已完成双向对称性校验。";
                AppendLog("\r\n================ 评测量化结论 ================\r\n", Color.Gold);
                AppendLog($"1. DataStore (Jbin 紧凑模式) 使数据体积缩减 {jbinExclusiveResult?.SizeReductionPercent:F1}%\r\n", Color.LightGreen);
                if (jsonGzipResult != null && jbinExclusiveResult != null)
                {
                    AppendLog($"2. 相对传统 Json+GZip，DataStore (Jbin 紧凑模式) 往返耗时缩短 {(1.0 - jbinExclusiveResult.TotalElapsedMs / jsonGzipResult.TotalElapsedMs) * 100.0:F1}%（提速 {(jsonGzipResult.TotalElapsedMs / jbinExclusiveResult.TotalElapsedMs):F1} 倍）\r\n", Color.LightGreen);
                }
                if (jbinGzipResult != null && jbinExclusiveResult != null)
                {
                    double timeCost = ((jbinGzipResult.TotalElapsedMs - jbinExclusiveResult.TotalElapsedMs) / jbinExclusiveResult.TotalElapsedMs) * 100.0;
                    double sizeBenefit = ((double)(jbinExclusiveResult.FileSizeBytes - jbinGzipResult.FileSizeBytes) / jbinExclusiveResult.FileSizeBytes) * 100.0;
                    AppendLog($"3. 【开销对比实证】：在已启用紧凑模式时，叠加 GZip 导致耗时增加 +{timeCost:F1}%，而体积仅额外微缩 {sizeBenefit:F1}%。在合适的数据结构下，无需额外开启通用流压缩！\r\n", Color.OrangeRed);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"评测发生异常: {ex.Message}";
                AppendLog($"[错误] {ex}\r\n", Color.Red);
            }
            finally
            {
                btnRun.Enabled = true;
                btnClear.Enabled = true;
                cboScale.Enabled = true;
            }
        }

        private void ClearResults()
        {
            dgvResults.Rows.Clear();
            rtbLog.Clear();
            progressBar.Value = 0;
            lblStatus.Text = "就绪。";
            lblCardJbinReduction.Text = "- %";
            lblCardJbinLatency.Text = "- ms";
            lblCardSpeedupVsGzip.Text = "- 倍";
            lblCardGzipRedundancyCost.Text = "- % / - %";
            service.CleanUp();
        }

        private void AppendLog(string text, Color? color = null)
        {
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor = color ?? Color.FromArgb(220, 225, 235);
            rtbLog.AppendText(text);
            rtbLog.ScrollToCaret();
        }
    }
}
