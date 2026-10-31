using System;
using System.Drawing;
using System.Windows.Forms;
using ApeFree.DataStore.BenchmarkDemo.Views;

namespace ApeFree.DataStore.BenchmarkDemo
{
    public class MainForm : Form
    {
        private TabControl tabControl;
        private TabPage tabHighFreq;
        private TabPage tabBigData;
        private TabPage tabJbinExclusive;
        private TabPage tabArchInfo;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "ApeFree.DataStore 3.0 性能与大数据存储基准评测 (Benchmark & Performance Demo)";
            this.Size = new Size(1180, 780);
            this.MinimumSize = new Size(950, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // 顶部横幅
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(24, 32, 47),
                Padding = new Padding(20, 10, 20, 10)
            };

            var lblTitle = new Label
            {
                Text = "ApeFree.DataStore 3.0 基准评测与优势演示",
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 13.5F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 28
            };

            var lblSubtitle = new Label
            {
                Text = "⚡ Coalesce 并发因果写合并调度器  |  🚀 Jbin 混合二进制列式存储与零拷贝  |  📦 插件化流压缩管道 (GZip / Deflate / AES)",
                ForeColor = Color.FromArgb(160, 175, 200),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular),
                Dock = DockStyle.Fill
            };

            headerPanel.Controls.Add(lblSubtitle);
            headerPanel.Controls.Add(lblTitle);

            // 选项卡容器
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular),
                Padding = new Point(16, 8)
            };

            // Tab 1: 高频配置读写对比
            tabHighFreq = new TabPage
            {
                Text = " ⚡ 高频配置读写对比 (Coalesce 并发写合并) ",
                BackColor = Color.FromArgb(248, 249, 250)
            };
            var highFreqControl = new HighFrequencyTestControl { Dock = DockStyle.Fill };
            tabHighFreq.Controls.Add(highFreqControl);

            // Tab 2: 超大数据吞吐与压缩
            tabBigData = new TabPage
            {
                Text = " 🚀 超大数据通用评测 (Jbin 列式存储与零拷贝) ",
                BackColor = Color.FromArgb(248, 249, 250)
            };
            var bigDataControl = new BigDataTestControl { Dock = DockStyle.Fill };
            tabBigData.Controls.Add(bigDataControl);

            // Tab 3: Jbin 闭源专有压缩极限与零累赘实战
            tabJbinExclusive = new TabPage
            {
                Text = " 🌟 Jbin 专有优势与零累赘实战 (理想数据形态 vs GZip 累赘对比) ",
                BackColor = Color.FromArgb(248, 249, 250)
            };
            var jbinExclusiveControl = new JbinExclusiveBenchmarkControl { Dock = DockStyle.Fill };
            tabJbinExclusive.Controls.Add(jbinExclusiveControl);

            // Tab 4: 架构原理解析
            tabArchInfo = new TabPage
            {
                Text = " ℹ️ 架构原理解析 (Architecture & Design) ",
                BackColor = Color.White
            };
            var archControl = new ArchitectureInfoControl { Dock = DockStyle.Fill };
            tabArchInfo.Controls.Add(archControl);

            tabControl.TabPages.Add(tabHighFreq);
            tabControl.TabPages.Add(tabBigData);
            tabControl.TabPages.Add(tabJbinExclusive);
            tabControl.TabPages.Add(tabArchInfo);

            this.Controls.Add(tabControl);
            this.Controls.Add(headerPanel);

            this.ResumeLayout(false);
        }
    }
}
