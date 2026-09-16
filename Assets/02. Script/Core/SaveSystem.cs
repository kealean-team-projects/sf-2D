using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace _02._Script.Core {
    public static class SaveSystem {
        private static readonly byte[] Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("kealeanshash"));

        private static string GetSavePath(string fileName) {
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        public static void Save<T>(string fileName, T data) where T : class {
            try {
                var json = JsonConvert.SerializeObject(data, Formatting.None);
                var encryptedBytes = Encrypt(json);
                var path = GetSavePath(fileName);

                File.WriteAllBytes(path, encryptedBytes);
                Debug.Log($"성공 {path}");
            }
            catch (Exception ex) {
                Debug.LogError($"실패 \n{ex}");
            }
        }

        public static T Load<T>(string fileName) where T : class, new() {
            var path = GetSavePath(fileName);

            if (!File.Exists(path)) {
                Debug.LogWarning("없어");
                return default;
            }

            try {
                var encryptedBytes = File.ReadAllBytes(path);
                var json = Decrypt(encryptedBytes);
                return JsonConvert.DeserializeObject(json) as T;
            }
            catch (CryptographicException) {
                Debug.LogError("이상한거 안받음");
                return default;
            }
            catch (Exception ex) {
                Debug.LogError($"실패 \n{ex}");
                return default;
            }
        }

        private static byte[] Encrypt(string plainText) {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.GenerateIV();
            var iv = aes.IV;

            using var ms = new MemoryStream();
            ms.Write(iv, 0, iv.Length);

            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8)) {
                sw.Write(plainText);
            }

            return ms.ToArray();
        }

        private static string Decrypt(byte[] cipherData) {
            using var aes = Aes.Create();
            aes.Key = Key;

            using var ms = new MemoryStream(cipherData);
            var iv = new byte[16];
            ms.Read(iv, 0, iv.Length);
            aes.IV = iv;

            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }
    }
}