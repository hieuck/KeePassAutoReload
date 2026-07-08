using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace KeePassAutoReload
{
    internal static class AssetVerifier
    {
        public static string ComputeSha256(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath");

            using (FileStream stream = File.OpenRead(filePath))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(stream);
                return BytesToHex(hash);
            }
        }

        public static Dictionary<string, string> ParseChecksums(string checksumsText)
        {
            Dictionary<string, string> entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(checksumsText)) return entries;

            using (StringReader reader = new StringReader(checksumsText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    int separatorIndex = trimmed.IndexOf(' ');
                    if (separatorIndex <= 0) continue;

                    string hash = trimmed.Substring(0, separatorIndex).Trim();
                    string fileName = trimmed.Substring(separatorIndex + 1).Trim();

                    if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(fileName)) continue;

                    entries[fileName] = hash;
                }
            }

            return entries;
        }

        public static bool VerifyFile(string filePath, string expectedHash)
        {
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(expectedHash)) return false;
            if (!File.Exists(filePath)) return false;

            string actualHash = ComputeSha256(filePath);
            return string.Equals(actualHash, expectedHash.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static string BytesToHex(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
