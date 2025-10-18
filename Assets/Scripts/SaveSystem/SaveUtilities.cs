using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Utility methods for save system operations including encryption, compression, and validation
/// </summary>
public static class SaveUtilities
{
    #region Encryption Utilities

    /// <summary>
    /// Generates a random encryption key
    /// </summary>
    public static string GenerateEncryptionKey(int keySize = 32)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        StringBuilder result = new StringBuilder(keySize);
        System.Random random = new System.Random();
        
        for (int i = 0; i < keySize; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Generates a hash of the data for integrity verification
    /// </summary>
    public static string GenerateHash(byte[] data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }
    }

    /// <summary>
    /// Verifies data integrity using hash
    /// </summary>
    public static bool VerifyHash(byte[] data, string expectedHash)
    {
        string actualHash = GenerateHash(data);
        return actualHash == expectedHash;
    }

    /// <summary>
    /// Encrypts a string using AES
    /// </summary>
    public static string EncryptString(string plainText, string key)
    {
        byte[] encrypted = EncryptBytes(Encoding.UTF8.GetBytes(plainText), key);
        return Convert.ToBase64String(encrypted);
    }

    /// <summary>
    /// Decrypts a string using AES
    /// </summary>
    public static string DecryptString(string encryptedText, string key)
    {
        byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
        byte[] decrypted = DecryptBytes(encryptedBytes, key);
        return Encoding.UTF8.GetString(decrypted);
    }

    /// <summary>
    /// Encrypts bytes using AES
    /// </summary>
    public static byte[] EncryptBytes(byte[] data, string key)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = GetKeyBytes(key);
            aes.GenerateIV();
            
