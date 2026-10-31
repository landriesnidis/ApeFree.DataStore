using System;
using System.Drawing;
using System.Windows.Forms;

namespace ApeFree.DataStore.BenchmarkDemo.Views
{
    /// <summary>
    /// 架构原理与核心设计对比展示视图
    /// </summary>
    public class ArchitectureInfoControl : UserControl
    {
        public ArchitectureInfoControl()
        {
            this.SuspendLayout();
            this.Dock = DockStyle.Fill;
            this.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.White;
            this.AutoScroll = true;

            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                BackColor = Color.White,
                Padding = new Padding(24, 20, 24, 20)
            };

            rtb.Rtf = GenerateRtfDocumentation();

            this.Controls.Add(rtb);
            this.ResumeLayout(false);
        }

        private string GenerateRtfDocumentation()
        {
            // 为保证纯净显示与良好的排版效果，使用标准 RichText 格式
            return @"{\rtf1\ansi\ansicpg936\deff0\nouicompat{\fonttbl{\f0\fnil\fcharset134 Microsoft YaHei UI;}{\f1\fnil\fcharset0 Consolas;}}
{\colortbl ;\red0\green102\blue204;\red40\green40\blue40;\red100\green100\blue100;\red16\green137\blue62;\red204\green75\blue0;}
{\*\generator Riched20 10.0.19041}\viewkind4\uc1 
\pard\sa200\sl276\slmult1\b\f0\fs26\cf1 1. 高频配置读写为何产生性能灾难？ApeFree.DataStore 是如何破局的？\cf2\fs20\par
\b0\cf3 \b \bullet  传统原生文件读写 (File.WriteAllText + JsonConvert):\b0\par
\cf2   1. \b 每次写入均全量落盘\b0 ：即使配置对象中仅仅一个 Counter 属性发生变动，每次 Save 都会完整遍历序列化整个 JSON 字符串，并调用操作系统文件 API 打开、刷盘、关闭文件，导致密集的磁盘物理寻道与 SSD 写入损耗；\par
  2. \b 多线程并发锁冲突\b0 ：多线程高频写入同一文件时，频繁触发操作系统 \f1 IOException: Sharing violation\f0  文件占用异常；若在业务层采用粗粒度 \f1 lock\f0 ，将直接导致所有业务线程严重阻塞挂起；\par
  3. \b 重复反序列化磁盘开销\b0 ：每次业务读取配置都要重新读取文件并做 JSON 解析，耗时高达数毫秒。\par
\par
\cf4 \b \bullet  ApeFree.DataStore 的因果合并解决策略 (CoalesceMangedModel):\b0\par
\cf2   1. \b 内存零 I/O 直读\b0 ：读取配置直接访问 \f1 store.Value\f0 ，O(1) 内存访问，仅需纳秒级延迟；\par
  2. \b 双缓冲读写因果合并调度 (Coalesce)\b0 ：高频并发写入时，调度器自动在缓冲队列中执行“写折叠（Write Coalescing）”，数千次连续写入被因果折叠为极少数物理落盘，将调用方耗时降低 99%，磁盘 I/O 磨损削减 98% 以上；\par
  3. \b 严格保证 Read-Your-Writes 因果一致性\b0 ：双缓冲批次交替调度，读任务紧跟对应批次，杜绝脏读、漏写与乱序；\par
  4. \b 无锁高并发调度\b0 ：底层采用无锁 CAS (\f1 Interlocked.CompareExchange\f0 ) 与 \f1 TaskCompletionSource\f0  异步通知，业务调用方零线程阻塞。\par
\par
\pard\sa200\sl276\slmult1\b\fs26\cf1 2. 超大数据存储为何出现体积与速度瓶颈？Jbin 混合协议的核心优势\cf2\fs20\par
\b0\cf3 \b \bullet  传统 JSON / XML 在大数据场景下的致命缺陷:\b0\par
\cf2   1. \b 浮点与数值的文本膨胀\b0 ：浮点数（如 \f1 123.45678\f0 ）在 JSON 文本中需占用 10~15 字节字符串，而原生内存仅需 4 字节（float）或 8 字节（double），造成 300%~400% 的体积膨胀；\par
  2. \b 冗余的键名重复\b0 ：包含数万个对象的集合中，每个对象都会重复输出 \f1 ""Id""\f0 、\f1 ""Timestamp""\f0 、\f1 ""DeviceId""\f0  等键名字符串，产生数兆至数十兆的无效冗余；\par
  3. \b CPU 字符串格式化瓶颈\b0 ：百万级数值转换为字符串及反向解析将耗尽 CPU 算力，并引发大量的字符串分配与 GC LOH 大对象堆碎片停顿。\par
\par
\cf4 \b \bullet  Jbin 混合协议与 ApeFree.DataStore 流管道的技术设计:\b0\par
\cf2   1. \b 结构与负载分离 (JSON 骨架 + 二进制数据块池)\b0 ：对象拓扑用轻量 JSON 表示，大数组直接剥离为独立的 \f1 DataBlocks\f0 ；\par
  2. \b 基元数组零拷贝内存映射 (Zero-Copy BlockCopy)\b0 ：针对 \f1 float[]\f0 、\f1 double[]\f0 、\f1 int[]\f0  等值类型大数组，直接利用连续内存块读写，写入性能比普通 JSON 快 5~10 倍；\par
  3. \b 高性能列式存储转置 (TransposeToDictionary)\b0 ：针对超大业务实体对象集合，通过表达式树预编译转置引擎将行式集合转置为物理连续的属性列数组，彻底消除全部键名冗余；\par
  4. \b 管道流式压缩增益 (Jbin + GZipStreamFilter)\b0 ：高度连续规整的列式二进制数据流与流式压缩管道高度契合，体积较原生 JSON 暴降 90%~97%，以极小磁盘代价存储海量业务数据。\par
\par
\pard\sa200\sl276\slmult1\b\fs26\cf1 3. 流式管道架构 (IStreamFilter Pipeline)\cf2\fs20\par
\b0\cf2 ApeFree.DataStore 3.0 全面引入自由编排的装饰器流管道，序列化适配器（\f1 ISerializationAdapter\f0 ）直接往流管道输出，流过滤器（\f1 IStreamFilter\f0 ）负责即时透明包装，支持无缝级联压缩（\f1 GZipStreamFilter\f0 ）、加密（\f1 AesEncryptionStreamFilter\f0 ）及自定义监控过滤器，全程无中间全量内存拷贝。\par
}
";
        }
    }
}
