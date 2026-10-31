using System;
using System.IO;
using System.Text;
using ApeFree.DataStore.Adapters;
using ApeFree.DataStore.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ApeFree.DataStore.Tests
{
    [TestClass]
    public class StreamPipelineTests
    {
        private static readonly byte[] AesKey = Encoding.UTF8.GetBytes("01234567890123456789012345678901"); // 32 bytes
        private static readonly byte[] AesIV = Encoding.UTF8.GetBytes("0123456789abcdef"); // 16 bytes

        [TestMethod]
        public void TestGZipStreamFilter()
        {
            var filter = new GZipStreamFilter();
            var serializer = new JsonSerializationAdapter();
            var original = new SampleSettings { AppName = "GZipPipelineTest", Version = 100 };

            byte[] compressedBytes;
            using (var ms = new MemoryStream())
            {
                using (var writeStream = filter.WrapWriteStream(ms))
                {
                    serializer.Serialize(original, writeStream);
                }
                compressedBytes = ms.ToArray();
            }

            Assert.IsTrue(compressedBytes.Length > 0);

            SampleSettings restored;
            using (var ms = new MemoryStream(compressedBytes))
            using (var readStream = filter.WrapReadStream(ms))
            {
                restored = serializer.Deserialize<SampleSettings>(readStream);
            }

            Assert.IsNotNull(restored);
            Assert.AreEqual(original.AppName, restored.AppName);
            Assert.AreEqual(original.Version, restored.Version);
        }

        [TestMethod]
        public void TestDeflateStreamFilter()
        {
            var filter = new DeflateStreamFilter();
            var serializer = new JsonSerializationAdapter();
            var original = new SampleSettings { AppName = "DeflatePipelineTest", Version = 200 };

            byte[] compressedBytes;
            using (var ms = new MemoryStream())
            {
                using (var writeStream = filter.WrapWriteStream(ms))
                {
                    serializer.Serialize(original, writeStream);
                }
                compressedBytes = ms.ToArray();
            }

            Assert.IsTrue(compressedBytes.Length > 0);

            SampleSettings restored;
            using (var ms = new MemoryStream(compressedBytes))
            using (var readStream = filter.WrapReadStream(ms))
            {
                restored = serializer.Deserialize<SampleSettings>(readStream);
            }

            Assert.IsNotNull(restored);
            Assert.AreEqual(original.AppName, restored.AppName);
            Assert.AreEqual(original.Version, restored.Version);
        }

        [TestMethod]
        public void TestAesEncryptionStreamFilter()
        {
            var filter = new AesEncryptionStreamFilter(AesKey, AesIV);
            var serializer = new JsonSerializationAdapter();
            var original = new SampleSettings { AppName = "SecretApp", Version = 999 };

            byte[] cipherBytes;
            using (var ms = new MemoryStream())
            {
                using (var writeStream = filter.WrapWriteStream(ms))
                {
                    serializer.Serialize(original, writeStream);
                }
                cipherBytes = ms.ToArray();
            }

            // 验证输出密文不包含明文关键词
            var cipherText = Encoding.UTF8.GetString(cipherBytes);
            Assert.IsFalse(cipherText.Contains("SecretApp"), "密文不应包含明文关键字");

            SampleSettings restored;
            using (var ms = new MemoryStream(cipherBytes))
            using (var readStream = filter.WrapReadStream(ms))
            {
                restored = serializer.Deserialize<SampleSettings>(readStream);
            }

            Assert.IsNotNull(restored);
            Assert.AreEqual(original.AppName, restored.AppName);
            Assert.AreEqual(original.Version, restored.Version);
        }

        [TestMethod]
        public void TestCompositePipeline_CompressThenEncrypt()
        {
            // 组装双层管道：先压缩，后加密
            var gzip = new GZipStreamFilter();
            var aes = new AesEncryptionStreamFilter(AesKey, AesIV);
            var serializer = new JsonSerializationAdapter();

            var original = new SampleSettings { AppName = "CompositePipelineTest", Version = 300 };

            byte[] finalBytes;
            using (var ms = new MemoryStream())
            {
                // 写入装饰顺序：先包装 AES，再包装 GZip
                using (var aesStream = aes.WrapWriteStream(ms))
                using (var gzipStream = gzip.WrapWriteStream(aesStream))
                {
                    serializer.Serialize(original, gzipStream);
                }
                finalBytes = ms.ToArray();
            }

            // 读取解包顺序：先从 AES 解密，再由 GZip 解压
            SampleSettings restored;
            using (var ms = new MemoryStream(finalBytes))
            using (var aesReadStream = aes.WrapReadStream(ms))
            using (var gzipReadStream = gzip.WrapReadStream(aesReadStream))
            {
                restored = serializer.Deserialize<SampleSettings>(gzipReadStream);
            }

            Assert.IsNotNull(restored);
            Assert.AreEqual(original.AppName, restored.AppName);
            Assert.AreEqual(original.Version, restored.Version);
        }
    }
}
