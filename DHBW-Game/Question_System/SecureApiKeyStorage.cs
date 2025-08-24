using System;
using System.IO;
using Microsoft.AspNetCore.DataProtection;

namespace DHBW_Game.Question_System;

/// <summary>
/// Provides methods for securely storing and retrieving an API key using data protection.
/// </summary>
public static class SecureApiKeyStorage
{
    // Unique purpose string for the data protector
    private const string KeyPurpose = "DHBW-Game.GeminiApiKey";
    
    // File name for storing the protected API key
    private const string KeyFileName = "apikey.dat";

    /// <summary>
    /// Gets the secure storage directory in the user's AppData folder.
    /// </summary>
    /// <returns>The path to the secure storage directory.</returns>
    private static string GetStorageDirectory()
    {
        // Construct the path to the secure storage directory
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DHBW-Game", "SecureStorage");
    }

    /// <summary>
    /// Creates a data protector instance using the cross-platform DataProtectionProvider.
    /// </summary>
    /// <returns>An <see cref="IDataProtector"/> instance for encrypting and decrypting data.</returns>
    private static IDataProtector GetProtector()
    {
        // Ensure the storage directory exists
        var dir = new DirectoryInfo(GetStorageDirectory());
        if (!dir.Exists)
        {
            dir.Create();
        }

        // Create a data protection provider for the directory
        var provider = DataProtectionProvider.Create(dir);
        
        // Return a protector instance with the specified purpose
        return provider.CreateProtector(KeyPurpose);
    }

    /// <summary>
    /// Securely saves the API key to a protected file.
    /// Deletes the file if the key parameter is null or empty.
    /// </summary>
    /// <param name="apiKey">The API key to save.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="apiKey"/> is null or empty.</exception>
    public static void SaveApiKey(string apiKey)
    {
        var filePath = Path.Combine(GetStorageDirectory(), KeyFileName);
    
        // Delete file if the key parameter is null or empty
        if (string.IsNullOrEmpty(apiKey))
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return;
        }

        // Get the data protector and encrypt the API key
        var protector = GetProtector();
        var protectedKey = protector.Protect(apiKey);

        // Write the encrypted key to the file
        File.WriteAllText(filePath, protectedKey);
    }

    /// <summary>
    /// Loads and decrypts the API key from the protected file.
    /// </summary>
    /// <returns>The decrypted API key, or null if the file is not found or decryption fails.</returns>
    public static string LoadApiKey()
    {
        // Construct the file path for the protected key
        var filePath = Path.Combine(GetStorageDirectory(), KeyFileName);
        
        // Check if the file exists
        if (!File.Exists(filePath))
        {
            return null;
        }

        // Read the encrypted key from the file
        var protectedKey = File.ReadAllText(filePath);
        
        // Get the data protector
        var protector = GetProtector();

        try
        {
            // Attempt to decrypt the key
            return protector.Unprotect(protectedKey);
        }
        catch (Exception)
        {
            // Return null if decryption fails (e.g., corrupted file or wrong machine/user)
            return null;
        }
    }
}