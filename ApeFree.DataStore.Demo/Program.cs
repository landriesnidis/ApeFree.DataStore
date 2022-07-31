using ApeFree.DataStore.Local;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ApeFree.DataStore.Demo
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var store = new LoaclStore<IStudent>(new LoaclStoreAccessSettings("./config/student.json")
            {
                
            });
            Application.Run(new EditForm(store));
        }
    }
}
