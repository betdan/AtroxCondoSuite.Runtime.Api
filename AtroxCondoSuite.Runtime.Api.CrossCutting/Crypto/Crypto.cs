namespace AtroxCondoSuite.Runtime.Api.CrossCutting.Crypto
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System.Security.Cryptography;
    using System.Text;

    public class Crypto
    {
        private readonly ILogger<Crypto> _logger;
        private readonly byte[] _initializationVector;
        private readonly string _key;

        public Crypto(IConfiguration configuration, ILogger<Crypto> logger)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _initializationVector = Encoding.ASCII.GetBytes(configuration["IV_CRYPT"] ?? string.Empty);
            _key = configuration["KEY_CRYPT"] ?? string.Empty;
        }

        public string Encrypt(string text)
        {
            using var cipher = CreateCipher(_key);
            cipher.IV = _initializationVector;

            using var cryptTransform = cipher.CreateEncryptor();
            var plaintext = Encoding.UTF8.GetBytes(text);
            var cipherText = cryptTransform.TransformFinalBlock(plaintext, 0, plaintext.Length);
            return Convert.ToBase64String(cipherText);
        }

        public string Decrypt(string encryptedText)
        {
            _logger.LogDebug("Decrypting SQL connection secret material.");

            using var cipher = CreateCipher(_key);
            cipher.IV = _initializationVector;

            using var cryptTransform = cipher.CreateDecryptor();
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var plainBytes = cryptTransform.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private static Aes CreateCipher(string key)
        {
            var cipher = Aes.Create();
            cipher.Mode = CipherMode.CBC;
            cipher.Padding = PaddingMode.PKCS7;
            cipher.Key = Encoding.ASCII.GetBytes(key);
            return cipher;
        }
    }
}

