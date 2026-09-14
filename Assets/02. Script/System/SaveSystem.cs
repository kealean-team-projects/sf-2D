using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace _02._Script.System {
    public static class SaveSystem {
        private static readonly byte[] Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("kealeanshash"));

        private static string GetSavePath(string fileName) {
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        public static void Save(string fileName, Type data) {
            try {
                string json = JsonConvert.SerializeObject(data, Formatting.None);
                byte[] encryptedBytes = Encrypt(json);
                string path = GetSavePath(fileName);

                File.WriteAllBytes(path, encryptedBytes);
                Debug.Log("성공");
            }
            catch (Exception ex) {
                Debug.LogError("실패");
            }
        }

        public static object Load(string fileName) {
            string path = GetSavePath(fileName);

            if (!File.Exists(path)) {
                Debug.LogWarning("없어");
                return default;
            }

            try {
                byte[] encryptedBytes = File.ReadAllBytes(path);
                string json = Decrypt(encryptedBytes);
                return JsonConvert.DeserializeObject(json);
            }
            catch (CryptographicException) {
                Debug.LogError("이상한거 안받음");
                return default;
            }
            catch (Exception ex) {
                Debug.LogError("실패");
                return default;
            }
        }

        private static byte[] Encrypt(string plainText) {
            using (Aes aes = Aes.Create()) {
                aes.Key = Key;
                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (MemoryStream ms = new MemoryStream()) {
                    ms.Write(iv, 0, iv.Length);

                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (StreamWriter sw = new StreamWriter(cs, Encoding.UTF8)) {
                        sw.Write(plainText);
                    }

                    return ms.ToArray();
                }
            }
        }

        private static string Decrypt(byte[] cipherData) {
            using (Aes aes = Aes.Create()) {
                aes.Key = Key;

                using (MemoryStream ms = new MemoryStream(cipherData)) {
                    byte[] iv = new byte[16];
                    ms.Read(iv, 0, iv.Length);
                    aes.IV = iv;

                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (StreamReader sr = new StreamReader(cs, Encoding.UTF8)) {
                        return sr.ReadToEnd();
                    }
                }
            }
        }
    }
}