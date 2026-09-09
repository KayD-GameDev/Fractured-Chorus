using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace FracturedChorus.Meta
{
    public enum SaveBlobStatus
    {
        /// <summary>Giải mã thành công, chữ ký HMAC khớp.</summary>
        Success,

        /// <summary>Blob rỗng hoặc quá ngắn để chứa header.</summary>
        Empty,

        /// <summary>Không có magic "FCS1" — đây là save JSON thuần đời cũ, đọc thẳng được.</summary>
        Plaintext,

        /// <summary>HMAC không khớp: file bị sửa tay hoặc hỏng.</summary>
        Tampered,

        /// <summary>Định dạng nhận ra được nhưng version mới hơn bản build này.</summary>
        UnsupportedVersion,

        /// <summary>Lỗi mật mã hoặc I/O ngoài dự kiến.</summary>
        Failed
    }

    /// <summary>
    /// Mã hóa save theo kiểu encrypt-then-MAC: AES-256-CBC cho nội dung, HMAC-SHA256 cho toàn vẹn.
    /// Mục tiêu là chặn người chơi sửa file bằng tay, không phải chống dịch ngược —
    /// khóa nằm trong binary nên ai quyết tâm vẫn moi ra được.
    /// </summary>
    public static class SaveCrypto
    {
        public const string EncryptedExtension = ".fcsav";
        public const string PlaintextExtension = ".json";

        private const byte FormatVersion = 1;
        private const int IvSize = 16;
        private const int MacSize = 32;
        private const int Pbkdf2Iterations = 100_000;

        private static readonly byte[] Magic = { (byte)'F', (byte)'C', (byte)'S', (byte)'1' };
        private static readonly int HeaderSize = Magic.Length + 1 + IvSize + MacSize;

        /// <summary>
        /// Passphrase không nằm nguyên chuỗi trong binary: nó là XOR của hai mảng dưới đây,
        /// nên `strings` trên file build sẽ không lôi ra được.
        /// </summary>
        private static readonly byte[] KeyMaterialA =
        {
            0x8F, 0x2C, 0x41, 0xD7, 0x63, 0xB0, 0x1E, 0xA5,
            0x39, 0xC8, 0x7D, 0x04, 0xE2, 0x5B, 0x96, 0x3F,
            0xA1, 0x68, 0xD4, 0x0B, 0x77, 0xEC, 0x22, 0x59,
            0xBD, 0x10, 0x84, 0xF3, 0x46, 0x9A, 0x2D, 0xC7
        };

        private static readonly byte[] KeyMaterialB =
        {
            0x51, 0xA3, 0x0E, 0x6C, 0xF8, 0x27, 0x9D, 0x34,
            0xB6, 0x4F, 0xE1, 0x8A, 0x05, 0xD3, 0x72, 0xC9,
            0x1B, 0x95, 0x3E, 0xA7, 0x60, 0x08, 0xCF, 0xB2,
            0x47, 0xDA, 0x29, 0x16, 0xE5, 0x73, 0x8C, 0x30
        };

        private static readonly byte[] Salt =
        {
            0x46, 0x43, 0x53, 0x76, 0x31, 0x9E, 0x2B, 0x74,
            0xC5, 0x18, 0xAF, 0x63, 0xD0, 0x37, 0x8B, 0xE4
        };

        private static byte[] s_aesKey;
        private static byte[] s_macKey;

#if UNITY_EDITOR
        /// <summary>
        /// Bật trong Editor để ghi save dạng JSON đọc được bằng mắt.
        /// Giải mã luôn nhận cả hai dạng nên bật/tắt giữa chừng không làm hỏng save.
        /// </summary>
        public static bool DebugPlaintext;
#endif

        public static bool LooksEncrypted(byte[] blob)
        {
            if (blob == null || blob.Length < HeaderSize)
            {
                return false;
            }

            for (var i = 0; i < Magic.Length; i++)
            {
                if (blob[i] != Magic[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static byte[] Encrypt(string json)
        {
            var plain = Encoding.UTF8.GetBytes(json ?? string.Empty);

#if UNITY_EDITOR
            if (DebugPlaintext)
            {
                return plain;
            }
#endif

            EnsureKeys();

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = s_aesKey;
            aes.GenerateIV();

            var iv = aes.IV;
            byte[] cipher;
            using (var encryptor = aes.CreateEncryptor())
            {
                cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
            }

            var mac = ComputeMac(iv, cipher);

            var output = new byte[HeaderSize + cipher.Length];
            var offset = 0;
            Buffer.BlockCopy(Magic, 0, output, offset, Magic.Length);
            offset += Magic.Length;
            output[offset++] = FormatVersion;
            Buffer.BlockCopy(iv, 0, output, offset, IvSize);
            offset += IvSize;
            Buffer.BlockCopy(mac, 0, output, offset, MacSize);
            offset += MacSize;
            Buffer.BlockCopy(cipher, 0, output, offset, cipher.Length);

            return output;
        }

        public static SaveBlobStatus TryDecrypt(byte[] blob, out string json)
        {
            json = null;

            if (blob == null || blob.Length == 0)
            {
                return SaveBlobStatus.Empty;
            }

            if (!LooksEncrypted(blob))
            {
                // Save JSON đời cũ (hoặc file do DebugPlaintext ghi ra) — vẫn đọc được.
                try
                {
                    json = Encoding.UTF8.GetString(blob);
                    return SaveBlobStatus.Plaintext;
                }
                catch (Exception error)
                {
                    Debug.LogError($"[Fractured Chorus] SaveCrypto: không đọc nổi save dạng plaintext: {error}");
                    return SaveBlobStatus.Failed;
                }
            }

            var version = blob[Magic.Length];
            if (version > FormatVersion)
            {
                Debug.LogError(
                    $"[Fractured Chorus] SaveCrypto: save dùng format version {version}, " +
                    $"bản build này chỉ đọc tới {FormatVersion}.");
                return SaveBlobStatus.UnsupportedVersion;
            }

            try
            {
                EnsureKeys();

                var offset = Magic.Length + 1;
                var iv = new byte[IvSize];
                Buffer.BlockCopy(blob, offset, iv, 0, IvSize);
                offset += IvSize;

                var storedMac = new byte[MacSize];
                Buffer.BlockCopy(blob, offset, storedMac, 0, MacSize);
                offset += MacSize;

                var cipher = new byte[blob.Length - HeaderSize];
                Buffer.BlockCopy(blob, offset, cipher, 0, cipher.Length);

                var expectedMac = ComputeMac(iv, cipher);
                if (!ConstantTimeEquals(storedMac, expectedMac))
                {
                    return SaveBlobStatus.Tampered;
                }

                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = s_aesKey;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                json = Encoding.UTF8.GetString(plain);
                return SaveBlobStatus.Success;
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] SaveCrypto: giải mã thất bại: {error}");
                return SaveBlobStatus.Failed;
            }
        }

        private static byte[] ComputeMac(byte[] iv, byte[] cipher)
        {
            // MAC phủ magic + version + IV + ciphertext để không ai đổi được IV hay tráo file giữa các slot.
            var payload = new byte[Magic.Length + 1 + IvSize + cipher.Length];
            var offset = 0;
            Buffer.BlockCopy(Magic, 0, payload, offset, Magic.Length);
            offset += Magic.Length;
            payload[offset++] = FormatVersion;
            Buffer.BlockCopy(iv, 0, payload, offset, IvSize);
            offset += IvSize;
            Buffer.BlockCopy(cipher, 0, payload, offset, cipher.Length);

            using var hmac = new HMACSHA256(s_macKey);
            return hmac.ComputeHash(payload);
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }

        private static void EnsureKeys()
        {
            if (s_aesKey != null && s_macKey != null)
            {
                return;
            }

            var passphrase = new byte[KeyMaterialA.Length];
            for (var i = 0; i < passphrase.Length; i++)
            {
                passphrase[i] = (byte)(KeyMaterialA[i] ^ KeyMaterialB[i]);
            }

            // PBKDF2 tốn ~100ms nên chỉ chạy một lần rồi cache — liệt kê 10 slot không bị khựng.
            using (var kdf = new Rfc2898DeriveBytes(passphrase, Salt, Pbkdf2Iterations, HashAlgorithmName.SHA256))
            {
                s_aesKey = kdf.GetBytes(32);
                s_macKey = kdf.GetBytes(32);
            }

            Array.Clear(passphrase, 0, passphrase.Length);
        }
    }
}
