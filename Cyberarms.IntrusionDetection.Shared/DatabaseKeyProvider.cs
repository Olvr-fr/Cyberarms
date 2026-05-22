using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Cyberarms.IntrusionDetection.Shared {
    internal static class DatabaseKeyProvider {
        private const string ENV_VAR = "CYBERARMS_DB_KEY";
        private static readonly string KeyFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Cyberarms", "cyberarms.key");

        public static string GetKey() {
            string? envKey = Environment.GetEnvironmentVariable(ENV_VAR);
            if (!string.IsNullOrWhiteSpace(envKey)) return envKey;
            return LoadOrCreateKey();
        }

        private static string LoadOrCreateKey() {
            string dir = Path.GetDirectoryName(KeyFilePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (File.Exists(KeyFilePath)) {
                byte[] protectedData = File.ReadAllBytes(KeyFilePath);
                byte[] rawKey = ProtectedData.Unprotect(
                    protectedData, null, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(rawKey);
            }

            byte[] keyBytes = RandomNumberGenerator.GetBytes(32);
            string key = Convert.ToBase64String(keyBytes);
            byte[] protectedKey = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(key), null, DataProtectionScope.LocalMachine);
            File.WriteAllBytes(KeyFilePath, protectedKey);
            return key;
        }
    }
}
