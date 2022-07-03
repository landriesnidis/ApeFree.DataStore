﻿using ApeFree.DataStore.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ApeFree.DataStore.Local
{
    /// <summary>
    /// 本地存储器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LoaclStore<T> : Store<T, LoaclStoreAccessSettings> where T : new()
    {
        public LoaclStore(LoaclStoreAccessSettings accessSettings) : base(accessSettings) { }

        public override void Load()
        {
            var path = AccessSettings.SavePath;
            if (!File.Exists(path))
            {
                Value = Activator.CreateInstance<T>();
                Save();
            }
            else
            {
                using (var steam = File.Open(path, FileMode.Open, FileAccess.Read))
                {
                    LoadHandler(steam);
                }
            }
        }

        public override void Save()
        {
            var path = AccessSettings.SavePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            SaveHandler(stream =>
            {
                // TODO: 此处应使用文件流写入

                MemoryStream memoryStream;
                if (stream is MemoryStream)
                {
                    memoryStream = stream as MemoryStream;
                }
                else
                {
                    memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                }
                File.WriteAllBytes(path, memoryStream.ToArray());
            });
        }
    }
}