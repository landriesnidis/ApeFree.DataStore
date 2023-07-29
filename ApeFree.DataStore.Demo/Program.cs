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

            //var settings = new LocalStoreAccessSettings("./config/student.json")
            //{

            //};

            //var store = new LocalStore<IStudent>(settings,()=> new Student());

            var store = StoreFactory.Factory.CreateRegistryStore<Student>(new Registry.RegistryStoreAccessSettings(Microsoft.Win32.RegistryHive.CurrentUser, @"ApeFree\DataStore\Demo", "student"));

            Application.Run(new EditForm(store));
        }
    }
}
