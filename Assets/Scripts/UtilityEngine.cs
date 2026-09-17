using Unity.Mathematics;
using UnityEngine;

public static class UtilityEngine
{
    /// <summary>
    /// Linear response curve mapped between 0 and 1
    /// </summary>
    public static float Linear(float value, float min, float max)
    {
        return math.saturate((value-min) / (max-min));
    }

    /// <summary>
    /// Exponential curve for accelerating urgency as a metric
    /// </summary>
    public static float Exponential(float value, float min, float max, float exponent = 2.0f)
    {
        float norm = Linear(value, min, max);
        return math.pow(norm, exponent);
    }

    /// <summary>
    /// Logistic (S-Curve) for smooth transitions around an inflection point
    /// </summary>
    public static float Logistic(float value, float midpoint, float steepness = 10f)
    {
        return 1.0f / (1.0f + math.exp(-steepness * (value - midpoint)));
    }

    /// <summary>
    /// Step threshold (0 = Below, 1 = Above)
    /// </summary>
    public static float Step(float value, float threshold)
    {
        return value >= threshold ? 1.0f : 0.0f;
    }
}

/// <summary>
/// Allocation frtee struct containing squad tactical metrics
/// </summary>
public struct SquadTaticalContext
{
    public float averageDistanceToThreat;
    public float averageSquadHealth;
    public float flankExposureScore;
    public int totalActiveMembers;
    
    public float retreatUtility;
    public float flankUtlity;
    public float supressUtility;
    public float SquadClusteringScore;
}

// I really dont wanna make a new file for one enum
public enum TacticalRole
{
    Default, // Cover-Seeking
    Aggressor, // Direct attack on the threat
    Suppressor, // Anchor its position/line of sight
    Flanker // Wide routing around the threat
}