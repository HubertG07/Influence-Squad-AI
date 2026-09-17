using Unity.Mathematics;
using UnityEngine;

[System.Serializable]
public struct GridSettings
{
    public int width;
    public int height;
    public float cellSize;
    public float3 worldOrigin;

    public GridSettings(int width, int height, float cellSize, float3 worldOrigin)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.worldOrigin = worldOrigin;
    }

    public readonly int TotalCells => width * height;

    /// <summary>
    /// Convert the 2D coordinates into a 1D index
    /// </summary>
    public readonly int GridToIndex(int x, int z)
    {
        return x + (z * width);
    }

    /// <summary>
    /// Convert the 1D index to 2D coordinates
    /// </summary> 
    public readonly int2 IndexToGrid(int index)
    {
        int z  = index / width;
        int x = index % width;
        return new int2(x, z);
    }

    /// <summary>
    /// Convert 2D coordinates to World Space
    /// </summary>
    public readonly float3 GridToWorld(int x, int z)
    {
        return new float3(
            worldOrigin.x + (x * cellSize) + (cellSize * 0.5f),
            worldOrigin.y,
            worldOrigin.z + (z * cellSize) + (cellSize * 0.5f)
        );
    }

    /// <summary>
    /// Convert the World Space position to a 1D index
    /// </summary>
    public readonly int WorldToIndex(float3 worldPosition)
    {
        int x = math.clamp((int)math.floor((worldPosition.x - worldOrigin.x) / cellSize), 0, width - 1);
        int z = math.clamp((int)math.floor((worldPosition.z - worldOrigin.z) / cellSize), 0, height - 1);
        return GridToIndex(x, z);
    }

    /// <summary>
    /// Convert a World Space Position into 2D Grid Coordinates
    /// </summary>
    public readonly Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int x = math.clamp((int)math.floor((worldPosition.x - worldOrigin.x) / cellSize), 0, width - 1);
        int z = math.clamp((int)math.floor((worldPosition.z - worldOrigin.z) / cellSize), 0, height - 1);
        return new Vector2Int(x, z);
    }

    /// <summary>
    /// Check if grid coordinates and within bounds
    /// </summary>
    public readonly bool IsValidGridPosition(int x, int z)
    {
        return x >= 0 && x < width && z >= 0 && z < height;
    }
}
