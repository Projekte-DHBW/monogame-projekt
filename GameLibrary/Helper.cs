using System;

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

    /// <summary>
    /// Normalizes an angle in radians to the range [0, 2π).
    /// This handles negative angles or angles greater than 2π consistently, e.g., for slope physics.
    /// </summary>
    /// <param name="angle">The angle in radians to normalize.</param>
    /// <returns>The normalized angle in [0, 2π).</returns>
    public static float NormalizeAngleRadians(float angle)
    {
        float twoPi = (float)(2 * Math.PI);
        return ((angle % twoPi) + twoPi) % twoPi;
    }

    /// <summary>
    /// Computes the effective absolute slope angle in radians, representing the smallest deviation from flat (horizontal).
    /// This is useful for checks like tolerances on slopes or wall jumps.
    /// </summary>
    /// <param name="angle">The slope angle in radians.</param>
    /// <returns>The effective angle in [0, π/2].</returns>
    public static float GetEffectiveSlopeAngleRadians(float angle)
    {
        float pi = (float)Math.PI;
        float norm = NormalizeAngleRadians(angle);
        float modPi = norm % pi;
        return Math.Min(modPi, pi - modPi);
    }
}