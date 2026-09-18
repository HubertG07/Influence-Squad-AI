using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


/// <summary>
/// Calculate the threat level based on the distance and facing direction from the source
/// </summary>
[BurstCompile(CompileSynchronously = true)]
public struct CalculateThreatJob : IJobParallelFor
{
    public GridSettings grid;
    public float3 threatPosition;
    public float3 threatForward;
    public float maxThreatDistance;
    public float maxThreatAngle;

    [WriteOnly] public NativeArray<float> threatMap;

    public void Execute(int index)
    {
        int2 gridPos = grid.IndexToGrid(index);
        float3 cellWorldPos = grid.GridToWorld(gridPos.x, gridPos.y);

        float3 dirToCell = cellWorldPos - threatPosition;
        float distance = math.length(dirToCell);

        if (distance > maxThreatDistance || distance < 0.0001f)
        {
            threatMap[index] = 0f;
            return;
        }

        float3 normDir = dirToCell / distance;

        float dot = math.dot(math.normalize(threatForward), normDir);
        float angle = math.degrees(math.acos(math.clamp(dot, -1f, 1f)));

        if (angle > maxThreatAngle)
        {
            threatMap[index] = 0f;
            return;
        }

        float distanceFactor = 1f - (distance / maxThreatDistance);
        float angleFactor = 1f - (angle / maxThreatAngle);

        threatMap[index] = distanceFactor * angleFactor;
    }
}

/// <summary>
/// Combine all the maps into a weighted score
/// </summary>
[BurstCompile(CompileSynchronously = true)]
public struct CombineMapsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> threatMap;
    [ReadOnly] public NativeArray<float> coverMap;
    [ReadOnly] public NativeArray<float> allyDensityMap;

    public float threatWeight;
    public float coverWeight;
    public float allyWeight;

    [WriteOnly] public NativeArray<float> combinedMap;

    public void Execute(int index)
    {
        float threat = math.clamp(threatMap[index] * threatWeight, 0f, 1f);
        float cover = math.clamp(coverMap[index] * coverWeight, 0f, 1f);
        float allyDensity = math.clamp(allyDensityMap[index] * allyWeight, 0f, 1f);

        float openGroundSafety = 0.50f;

        float directThreatSafety = openGroundSafety * math.pow((1f - threat), 2f);

        float finalSafety = math.select(directThreatSafety, 1.0f, cover > 0.5f);

        float score = finalSafety - (allyDensity * 0.2f);
        combinedMap[index] = math.clamp(score, 0f, 1f);
    }
}

[BurstCompile(CompileSynchronously = true)]
public struct CalculateAllyDensityJob : IJobParallelFor
{
    public GridSettings grid;
    [ReadOnly] public NativeArray<float3> allyPositions;
    public float maxClusterRadius;

    [WriteOnly] public NativeArray<float> allyDensityMap;

    public void Execute(int index)
    {
        if (allyPositions.Length == 0)
        {
            allyDensityMap[index] = 0f;
            return;
        }

        int2 gridPos = grid.IndexToGrid(index);
        float3 cellPos = grid.GridToWorld(gridPos.x, gridPos.y);

        float totalDensity = 0f;

        for (int i = 0; i < allyPositions.Length; i++)
        {
            float dist = math.distance(cellPos, allyPositions[i]);
            if (dist > 0.3f && dist < maxClusterRadius)
            {
                totalDensity += 1f - (dist / maxClusterRadius);
            }
        }

        allyDensityMap[index] = math.clamp(totalDensity, 0f, 1f);
    }
}

[BurstCompile(CompileSynchronously = true)]
public struct ProcessCoverRaycastsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<RaycastHit> raycaseResults;
    [WriteOnly] public NativeArray<float> coverMap;

    public void Execute(int index)
    {
        // if a ray hits an obstacle the cell is marked as blocked / non viable
        bool isBlocked = raycaseResults[index].colliderEntityId != default;
        coverMap[index] = isBlocked ? 0.0f : 1.0f;
    }
}

[BurstCompile(CompileSynchronously = true)]
public struct ProcessTacticalCoverJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<RaycastHit> coverRaycastResults;
    [WriteOnly] public NativeArray<float> coverMap;

    public void Execute(int index)
    {
        // If the raycast from the cell towards the threat hits an obstacle
        bool hitObstacle = coverRaycastResults[index].colliderEntityId != default;
        coverMap[index] = hitObstacle ? 1.0f : 0.0f;
    }
}

[BurstCompile(CompileSynchronously = true)]
public struct ProcessThreatOcclusionJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<RaycastHit> threatRaycastResults;
    public NativeArray<float> threatMap;

    public void Execute(int index)
    {
        if (threatRaycastResults[index].colliderEntityId != default)
        {
            threatMap[index] = 0f;
        }
    }
}