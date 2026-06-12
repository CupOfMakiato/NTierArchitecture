using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using NTierArchitecture.Application.IServices;
using NTierArchitecture.Application.Settings;

namespace NTierArchitecture.Infrastructure.Services
{
    public class AesEncryptionService : IEncryptionService
    {
        private const string Version = "v1";
        private const int KeySizeInBytes = 32;
        private const int NonceSizeInBytes = 12;
        private const int TagSizeInBytes = 16;

        private readonly byte[] _key;

        public AesEncryptionService(IOptions<EncryptionSettings> options)
        {
            var keyValue = options.Value.AesKey;
            if (string.IsNullOrWhiteSpace(keyValue))
            {
                throw new InvalidOperationException($"{EncryptionSettings.SectionName}:AesKey is missing.");
            }

            try
            {
                _key = Convert.FromBase64String(keyValue);
            }
            catch (FormatException exception)
            {
                throw new InvalidOperationException(
                    $"{EncryptionSettings.SectionName}:AesKey must be a base64-encoded 32-byte key.",
                    exception);
            }

            if (_key.Length != KeySizeInBytes)
            {
                throw new InvalidOperationException(
                    $"{EncryptionSettings.SectionName}:AesKey must decode to exactly {KeySizeInBytes} bytes.");
            }

            //if (!AesGcm.IsSupported)
            //{
            //    throw new PlatformNotSupportedException("AES-GCM is not supported on this platform.");
            //}
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return plainText;
            }

            var nonce = RandomNumberGenerator.GetBytes(NonceSizeInBytes);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[TagSizeInBytes];

            using var aesGcm = new AesGcm(_key, TagSizeInBytes);
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

            return string.Join(
                ':',
                Version,
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(tag),
                Convert.ToBase64String(cipherBytes));
            // end ciphertext: v1:<nonceBase64>:<tagBase64>:<cipherTextBase64>
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return cipherText;
            }

            if (!cipherText.StartsWith($"{Version}:", StringComparison.Ordinal))
            {
                return cipherText;
            }

            var parts = cipherText.Split(':');
            if (parts.Length != 4 || parts[0] != Version)
            {
                throw new FormatException("Encrypted value has an invalid format.");
            }

            var nonce = Convert.FromBase64String(parts[1]);
            var tag = Convert.FromBase64String(parts[2]);
            var cipherBytes = Convert.FromBase64String(parts[3]);
            var plainBytes = new byte[cipherBytes.Length];

            using var aesGcm = new AesGcm(_key, TagSizeInBytes);
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
