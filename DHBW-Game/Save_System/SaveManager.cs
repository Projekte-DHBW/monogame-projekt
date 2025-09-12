using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace DHBW_Game.Save_System;

/// <summary>
/// Provides methods for saving, loading, and resetting game progress.
/// Currently stores only the current level number.
/// </summary>
public static class SaveManager
{
    // File name for storing the progress
    private const string SaveFileName = "progress.dat";
    private const string GradesFileName = "grades.dat";

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
    /// Saves the grades list to a file.
    /// </summary>
    /// <param name="grades">The list of grades to save.</param>
    public static void SaveGrades(List<double> grades)
    {
        var filePath = Path.Combine(GetStorageDirectory(), GradesFileName);

        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);

        // Write each grade to a line in the file using invariant culture
        File.WriteAllLines(filePath, grades.Select(g => g.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Loads the saved grades from the file.
    /// </summary>
    /// <returns>The list of saved grades, or an empty list if no save exists or parsing fails.</returns>
    public static List<double> LoadGrades()
    {
        var filePath = Path.Combine(GetStorageDirectory(), GradesFileName);

        // Check if the file exists
        if (!File.Exists(filePath))
        {
            return new List<double>();
        }

        // Read all lines
        var lines = File.ReadAllLines(filePath);

        // Parse each line to double using invariant culture
        return lines.Select(line => double.TryParse(line, NumberStyles.Any, CultureInfo.InvariantCulture, out double grade) ? grade : 0.0).ToList();
    }

    /// <summary>
    /// Resets the saved progress by deleting the save file.
    /// </summary>
    public static void ResetProgress()
    {
        var progressFilePath = Path.Combine(GetStorageDirectory(), SaveFileName);
        var gradesFilePath = Path.Combine(GetStorageDirectory(), GradesFileName);

        // Delete the files if they exist
        if (File.Exists(progressFilePath))
        {
            File.Delete(progressFilePath);
        }

        if (File.Exists(gradesFilePath))
        {
            File.Delete(gradesFilePath);
        }
    }
}