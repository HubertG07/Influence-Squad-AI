using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

public class InfluenceQuerySytem : MonoBehaviour
{
    [SerializeField] private InfluenceGridManager gridManager;

    private Vector2Int[] sampleOffsets;

    void Awake()
    {
        GenerateSampleOffsets(searchRadius: 5);
    }

    /// <summary>
    /// Pre compute a local ring of cell offsets to sample around any agent
    /// </summary>
    private void GenerateSampleOffsets(int searchRadius)
    {
        var offsetsList = new List<Vector2Int>();

        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int z = -searchRadius; z <= searchRadius; z++)
            {
                // Only points within search radius circle
                if (x * x + z * z <= searchRadius * searchRadius)
                {
                    offsetsList.Add(new Vector2Int(x, z));
                }
            }
        }

        sampleOffsets = offsetsList.ToArray();
    }

    /// <summary>
    /// Finds the best target position on the NavMesh around the agent based on the heatmap sources
    /// </summary>
    public bool TryFindBestPosition(
        Vector3 agentPosition,
        float searchRadius,
        float navMeshSampleDistance,
        out Vector3 bestWorldPosition,
        out float bestScore,
        ref List<(Vector3 pos, float score)> evaluatedCandidates)
    {
        bestWorldPosition = agentPosition;
        bestScore = -1f;
        evaluatedCandidates.Clear();

        if (gridManager == null) return false;

        GridSettings settings = gridManager.Settings;
        NativeArray<float> combinedMap = gridManager.CombinedMap;

        if (!combinedMap.IsCreated) return false;

        // Conver the Agent position to the grid center
        int centerIndex = settings.WorldToIndex(agentPosition);
        int2 centerGridPos = settings.IndexToGrid(centerIndex);

        NavMeshHit navHit;
        bool foundValidPosition = false;

        for (int i = 0; i < sampleOffsets.Length; i++)
        {
            Vector2Int offset = sampleOffsets[i];
            int sampleX = centerGridPos.x + offset.x;
            int sampleZ = centerGridPos.y + offset.y;

            if (sampleX < 0 || sampleX >= settings.width || sampleZ < 0 || sampleZ >= settings.height)
            {
                continue;
            }

            int sampleIndex = settings.GridToIndex(sampleX, sampleZ);
            float score = combinedMap[sampleIndex];

            // Ignore cells worse than the best
            if (score <= bestScore) continue;

            Vector3 candidateWorldPos = settings.GridToWorld(sampleX, sampleZ);

            // Make sure its valid against Unity's Navmesh
            if (NavMesh.SamplePosition(candidateWorldPos, out navHit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                evaluatedCandidates.Add((navHit.position, score));
                bestScore = score;
                bestWorldPosition = navHit.position;
                foundValidPosition = true;
            }
        }

        return foundValidPosition;
    }

    /// <summary>
    /// Read the score of a single point in world space
    /// </summary>
    public float GetScoreAtPosition(Vector3 worldPos)
    {
        if (gridManager == null) return 0f;
        NativeArray<float> combinedMap = gridManager.CombinedMap;
        if (!combinedMap.IsCreated) return 0f;

        int index = gridManager.Settings.WorldToIndex(worldPos);
        return combinedMap[index];
    }
}
