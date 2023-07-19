using ApeFree.DataStore.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ApeFree.DataStore.Adapters
{
    public class AesEncryptionAdapter : BaseEncryptionAdapter
    {
        private ICryptoTransform encryptor;
        private ICryptoTransform decryptor;

        /// <summary>
        /// 指定要用于加密的块密码模式
        /// </summary>
        public CipherMode CipherMode { get; set; } = CipherMode.CBC;

        /// <summary>
        /// 指定在消息数据块短于加密操作所需的完整字节数时要应用的填充类型
        /// </summary>
        public PaddingMode PaddingMode { get; set; } = PaddingMode.PKCS7;

        /// <summary>
        /// 获取或设置加密操作的块大小（以位为单位）
        /// </summary>
        public int BlockSize { get; set; } = 128;

        /// <summary>
        /// 获取或设置用于对称算法的密钥大小（以位为单位）
        /// </summary>
        public int KeySize { get; set; } = 256;

        /// <summary>
        /// 用于对称算法的密钥（长度为[KeySize/8]字节）
        /// </summary>
        public byte[] RgbKey { get; }

        /// <summary>
        /// 用于对称算法的初始化向量（长度为16字节）
        /// </summary>
        public byte[] RgbIV { get; }

        public AesEncryptionAdapter(byte[] rgbKey, byte[] rgbIV)
        {
            RgbKey = rgbKey;
            RgbIV = rgbIV;

            Init();
        }

        private void Init()
        {
            encryptor?.Dispose();
            decryptor?.Dispose();

            using (var managed = new AesManaged()
            {
                Mode = CipherMode,
                KeySize = KeySize,
                Padding = PaddingMode,
                BlockSize = BlockSize,
                Key = RgbKey,
                IV = RgbIV,
            })
            {
                encryptor = managed.CreateEncryptor();
                decryptor = managed.CreateDecryptor();
            }
        }

        /// <inheritdoc/>
        public override Stream Encode(Stream stream)
        {
            return new CryptoStream(stream, encryptor, CryptoStreamMode.Read);
        }

        /// <inheritdoc/>
        public override Stream Decode(Stream stream)
        {
            return new CryptoStream(stream, decryptor, CryptoStreamMode.Read);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            encryptor?.Dispose();
            decryptor?.Dispose();
        }
    }
}
