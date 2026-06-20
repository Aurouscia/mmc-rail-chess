using System.Security.Cryptography;
using System.Text;

namespace RailChess.Utils
{
    /// <summary>
    /// API 响应加密服务接口
    /// </summary>
    public interface IApiEncryption
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }

    /// <summary>
    /// 使用 AES-256-GCM 对 HTTP API 响应中的 data 字段进行加密/解密。
    /// 密钥由 IConfiguration 中的三段配置拼接后经 SHA-256 派生得到。
    /// </summary>
    public class ApiEncryption : IApiEncryption
    {
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private readonly byte[] _key;

        public ApiEncryption(IConfiguration configuration)
        {
            var part1 = configuration["ApiEncryption:KeyP1"] ?? "";
            var part2 = configuration["ApiEncryption:KeyP2"] ?? "";
            var part3 = configuration["ApiEncryption:KeyP3"] ?? "";
            var raw = part1 + part2 + part3;

            if (string.IsNullOrEmpty(raw))
                throw new InvalidOperationException("未配置 ApiEncryption:KeyP1/P2/P3，无法初始化 API 加密");

            _key = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        }

        /// <summary>
        /// 加密字符串，返回格式：Base64(iv).Base64(cipher).Base64(tag)
        /// </summary>
        public string Encrypt(string plainText)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[TagSize];
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            return $"{Convert.ToBase64String(nonce)}.{Convert.ToBase64String(cipherBytes)}.{Convert.ToBase64String(tag)}";
        }

        /// <summary>
        /// 解密字符串，输入格式：Base64(iv).Base64(cipher).Base64(tag)
        /// </summary>
        public string Decrypt(string cipherText)
        {
            var parts = cipherText.Split('.');
            if (parts.Length != 3)
                throw new FormatException("密文格式错误，应为 iv.cipher.tag");

            var nonce = Convert.FromBase64String(parts[0]);
            var cipherBytes = Convert.FromBase64String(parts[1]);
            var tag = Convert.FromBase64String(parts[2]);

            if (nonce.Length != NonceSize)
                throw new CryptographicException($"IV 长度错误，期望 {NonceSize}，实际 {nonce.Length}");
            if (tag.Length != TagSize)
                throw new CryptographicException($"Tag 长度错误，期望 {TagSize}，实际 {tag.Length}");

            var plainBytes = new byte[cipherBytes.Length];
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