            using (MemoryStream output = new MemoryStream())
            {
                output.Write(aes.IV, 0, aes.IV.Length);
                
                using (CryptoStream cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                }
                
                return output.ToArray();
            }
        }
    }

    /// <summary>
    /// Decrypts bytes using AES
    /// </summary>
    public static byte[] DecryptBytes(byte[] data, string key)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = GetKeyBytes(key);
            
            using (MemoryStream input = new MemoryStream(data))
            {
                byte[] iv = new byte[aes.IV.Length];
                input.Read(iv, 0, iv.Length);
                aes.IV = iv;
                
                using (CryptoStream cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (MemoryStream output = new MemoryStream())
                {
                    cryptoStream.CopyTo(output);
                    return output.ToArray();
                }
            }
        }
    }

    /// <summary>
    /// Converts a string key to byte array of correct size
    /// </summary>
    private static byte[] GetKeyBytes(string key)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        }
    }

    #endregion

    #region File Utilities

    /// <summary>
    /// Gets the file size in bytes
    /// </summary>
    public static long GetFileSize(string filePath)
    {
        if (File.Exists(filePath))
        {
            FileInfo fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }
        return 0;
    }

    /// <summary>
    /// Gets the file size as a formatted string
    /// </summary>
    public static string GetFileSizeFormatted(string filePath)
    {
        long bytes = GetFileSize(filePath);
        return FormatBytes(bytes);
    }

    /// <summary>
    /// Formats bytes to human-readable format
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Creates a backup of a file
    /// </summary>
    public static bool CreateBackup(string filePath, string backupPath = null)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"File not found: {filePath}");
                return false;
            }

            if (string.IsNullOrEmpty(backupPath))
            {
                string directory = Path.GetDirectoryName(filePath);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string extension = Path.GetExtension(filePath);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                backupPath = Path.Combine(directory, $"{fileName}_backup_{timestamp}{extension}");
            }

            File.Copy(filePath, backupPath, true);
            Debug.Log($"Backup created: {backupPath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create backup: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Safely deletes a file
    /// </summary>
    public static bool SafeDelete(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete file: {e.Message}");
            return false;
        }
    }

    #endregion

    #region Validation Utilities

    /// <summary>
    /// Validates if a file path is valid
    /// </summary>
    public static bool IsValidPath(string path)
    {
        try
        {
            Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if a file name is valid
    /// </summary>
    public static bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        char[] invalidChars = Path.GetInvalidFileNameChars();
        return fileName.IndexOfAny(invalidChars) < 0;
    }

    /// <summary>
    /// Sanitizes a file name by removing invalid characters
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        StringBuilder sanitized = new StringBuilder(fileName);
        
        foreach (char c in invalidChars)
        {
            sanitized.Replace(c, '_');
        }
        
        return sanitized.ToString();
    }

    #endregion

    #region JSON Utilities

    /// <summary>
    /// Pretty prints JSON data
    /// </summary>
    public static string PrettyPrintJson(string json)
    {
        try
        {
            // Unity's JsonUtility doesn't have pretty print built-in
            // This is a simple implementation
            int indent = 0;
            StringBuilder result = new StringBuilder();
            bool inString = false;
            
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                
                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                {
                    inString = !inString;
                }
                
                if (!inString)
                {
                    if (c == '{' || c == '[')
                    {
                        result.Append(c);
                        result.Append('\n');
                        indent++;
                        result.Append(new string(' ', indent * 2));
                    }
                    else if (c == '}' || c == ']')
                    {
                        result.Append('\n');
                        indent--;
                        result.Append(new string(' ', indent * 2));
                        result.Append(c);
                    }
                    else if (c == ',')
                    {
                        result.Append(c);
                        result.Append('\n');
                        result.Append(new string(' ', indent * 2));
                    }
                    else if (c == ':')
                    {
                        result.Append(c);
                        result.Append(' ');
                    }
                    else if (!char.IsWhiteSpace(c))
                    {
                        result.Append(c);
                    }
                }
                else
                {
                    result.Append(c);
                }
            }
            
            return result.ToString();
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    /// Validates if a string is valid JSON
    /// </summary>
    public static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        json = json.Trim();
        
        if ((json.StartsWith("{") && json.EndsWith("}")) ||
            (json.StartsWith("[") && json.EndsWith("]")))
        {
            try
            {
                // Try to parse it
                JsonUtility.FromJson<object>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        return false;
    }

    #endregion

    #region Time Utilities

    /// <summary>
    /// Formats a DateTime as a save-friendly string
    /// </summary>
    public static string FormatSaveTime(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Gets a timestamp for file naming
    /// </summary>
    public static string GetTimestamp()
    {
        return DateTime.Now.ToString("yyyyMMdd_HHmmss");
    }

    /// <summary>
    /// Gets elapsed time since last save in a human-readable format
    /// </summary>
    public static string GetElapsedTime(DateTime lastSaveTime)
    {
        TimeSpan elapsed = DateTime.UtcNow - lastSaveTime;
        
        if (elapsed.TotalMinutes < 1)
            return "Just now";
        else if (elapsed.TotalHours < 1)
            return $"{(int)elapsed.TotalMinutes} minute(s) ago";
        else if (elapsed.TotalDays < 1)
            return $"{(int)elapsed.TotalHours} hour(s) ago";
        else
            return $"{(int)elapsed.TotalDays} day(s) ago";
    }

    #endregion

    #region Debug Utilities

    /// <summary>
    /// Logs save system information
    /// </summary>
    public static void LogSaveInfo(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.Log($"File does not exist: {filePath}");
            return;
        }

        FileInfo fileInfo = new FileInfo(filePath);
        Debug.Log($"Save File Info:\n" +
                  $"Path: {filePath}\n" +
                  $"Size: {FormatBytes(fileInfo.Length)}\n" +
                  $"Created: {fileInfo.CreationTime}\n" +
                  $"Modified: {fileInfo.LastWriteTime}");
    }

    /// <summary>
    /// Gets save file metadata as a string
    /// </summary>
    public static string GetSaveFileInfo(string filePath)
    {
        if (!File.Exists(filePath))
            return "File does not exist";

        FileInfo fileInfo = new FileInfo(filePath);
        return $"Size: {FormatBytes(fileInfo.Length)} | " +
               $"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}";
    }

    #endregion
}