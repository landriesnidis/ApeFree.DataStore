using System;
using System.IO;
using System.Linq;
using ApeFree.DataStore.Adapters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApeFree.DataStore.Tests
{
    [TestClass]
    public class JbinOptimizationTests
    {
        [TestMethod]
        public void TestJbinLargeArraySerializationAndDirectStreamWrite()
        {
            var adapter = new JbinSerializationAdapter();

            const int arraySize = 50000;
            var bigData = new BigDataModel
            {
                Title = "工业级点云与传感器时序测试数据",
                SensorData = Enumerable.Range(0, arraySize).Select(i => (float)(i * 0.12345)).ToArray(),
                MatrixIndices = Enumerable.Range(0, arraySize).Select(i => i * 2).ToArray(),
                Timestamp = DateTime.UtcNow
            };

            byte[] serializedBytes;
            using (var ms = new MemoryStream())
            {
                // 端到端直接流式写入
                adapter.Serialize(bigData, ms);
                serializedBytes = ms.ToArray();
            }

            Assert.IsTrue(serializedBytes.Length > 0, "序列化字节流不能为空");

            BigDataModel restored;
            using (var ms = new MemoryStream(serializedBytes))
            {
                restored = adapter.Deserialize<BigDataModel>(ms);
            }

            Assert.IsNotNull(restored);
            Assert.AreEqual(bigData.Title, restored.Title);
            Assert.AreEqual(arraySize, restored.SensorData.Length);
            Assert.AreEqual(arraySize, restored.MatrixIndices.Length);

            // 抽样校验数组元素精准一致性
            Assert.AreEqual(bigData.SensorData[0], restored.SensorData[0], 0.00001f);
            Assert.AreEqual(bigData.SensorData[25000], restored.SensorData[25000], 0.00001f);
            Assert.AreEqual(bigData.SensorData[arraySize - 1], restored.SensorData[arraySize - 1], 0.00001f);
            Assert.AreEqual(bigData.MatrixIndices[25000], restored.MatrixIndices[25000]);
        }

        [TestMethod]
        public void TestJbinPrimitiveSpecializedCompression()
        {
            var settings = ApeFree.Protocols.Json.Jbin.JbinObject.JsonSerializerSettings;
            var conv = settings.Converters.OfType<ApeFree.Protocols.Json.Jbin.JbinPrimitiveArrayConverter>().FirstOrDefault();
            Assert.IsNotNull(conv);
            conv.SingleMode = ApeFree.Protocols.Json.Jbin.SingleArrayCompressMode.Gorilla;
            conv.Int32Mode = ApeFree.Protocols.Json.Jbin.Int32ArrayCompressMode.FrameOfReference;

            const int arraySize = 10000;
            var bigData = new BigDataModel
            {
                Title = "时序压缩测试",
                SensorData = Enumerable.Range(0, arraySize).Select(i => (float)(i * 0.1)).ToArray(),
                MatrixIndices = Enumerable.Range(0, arraySize).Select(i => 1000 + (i % 50)).ToArray(),
                Timestamp = DateTime.UtcNow
            };

            var adapter = new JbinSerializationAdapter(settings);
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                adapter.Serialize(bigData, ms);
                bytes = ms.ToArray();
            }

            Assert.IsTrue(bytes.Length > 0);

            // 自适应反序列化测试：默认 adapter 根据帧中 ModeId 自适应解压缩
            var defaultAdapter = new JbinSerializationAdapter();
            BigDataModel restored;
            using (var ms = new MemoryStream(bytes))
            {
                restored = defaultAdapter.Deserialize<BigDataModel>(ms);
            }

            Assert.IsNotNull(restored);
            Assert.AreEqual(arraySize, restored.SensorData.Length);
            Assert.AreEqual(bigData.SensorData[123], restored.SensorData[123], 0.0001f);
            Assert.AreEqual(bigData.MatrixIndices[123], restored.MatrixIndices[123]);
        }

        [TestMethod]
        public void TestJbinPolymorphicAndComplexObject()
        {
            var adapter = new JbinSerializationAdapter();
            var original = new SampleSettings
            {
                AppName = "JbinComplexTest",
                Version = 300,
                IsEnabled = true
            };
            original.Tags.Add("ExtraTag");
            original.Metadata["Key1"] = "Val1";

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                adapter.Serialize(original, ms);
                bytes = ms.ToArray();
            }

            SampleSettings restored;
            using (var ms = new MemoryStream(bytes))
            {
                restored = adapter.Deserialize<SampleSettings>(ms);
            }

            Assert.IsNotNull(restored);
            Assert.AreEqual(original.AppName, restored.AppName);
            Assert.AreEqual(original.Tags.Count, restored.Tags.Count);
            Assert.AreEqual("Val1", restored.Metadata["Key1"]);
        }

        public class MockRecord
        {
            public long Id { get; set; }
            public string Code { get; set; }
            public double Value { get; set; }
        }

        [TestMethod]
        public void TestJbinColumnarTranspositionAndRestoration()
        {
            const int count = 2000;
            var list = new System.Collections.Generic.List<MockRecord>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new MockRecord { Id = i, Code = $"CODE_{i % 10}", Value = i * 1.5 });
            }

            // 行转列
            var dict = ApeFree.Protocols.Json.Jbin.Extensions.JbinExtensions.TransposeToDictionary(list);
            Assert.IsNotNull(dict);
            Assert.IsTrue(dict.ContainsKey("Id"));
            Assert.IsTrue(dict.ContainsKey("Code"));
            Assert.IsTrue(dict.ContainsKey("Value"));

            var adapter = new JbinSerializationAdapter();
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                adapter.Serialize(dict, ms);
                bytes = ms.ToArray();
            }

            System.Collections.Generic.Dictionary<string, Array> restoredDict;
            using (var ms = new MemoryStream(bytes))
            {
                restoredDict = adapter.Deserialize<System.Collections.Generic.Dictionary<string, Array>>(ms);
            }

            Assert.IsNotNull(restoredDict);

            // 列转行
            var restoredList = ApeFree.Protocols.Json.Jbin.Extensions.JbinExtensions.TransposeFromDictionary<MockRecord>(restoredDict);
            Assert.IsNotNull(restoredList);
            Assert.AreEqual(count, restoredList.Length);
            Assert.AreEqual(list[0].Id, restoredList[0].Id);
            Assert.AreEqual(list[100].Code, restoredList[100].Code);
            Assert.AreEqual(list[count - 1].Value, restoredList[count - 1].Value, 0.0001);
        }
    }
}
