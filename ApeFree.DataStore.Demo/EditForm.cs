using ApeFree.DataStore.Core;
using ApeFree.DataStore.Demo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ApeFree.DataStore.Demo
{
    public partial class EditForm : Form
    {

        private IStore<IStudent> store;

        public EditForm(IStore<IStudent> store) : this()
        {
            this.store = store;

            tsmiLoad.PerformClick();
        }

        private EditForm()
        {
            InitializeComponent();
        }

        private void tsmiLoad_Click(object sender, EventArgs e)
        {
            store.Load();
            propertyGrid.SelectedObject = store.Value;
        }

        private void tsmiSave_Click(object sender, EventArgs e)
        {
            store.Save();
            Close();
        }

        private void tsmiTestIO_Click(object sender, EventArgs e)
        {
            int times = 1000;

            Stopwatch watch = new Stopwatch();
            watch.Restart();
            for (int i = 0; i < times; i++)
            {
                store.Load();
                store.Save();
            }
            watch.Stop();

            // 计算耗时（毫秒）
            var elapsedTime = watch.ElapsedTicks * 1000.0 / Stopwatch.Frequency;

            MessageBox.Show($"存取{times}次测试完毕。\r\n" +
                $"总耗时：{elapsedTime}毫秒。\r\n" +
                $"平均单次读取+保存耗时：{elapsedTime / times}毫秒");
        }

        private void tsmiConcurrentTestIO_Click(object sender, EventArgs e)
        {
            int times = 1000;
            List<Task> tasks = new List<Task>();
            Stopwatch watch = new Stopwatch();
            watch.Restart();
            for (int i = 0; i < times; i += 2)
            {
                tasks.Add(store.LoadAsync());
                tasks.Add(store.LoadAsync());
                tasks.Add(store.SaveAsync());
                tasks.Add(store.SaveAsync());
            }
            Task.WaitAll(tasks.ToArray());
            watch.Stop();

            // 计算耗时（毫秒）
            var elapsedTime = watch.ElapsedTicks * 1000.0 / Stopwatch.Frequency;

            MessageBox.Show($"存取{times}次测试完毕。\r\n" +
                $"总耗时：{elapsedTime}毫秒。\r\n" +
                $"平均单次读取+保存耗时：{elapsedTime / times}毫秒");
        }
    }
}
