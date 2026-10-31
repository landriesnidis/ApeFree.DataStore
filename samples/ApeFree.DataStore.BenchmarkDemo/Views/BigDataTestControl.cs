using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ApeFree.DataStore.BenchmarkDemo.Services;

namespace ApeFree.DataStore.BenchmarkDemo.Views
{
    public class BigDataTestControl : UserControl
    {
        private readonly BigDataBenchmarkService service = new BigDataBenchmarkService();

        // 控件声明
        private ComboBox cboDataset;
        private ComboBox cboScale;
        private Button btnRun;
        private Button btnClear;
        private ProgressBar progressBar;
        private Label lblStatus;
        private DataGridView dgvResults;
        private RichTextBox rtbLog;
        private Label lblCardMinSize;
        private Label lblCardMaxSpeed;
        private Label lblCardReduction;

        public BigDataTestControl()
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
                WrapContents = true
            };

            // 数据集选择
            flpConfig.Controls.Add(CreateLabel("测试数据集类型:"));
            cboDataset = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 310 };
            cboDataset.Items.AddRange(new object[]
            {
                "1. 海量传感器/3D点云时序 (基元值类型大数组: float[], double[])",
                "2. 超大业务实体对象集合 (对象密集型: Jbin 列式转置与压缩)"
            });
            cboDataset.SelectedIndex = 0;
            cboDataset.SelectedIndexChanged += (s, e) => UpdateScaleOptions();
            flpConfig.Controls.Add(cboDataset);

            // 数据规模选择
            flpConfig.Controls.Add(CreateLabel("  数据规模:"));
            cboScale = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
            flpConfig.Controls.Add(cboScale);
            UpdateScaleOptions();

            // 启动评测按钮
            btnRun = new Button
            {
                Text = "🚀 开始大数据读写评测",
                Width = 175,
                Height = 32,
                BackColor = Color.FromArgb(16, 137, 62),
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

            // 状态与进度面板
            var panelProgress = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Color.FromArgb(240, 243, 246),
                Padding = new Padding(16, 4, 16, 4)
            };
            lblStatus = new Label
            {
                Text = "就绪 - 请选择数据集类型与规模后点击“开始大数据读写评测”",
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

            lblCardReduction = CreateMetricCard("🏆 体积最大缩减比", "-- %", Color.FromArgb(235, 251, 238), Color.FromArgb(16, 137, 62));
            lblCardMaxSpeed = CreateMetricCard("⚡ 读写往返最高提速", "-- 倍", Color.FromArgb(230, 244, 255), Color.FromArgb(0, 102, 204));
            lblCardMinSize = CreateMetricCard("💾 磁盘存储最小尺寸", "--", Color.FromArgb(254, 243, 235), Color.FromArgb(204, 75, 0));

            panelCards.Controls.Add(lblCardReduction.Parent, 0, 0);
            panelCards.Controls.Add(lblCardMaxSpeed.Parent, 1, 0);
            panelCards.Controls.Add(lblCardMinSize.Parent, 2, 0);

            // 主分割容器
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 290,
                Panel1MinSize = 200,
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
                RowTemplate = { Height = 32 },
                GridColor = Color.FromArgb(230, 234, 240)
            };
            dgvResults.EnableHeadersVisualStyles = false;
            dgvResults.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(243, 245, 249);
            dgvResults.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(40, 50, 60);
            dgvResults.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);

            dgvResults.Columns.Add("Solution", "存储与序列化方案 (Solution)");
            dgvResults.Columns.Add("WriteMs", "写入耗时 (Write ms)");
            dgvResults.Columns.Add("ReadMs", "读取耗时 (Read ms)");
            dgvResults.Columns.Add("TotalMs", "往返耗时 (Total ms)");
            dgvResults.Columns.Add("FileSize", "磁盘体积 (Size)");
            dgvResults.Columns.Add("Reduction", "相对 JSON 压缩率");
            dgvResults.Columns.Add("Throughput", "写入吞吐 (MB/s)");
            dgvResults.Columns.Add("Verification", "数据校验");
            dgvResults.Columns.Add("Notes", "方案特征与核心亮点 (Highlights)");

            // 调整关键列宽比例
            dgvResults.Columns["Solution"].FillWeight = 160;
            dgvResults.Columns["WriteMs"].FillWeight = 85;
            dgvResults.Columns["ReadMs"].FillWeight = 85;
            dgvResults.Columns["TotalMs"].FillWeight = 85;
            dgvResults.Columns["FileSize"].FillWeight = 80;
            dgvResults.Columns["Reduction"].FillWeight = 100;
            dgvResults.Columns["Throughput"].FillWeight = 85;
            dgvResults.Columns["Verification"].FillWeight = 75;
            dgvResults.Columns["Notes"].FillWeight = 200;

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
        }

        private void UpdateScaleOptions()
        {
            cboScale.Items.Clear();
            if (cboDataset.SelectedIndex == 0)
            {
                cboScale.Items.AddRange(new object[] { "10,000 点", "50,000 点", "100,000 点", "200,000 点" });
                cboScale.SelectedIndex = 1;
            }
            else
            {
                cboScale.Items.AddRange(new object[] { "5,000 条", "10,000 条", "30,000 条", "50,000 条" });
                cboScale.SelectedIndex = 1;
            }
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

        private void ClearResults()
        {
            dgvResults.Rows.Clear();
            rtbLog.Clear();
            lblStatus.Text = "就绪";
            progressBar.Value = 0;
            lblCardReduction.Text = "-- %";
            lblCardMaxSpeed.Text = "-- 倍";
            lblCardMinSize.Text = "--";
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
            dgvResults.Rows.Clear();

            int datasetType = cboDataset.SelectedIndex;
            string scaleStr = cboScale.SelectedItem.ToString().Replace(" 点", "").Replace(" 条", "").Replace(",", "");
            int count = int.Parse(scaleStr);

            AppendLog("================================================================================", Color.FromArgb(80, 200, 120));
            AppendLog($"[大数据评测启动] 数据集: {cboDataset.SelectedItem} | 规模: {count:N0} 项", Color.FromArgb(80, 200, 120));

            try
            {
                List<BigDataBenchmarkResult> results;
                if (datasetType == 0)
                {
                    results = await Task.Run(() => service.RunPointCloudBenchmark(
                        count,
                        msg => AppendLog(msg),
                        (curr, tot, name) =>
                        {
                            if (progressBar.InvokeRequired)
                                progressBar.Invoke(new Action(() =>
                                {
                                    progressBar.Value = (int)(curr * 100.0 / tot);
                                    lblStatus.Text = $"正在评测 ({curr}/{tot}): {name}";
                                }));
                            else
                            {
                                progressBar.Value = (int)(curr * 100.0 / tot);
                                lblStatus.Text = $"正在评测 ({curr}/{tot}): {name}";
                            }
                        }
                    ));
                }
                else
                {
                    results = await Task.Run(() => service.RunObjectBatchBenchmark(
                        count,
                        msg => AppendLog(msg),
                        (curr, tot, name) =>
                        {
                            if (progressBar.InvokeRequired)
                                progressBar.Invoke(new Action(() =>
                                {
                                    progressBar.Value = (int)(curr * 100.0 / tot);
                                    lblStatus.Text = $"正在评测 ({curr}/{tot}): {name}";
                                }));
                            else
                            {
                                progressBar.Value = (int)(curr * 100.0 / tot);
                                lblStatus.Text = $"正在评测 ({curr}/{tot}): {name}";
                            }
                        }
                    ));
                }

                RenderResults(results);
                lblStatus.Text = "评测圆满完成！数据已更新至上方结果视图。";
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

        private void RenderResults(List<BigDataBenchmarkResult> results)
        {
            dgvResults.Rows.Clear();

            double maxReduction = 0;
            double baselineTotalTime = 0;
            double fastestTotalTime = double.MaxValue;
            long minSize = long.MaxValue;

            var baseline = results.Count > 0 ? results[0] : null;
            if (baseline != null)
            {
                baselineTotalTime = baseline.TotalElapsedMs;
            }

            foreach (var r in results)
            {
                if (r.SizeReductionPercent > maxReduction) maxReduction = r.SizeReductionPercent;
                if (r.TotalElapsedMs < fastestTotalTime) fastestTotalTime = r.TotalElapsedMs;
                if (r.FileSizeBytes < minSize) minSize = r.FileSizeBytes;

                string reductionStr = r.SizeReductionPercent == 0 ? "基准 (0%)" :
                    (r.SizeReductionPercent > 0 ? $"-{r.SizeReductionPercent:F1}%" : $"+{-r.SizeReductionPercent:F1}%");

                int rowIdx = dgvResults.Rows.Add(
                    r.StrategyName,
                    $"{r.WriteElapsedMs:F1} ms",
                    $"{r.ReadElapsedMs:F1} ms",
                    $"{r.TotalElapsedMs:F1} ms",
                    r.FormattedFileSize,
                    reductionStr,
                    $"{r.WriteThroughputMBs:F1} MB/s",
                    r.IsVerified ? "✅ PASS" : "❌ FAIL",
                    r.Notes
                );

                var row = dgvResults.Rows[rowIdx];

                // 高亮特定优势方案
                if (r.StrategyName.Contains("Jbin") && r.StrategyName.Contains("极限压缩"))
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 244);
                    row.Cells["Reduction"].Style.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
                    row.Cells["Reduction"].Style.ForeColor = Color.FromArgb(16, 137, 62);
                }
                else if (r.StrategyName.Contains("Jbin") && (r.StrategyName.Contains("零拷贝") || r.StrategyName.Contains("列式存储")))
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(240, 248, 255);
                    row.Cells["TotalMs"].Style.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
                    row.Cells["TotalMs"].Style.ForeColor = Color.FromArgb(0, 102, 204);
                }
            }

            // 更新顶部卡片
            lblCardReduction.Text = $"-{maxReduction:F1} %";
            if (fastestTotalTime > 0 && baselineTotalTime > 0)
            {
                lblCardMaxSpeed.Text = $"{(baselineTotalTime / fastestTotalTime):F1} 倍";
            }
            if (minSize < long.MaxValue)
            {
                lblCardMinSize.Text = FormatBytes(minSize);
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024) return $"{(double)bytes / (1024 * 1024):F2} MB";
            if (bytes >= 1024) return $"{(double)bytes / 1024:F2} KB";
            return $"{bytes} B";
        }
    }
}
