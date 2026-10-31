using System;
using System.IO;
using System.Text;
using ApeFree.DataStore.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace ApeFree.DataStore.Tests
{
    [TestClass]
    public class SerializationAdapterTests
    {
        [TestMethod]
        public void TestJsonSerializationAdapter_DefaultSettings()
        {
            var adapter = new JsonSerializationAdapter();
            var model = new SampleSettings
            {
                AppName = "JsonTestApp",
                Version = 42,
                IsEnabled = true
            };
            model.Tags.Add("JsonTag");
            model.Metadata["Env"] = "Production";

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                adapter.Serialize(model, ms);
                bytes = ms.ToArray();
            }

            Assert.IsTrue(bytes.Length > 0);
            var jsonString = Encoding.UTF8.GetString(bytes);
            StringAssert.Contains(jsonString, "JsonTestApp");
            StringAssert.Contains(jsonString, "Production");

            SampleSettings restored;
            using (var ms = new MemoryStream(bytes))
            {
                restored = adapter.Deserialize<SampleSettings>(ms);
            }

            Assert.IsNotNull(restored);
            Assert.AreEqual("JsonTestApp", restored.AppName);
            Assert.AreEqual(42, restored.Version);
            Assert.AreEqual(true, restored.IsEnabled);
            Assert.AreEqual("Production", restored.Metadata["Env"]);
        }

        [TestMethod]
        public void TestJsonSerializationAdapter_CustomSettings()
        {
            var customSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Include
            };
            var adapter = new JsonSerializationAdapter(customSettings);
            var model = new SampleSettings { AppName = "CompactJson", Version = 1 };

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                adapter.Serialize(model, ms);
                bytes = ms.ToArray();
            }

            var jsonString = Encoding.UTF8.GetString(bytes);
            // 验证紧凑无换行格式
            Assert.IsFalse(jsonString.Contains("\r\n"), "紧凑格式不应包含换行符");

            using (var ms = new MemoryStream(bytes))
            {
                var restored = adapter.Deserialize<SampleSettings>(ms);
                Assert.AreEqual("CompactJson", restored.AppName);
            }
        }

        [TestMethod]
        public void TestXmlSerializationAdapter_Default()
        {
            using (var adapter = new XmlSerializationAdapter())
            {
                var model = new XmlCompatibleSettings
                {
                    Title = "StandardXml",
                    Id = 2026,
                    Score = 98.75,
                    Active = true
                };
                model.Items.Add("Alpha");
                model.Items.Add("Beta");

                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    adapter.Serialize(model, ms);
                    bytes = ms.ToArray();
                }

                Assert.IsTrue(bytes.Length > 0);
                var xmlText = Encoding.UTF8.GetString(bytes);
                StringAssert.Contains(xmlText, "<XmlCompatibleSettings");
                StringAssert.Contains(xmlText, "<Title>StandardXml</Title>");
                StringAssert.Contains(xmlText, "<Score>98.75</Score>");

                XmlCompatibleSettings restored;
                using (var ms = new MemoryStream(bytes))
                {
                    restored = adapter.Deserialize<XmlCompatibleSettings>(ms);
                }

                Assert.IsNotNull(restored);
                Assert.AreEqual("StandardXml", restored.Title);
                Assert.AreEqual(2026, restored.Id);
                Assert.AreEqual(98.75, restored.Score, 0.001);
                Assert.AreEqual(true, restored.Active);
                Assert.AreEqual(2, restored.Items.Count);
            }
        }

        [TestMethod]
        public void TestXmlSerializationAdapter_CustomRootName()
        {
            const string customRoot = "MyCustomConfigRoot";
            using (var adapter = new XmlSerializationAdapter(customRoot))
            {
                var model = new XmlCompatibleSettings
                {
                    Title = "CustomRootXml",
                    Id = 8888
                };

                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    adapter.Serialize(model, ms);
                    bytes = ms.ToArray();
                }

                var xmlText = Encoding.UTF8.GetString(bytes);
                StringAssert.Contains(xmlText, $"<{customRoot}");

                using (var ms = new MemoryStream(bytes))
                {
                    var restored = adapter.Deserialize<XmlCompatibleSettings>(ms);
                    Assert.AreEqual("CustomRootXml", restored.Title);
                    Assert.AreEqual(8888, restored.Id);
                }
            }
        }

        [TestMethod]
        public void TestAdapters_NullSafety()
        {
            var jsonAdapter = new JsonSerializationAdapter();
            var xmlAdapter = new XmlSerializationAdapter();
            var jbinAdapter = new JbinSerializationAdapter();

            using (var ms = new MemoryStream())
            {
                // 传入 null 对象不应引发崩溃
                jsonAdapter.Serialize(null, ms);
                xmlAdapter.Serialize(null, ms);
                jbinAdapter.Serialize(null, ms);
            }

            // 对 null stream 反序列化应安全返回 null
            Assert.IsNull(jsonAdapter.Deserialize(null, typeof(SampleSettings)));
            Assert.IsNull(xmlAdapter.Deserialize(null, typeof(XmlCompatibleSettings)));
            Assert.IsNull(jbinAdapter.Deserialize(null, typeof(SampleSettings)));
        }
    }
}
