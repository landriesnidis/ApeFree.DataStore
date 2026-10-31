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
    }
}
