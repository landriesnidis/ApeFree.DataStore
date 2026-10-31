using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ApeFree.DataStore.Adapters;
using ApeFree.DataStore.Filters;
using ApeFree.DataStore.Local;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApeFree.DataStore.Tests
{
    [TestClass]
    public class LocalStoreTests
    {
        private static readonly byte[] AesKey = Encoding.UTF8.GetBytes("12345678901234567890123456789012");
        private static readonly byte[] AesIV = Encoding.UTF8.GetBytes("abcdef0123456789");

        private string testDir;

        [TestInitialize]
        public void Init()
        {
            testDir = Path.Combine(Path.GetTempPath(), "ApeFree_DataStore_Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(testDir))
            {
                try { Directory.Delete(testDir, true); } catch { }
            }
        }

        [TestMethod]
        public void TestLocalStoreJsonSaveAndLoad()
        {
            var filePath = Path.Combine(testDir, "settings.json");
            var settings = new LocalStoreAccessSettings(filePath)
            {
                SerializationAdapter = new JsonSerializationAdapter()
            };

            // 1. 初始化并保存
            using (var store = new LocalStore<SampleSettings>(settings, () => new SampleSettings { AppName = "LocalStoreJson", Version = 1 }))
            {
                store.Load();
                Assert.AreEqual("LocalStoreJson", store.Value.AppName);
                store.Value.Version = 2;
                store.Save();
            }

            Assert.IsTrue(File.Exists(filePath), "文件应该已被创建");

            // 2. 重新从文件加载
            var loadSettings = new LocalStoreAccessSettings(filePath)
            {
                SerializationAdapter = new JsonSerializationAdapter()
            };
            using (var store2 = new LocalStore<SampleSettings>(loadSettings))
            {
                store2.Load();
                Assert.IsNotNull(store2.Value);
                Assert.AreEqual("LocalStoreJson", store2.Value.AppName);
                Assert.AreEqual(2, store2.Value.Version);
            }
        }

        [TestMethod]
        public void TestLocalStoreXmlSaveAndLoad()
        {
            var filePath = Path.Combine(testDir, "settings.xml");
            var settings = new LocalStoreAccessSettings(filePath)
            {
                SerializationAdapter = new XmlSerializationAdapter("ApplicationXmlConfig")
            };

            // 1. 初始化并物理保存 XML
            using (var store = new LocalStore<XmlCompatibleSettings>(settings, () => new XmlCompatibleSettings { Title = "XmlDiskConfig", Id = 9999 }))
            {
                store.Load();
                store.Save();
            }

            Assert.IsTrue(File.Exists(filePath), "XML 文件应当已落盘生成");
            var rawXml = File.ReadAllText(filePath);
            StringAssert.Contains(rawXml, "<ApplicationXmlConfig");
            StringAssert.Contains(rawXml, "<Title>XmlDiskConfig</Title>");

            // 2. 重新从物理 XML 文件加载
            var loadSettings = new LocalStoreAccessSettings(filePath)
            {
                SerializationAdapter = new XmlSerializationAdapter("ApplicationXmlConfig")
            };
            using (var store2 = new LocalStore<XmlCompatibleSettings>(loadSettings))
            {
                store2.Load();
                Assert.IsNotNull(store2.Value);
                Assert.AreEqual("XmlDiskConfig", store2.Value.Title);
                Assert.AreEqual(9999, store2.Value.Id);
            }
        }

        [TestMethod]
        public void TestLocalStoreXmlWithPipelineCompression()
        {
            var filePath = Path.Combine(testDir, "compressed_xml.dat");
            var settings = new LocalStoreAccessSettings(filePath)
            {
                SerializationAdapter = new XmlSerializationAdapter()
            };
            settings.StreamFilters.Add(new DeflateStreamFilter());

            using (var store = new LocalStore<XmlCompatibleSettings>(settings, () => new XmlCompatibleSettings { Title = "DeflateCompressedXml", Id = 777 }))
            {
                store.Load();
                store.Save();
            }

            Assert.IsTrue(File.Exists(filePath));

            // 加载验证
            var loadSettings = new LocalStoreAccessSettings(filePath)
            {
                SerializationAdapter = new XmlSerializationAdapter()
            };
            loadSettings.StreamFilters.Add(new DeflateStreamFilter());

            using (var store2 = new LocalStore<XmlCompatibleSettings>(loadSettings))
            {
                store2.Load();
                Assert.IsNotNull(store2.Value);
                Assert.AreEqual("DeflateCompressedXml", store2.Value.Title);
                Assert.AreEqual(777, store2.Value.Id);
            }
        }

        [TestMethod]
        public void TestLocalStoreWithPipelineCompressionAndEncryption()
        {
            var filePath = Path.Combine(testDir, "secure_config.bin");
            var settings = new LocalStoreAccessSettings(filePath)
            {
                SerializationAdapter = new JsonSerializationAdapter()
            };

            // 自由配置流式过滤器管道：先压缩，后加密
            settings.StreamFilters.Add(new GZipStreamFilter());
            settings.StreamFilters.Add(new AesEncryptionStreamFilter(AesKey, AesIV));

            using (var store = new LocalStore<SampleSettings>(settings, () => new SampleSettings { AppName = "TopSecretApp", Version = 888 }))
            {
                store.Load();
                store.Save();
            }

            // 验证磁盘上的文件内容已经被加密（不含明文关键字）
            var rawText = File.ReadAllText(filePath);
            Assert.IsFalse(rawText.Contains("TopSecretApp"), "磁盘文件应当完全加密不可读");

            // 重新通过相同管道解包加载（模拟客户端重新启动）
            var loadSettings = new LocalStoreAccessSettings(filePath)
            {
                SerializationAdapter = new JsonSerializationAdapter()
            };
            loadSettings.StreamFilters.Add(new GZipStreamFilter());
            loadSettings.StreamFilters.Add(new AesEncryptionStreamFilter(AesKey, AesIV));

            using (var store2 = new LocalStore<SampleSettings>(loadSettings))
            {
                store2.Load();
                Assert.IsNotNull(store2.Value);
                Assert.AreEqual("TopSecretApp", store2.Value.AppName);
                Assert.AreEqual(888, store2.Value.Version);
            }
        }

        [TestMethod]
        public async Task TestLocalStoreConcurrentAsyncOperations()
        {
            var filePath = Path.Combine(testDir, "concurrent_test.jbin");
            var settings = new LocalStoreAccessSettings(filePath)
            {
                SerializationAdapter = new JbinSerializationAdapter()
            };

            using (var store = new LocalStore<SampleSettings>(settings, () => new SampleSettings { Version = 0 }))
            {
                store.Load();

                const int loopCount = 200;
                var tasks = new Task[loopCount];

                for (int i = 0; i < loopCount; i++)
                {
                    int index = i;
                    tasks[i] = Task.Run(async () =>
                    {
                        store.Value.Version = index;
                        await store.SaveAsync();
                        await store.LoadAsync();
                    });
                }

                await Task.WhenAll(tasks);
                Assert.IsNotNull(store.Value);
            }
        }
    }
}
