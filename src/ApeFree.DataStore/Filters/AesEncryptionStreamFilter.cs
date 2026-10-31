using System;
using System.IO;
using System.Security.Cryptography;
using ApeFree.DataStore.Core;

namespace ApeFree.DataStore.Filters
{
    /// <summary>
    /// AES 对称加密流式过滤器
    /// </summary>
    public class AesEncryptionStreamFilter : IStreamFilter
    {
        public byte[] Key { get; }
        public byte[] IV { get; }
        public CipherMode CipherMode { get; set; } = CipherMode.CBC;
        public PaddingMode PaddingMode { get; set; } = PaddingMode.PKCS7;

        public AesEncryptionStreamFilter(byte[] key, byte[] iv)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            IV = iv ?? throw new ArgumentNullException(nameof(iv));
        }

        private SymmetricAlgorithm CreateAlgorithm()
        {
            var aes = Aes.Create();
            aes.Mode = CipherMode;
            aes.Padding = PaddingMode;
            aes.Key = Key;
            aes.IV = IV;
            return aes;
        }

        /// <inheritdoc/>
        public Stream WrapWriteStream(Stream outputStream)
        {
            var aes = CreateAlgorithm();
            var encryptor = aes.CreateEncryptor();
            return new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);
        }

        /// <inheritdoc/>
        public Stream WrapReadStream(Stream inputStream)
        {
            var aes = CreateAlgorithm();
            var decryptor = aes.CreateDecryptor();
            return new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);
        }

        /// <inheritdoc/>
        public void Dispose() { }
    }
}
