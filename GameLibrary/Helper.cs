namespace GameLibrary;

public static class Helper
{
    /// <summary>
    /// Rounds a float to an int with midpoint rounding set to "away from zero".
    /// </summary>
    /// <param name="f">The float value to round.</param>
    /// <returns>The rounded int.</returns>
    public static int RoundFloatToInt(float f)
    {
        if (f < 0)
        {
            return (int)(f - 0.5);
        }
        return (int)(f + 0.5);
    }
}