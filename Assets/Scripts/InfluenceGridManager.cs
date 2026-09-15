using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class InfluenceGridManager : MonoBehaviour, IDisposable
{
    [Header("Grid Setup")]
    [SerializeField] private int width = 50;
    [SerializeField] private int height = 50;
    [SerializeField] private float cellSize = 1f;

    [Header("Obstacle Setup")]
    [SerializeField] private LayerMask obstactleLayerMask;
    [SerializeField] private float raycastHeight = 10f;

    private GridSettings gridSettings;

    // Native memory buffers
    private NativeArray<float> threatMap;
    private NativeArray<float> coverMap;
    private NativeArray<float> allyDensityMap;
    private NativeArray<float> combinedMap;

    private NativeArray<RaycastCommand> raycastCommands;
    private NativeArray<RaycastHit> raycastResults;
    private NativeArray<RaycastCommand> threatRayCommands;
    private NativeArray<RaycastHit> threatRayResults;

    public GridSettings Settings => gridSettings;
    public NativeArray<float> CombinedMap => combinedMap;
    

    void Awake()
    {
        gridSettings = new GridSettings(width, height, cellSize, transform.position);
        InitializeBuffers();
    }

    private void InitializeBuffers()
    {
        int totalCells = gridSettings.TotalCells;

        threatMap = new NativeArray<float>(totalCells, Allocator.Persistent);
        coverMap = new NativeArray<float>(totalCells, Allocator.Persistent);
        allyDensityMap = new NativeArray<float>(totalCells, Allocator.Persistent);
        combinedMap = new NativeArray<float>(totalCells, Allocator.Persistent);

        raycastCommands = new NativeArray<RaycastCommand>(totalCells, Allocator.Persistent);
        raycastResults = new NativeArray<RaycastHit>(totalCells, Allocator.Persistent);

        threatRayCommands = new NativeArray<RaycastCommand>(totalCells, Allocator.Persistent);
        threatRayResults = new NativeArray<RaycastHit>(totalCells, Allocator.Persistent);

        BakeCoverMap();
    }

    /// <summary>
    ///  Batch raycasts across the grid to populate the coverMap
    /// </summary>
    public void BakeCoverMap()
    {
        int totalCells = gridSettings.TotalCells;
        QueryParameters queryParams = new QueryParameters(obstactleLayerMask);

        for (int i = 0; i < totalCells; i++)
        {
            int2 gridPos = gridSettings.IndexToGrid(i);
            float3 cellPos = gridSettings.GridToWorld(gridPos.x, gridPos.y);

            Vector3 rayStart = new Vector3(cellPos.x, cellPos.y + raycastHeight, cellPos.z);
            Vector3 rayDir = Vector3.down;

            raycastCommands[i] = new RaycastCommand(
                rayStart,
                rayDir,
                queryParams,
                raycastHeight * 1.5f
            );
        }

        JobHandle raycastHandle = RaycastCommand.ScheduleBatch(
            raycastCommands,
            raycastResults,
            minCommandsPerJob: 64
        );

        var processCoverJob = new ProcessCoverRaycastsJob
        {
            raycaseResults = raycastResults,
            coverMap = coverMap
        };

        JobHandle bakeHandle = processCoverJob.Schedule(totalCells, 64, raycastHandle);
        bakeHandle.Complete();
    }

    // Called inside the manager update loop
    public JobHandle UpdateInfluenceMap(Vector3 threatPos, Vector3 threatForward, float maxDistance, float maxAngle, NativeArray<float3> currentAllyPositions)
    {
        int totalCells = gridSettings.TotalCells;
        QueryParameters queryParams = new QueryParameters(obstactleLayerMask);

        // Calculate the threat job
        var threatJob = new CalculateThreatJob
        {
            grid = gridSettings,
            threatPosition = threatPos,
            threatForward = threatForward,
            maxThreatDistance = maxDistance,
            maxThreatAngle = maxAngle,
            threatMap = threatMap
        };
        JobHandle threatHandle = threatJob.Schedule(gridSettings.TotalCells, 64);

        for (int i = 0; i < totalCells; i++)
        {
            int2 gridPos = gridSettings.IndexToGrid(i);
            float3 cellPos = gridSettings.GridToWorld(gridPos.x, gridPos.y);

            float3 dir = cellPos - (float3)threatPos;
            float dist = math.length(dir);

            if (dist > 0.001f)
            {
                threatRayCommands[i] = new RaycastCommand(threatPos, math.normalize(dir), queryParams, dist);
            }
        }
        JobHandle threatRayHandle = RaycastCommand.ScheduleBatch(threatRayCommands, threatRayResults, 64, threatHandle);

        var occlusionJob = new ProcessThreatOcclusionJob
        {
            threatRaycastResults = threatRayResults,
            threatMap = threatMap
        };
        JobHandle occlusionHandle = occlusionJob.Schedule(totalCells, 64, threatRayHandle);

        var allyJob = new CalculateAllyDensityJob
        {
            grid = gridSettings,
            allyPositions = currentAllyPositions,
            maxClusterRadius = 5.0f,
            allyDensityMap = allyDensityMap
        };
        JobHandle allyHandle = allyJob.Schedule(gridSettings.TotalCells, 64);


        // Combind the jobs depending on occlusion and ally jobs finishing
        JobHandle combinedDependency = JobHandle.CombineDependencies(occlusionHandle, allyHandle);

        // Map the combination job
        var combineJob = new CombineMapsJob
        {
            threatMap = threatMap,
            coverMap = coverMap,
            allyDensityMap = allyDensityMap,
            threatWeight = 1.0f,
            coverWeight = 1.0f,
            allyWeight = 0.8f,
            combinedMap = combinedMap
        };

        return combineJob.Schedule(gridSettings.TotalCells, 64, combinedDependency);
    }

    // Prevent a memory leak
    public void Dispose()
    {
        if (threatMap.IsCreated) threatMap.Dispose();
        if (coverMap.IsCreated) coverMap.Dispose();
        if (allyDensityMap.IsCreated) allyDensityMap.Dispose();
        if (combinedMap.IsCreated) combinedMap.Dispose();

        if (raycastCommands.IsCreated) raycastCommands.Dispose();
        if(raycastResults.IsCreated) raycastResults.Dispose();

        if (threatRayCommands.IsCreated) threatRayCommands.Dispose();
        if (threatRayResults.IsCreated) threatRayResults.Dispose();
    }

    void OnDestroy()
    {
        Dispose();
    }
}
