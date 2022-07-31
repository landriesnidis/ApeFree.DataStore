using ApeFree.DataStore.Adapters;
using ApeFree.DataStore.Core;
using ApeFree.DataStore.Demo;
using ApeFree.DataStore.Local;
using ApeFree.DataStore.Registry;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ApeFree.DataStore.Demo
{
    public partial class SelectForm : Form
    {
        public SelectForm()
        {
            InitializeComponent();
        }

        private void rbtnStore_Click(object sender, EventArgs e)
        {
            IStore<Student> store = null;
            if (rbtnDisk.Checked)
            {
                store = StoreFactory.Factory.CreateLoaclStore<Student>(
                    new LoaclStoreAccessSettings("./config/student.conf")
                    {
                        SerializationAdapter = new XmlSerializationAdapter(),
                        CompressionAdapter = new DeflateCompressionAdapter(),
                        // EncryptionAdapter = new AesEncryptionAdapter("12345678901234567890123456789012".GetBytes(), "0123456789abcdef".GetBytes()),
                    });
            }
            else if (rbtnRegistry.Checked)
            {
                store = StoreFactory.Factory.CreateRegistryStore<Student>(new RegistryStoreAccessSettings(RegistryHive.CurrentUser, @"ApeFree\DataStore\Demo", "student"));
            }
            propertyGrid.SelectedObject = store;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            var store = (IStore<IStudent>)propertyGrid.SelectedObject;
            if (store != null)
            {
                new EditForm(store).ShowDialog(); ;
            }
        }
    }
}
