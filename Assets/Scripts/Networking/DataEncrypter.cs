using Sirenix.OdinInspector;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class DataEncrypter : MonoBehaviour
{
    //API information
    [SerializeField] private string apiURL;
    [SerializeField] private string apiKey;
    [SerializeField] private string apiToken;
    [SerializeField] private string listId;

    //Label information
    [SerializeField] private string mildLabelID;
    [SerializeField] private string moderateLabelID;
    [SerializeField] private string severeLabelID;

    [Button]
    public void CreateEncryptedFile()
    {
        //Create the class with the data to encrypt
        APIInformation apiInfo = new APIInformation
        {
            apiURL = apiURL,
            apiKey = apiKey,
            apiToken = apiToken,
            listID = listId,
            mildLabelID = mildLabelID,
            moderateLabelID = moderateLabelID,
            severeLabelID = severeLabelID
        };
        string jsonData = JsonUtility.ToJson(apiInfo);

        string encryptedFilePath = Application.dataPath + "/bug_report_api_info.dat";
        string encryptionKey = GenerateRandomKey(16);

        //Encrypt the file and show the encryption key
        CreateEncryptedFile(jsonData, encryptedFilePath, encryptionKey);
        Debug.Log("File encrypted and saved at: " + encryptedFilePath);
        Debug.Log("Encryption Key: " + encryptionKey);
        GameSettings.CopyToClipboard(encryptionKey);
    }

    /// <summary>
    /// Creates an encrypted data file.
    /// </summary>
    /// <param name="fileData">The data to encrypt.</param>
    /// <param name="filePath">The path of the file.</param>
    /// <param name="key">The encryption key used.</param>
    public static void CreateEncryptedFile(string fileData, string filePath, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] iv = new byte[16];

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = iv;

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (CryptoStream csEncrypt = new CryptoStream(fs, encryptor, CryptoStreamMode.Write))
            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(fileData);
            }
        }
    }

    /// <summary>
    /// Decrypts a file to get its information.
    /// </summary>
    /// <param name="filePath">The path of the file.</param>
    /// <param name="key">The encryption key (the same one used to encrypt the file initially).</param>
    /// <returns>Returns the decrypted data information.</returns>
    public static string DecryptFile(string filePath, string key)
    {
        //If there is no path, return
        if (filePath.Length == 0)
            return null;

        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] iv = new byte[16];

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = iv;

            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (CryptoStream csDecrypt = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }
    }

    /// <summary>
    /// Generates a random key for encryption.
    /// </summary>
    /// <param name="keySize">The byte size of the key.</param>
    /// <returns>A key that can be used for encryption.</returns>
    private static string GenerateRandomKey(int keySize)
    {
        byte[] key = new byte[keySize];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(key);
        }
        return Convert.ToBase64String(key);
    }
}
