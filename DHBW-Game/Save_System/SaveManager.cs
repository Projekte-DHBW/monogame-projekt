using System;
using System.IO;

namespace DHBW_Game.Save_System;

/// <summary>
/// Provides methods for saving, loading, and resetting game progress.
/// Currently stores only the current level number.
/// </summary>
public static class SaveManager
{
    // File name for storing the progress
    private const string SaveFileName = "progress.dat";

    /// <summary>
    /// Gets the storage directory in the user's AppData folder.
    /// </summary>
    /// <returns>The path to the storage directory.</returns>
    private static string GetStorageDirectory()
    {
        // Construct the path to the storage directory
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DHBW-Game", "Saves");
    }

    /// <summary>
    /// Saves the current level progress to a file.
    /// </summary>
    /// <param name="levelNumber">The level number to save.</param>
    public static void SaveProgress(int levelNumber)
    {
        var filePath = Path.Combine(GetStorageDirectory(), SaveFileName);

        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);

        // Write the level number to the file
        File.WriteAllText(filePath, levelNumber.ToString());
    }

    /// <summary>
    /// Loads the saved level progress from the file.
    /// </summary>
    /// <returns>The saved level number, or 0 if no save exists or parsing fails.</returns>
    public static int LoadProgress()
    {
        var filePath = Path.Combine(GetStorageDirectory(), SaveFileName);

        // Check if the file exists
        if (!File.Exists(filePath))
        {
            return 0;
        }

        // Read the content
        var content = File.ReadAllText(filePath);

        // Try to parse the level number
        if (int.TryParse(content, out int levelNumber))
        {
            return levelNumber;
        }

        // Return 0 on parsing failure
        return 0;
    }

    /// <summary>
    /// Resets the saved progress by deleting the save file.
    /// </summary>
    public static void ResetProgress()
    {
        var filePath = Path.Combine(GetStorageDirectory(), SaveFileName);

        // Delete the file if it exists
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}