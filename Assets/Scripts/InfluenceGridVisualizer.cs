#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.Collections;

public enum HeatmapVisualizationMode
{
    Combined,
    Threat,
    Cover,
    AllyDensity
}

[RequireComponent(typeof(InfluenceGridManager))]
public class InfluenceGridVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] private bool showHeatmap = true;
    [SerializeField] private bool showCellScores = false;
    [SerializeField] private HeatmapVisualizationMode displayMode = HeatmapVisualizationMode.Combined;
    [SerializeField, Range(0.05f, 1.0f)] private float cellHeightOffset = 0.05f;

    [Header("Colour Gradients")]
    [SerializeField] private Gradient scoreGraident;

    private InfluenceGridManager gridManager;

    void OnEnable()
    {
        gridManager = GetComponent<InfluenceGridManager>();
        InitializeDefaultGradients();
    }

    private void InitializeDefaultGradients()
    {
        if (scoreGraident == null || scoreGraident.colorKeys.Length == 0)
        {
            scoreGraident = new Gradient();
            GradientColorKey[] colors = new GradientColorKey[3];
            colors[0] = new GradientColorKey(Color.red, 0.0f);     // High Threat / Bad Score
            colors[1] = new GradientColorKey(Color.yellow, 0.5f);  // Neutral
            colors[2] = new GradientColorKey(Color.green, 1.0f);   // Low Threat / Optimal
            GradientAlphaKey[] alphas = new GradientAlphaKey[2];
            alphas[0] = new GradientAlphaKey(0.45f, 0.0f);
            alphas[1] = new GradientAlphaKey(0.45f, 1.0f);
            scoreGraident.SetKeys(colors, alphas);
        }
    }

    void OnDrawGizmos()
    {
        if (!showHeatmap || gridManager == null || !Application.isPlaying) return;

        GridSettings settings = gridManager.Settings;
        NativeArray<float> activeMap = GetActiveMapData();
        if (activeMap == null || activeMap.Length == 0) return;

        Vector3 cellSize = new Vector3(settings.cellSize * 0.95f, 0.02f, settings.cellSize * 0.95f);

        for (int x = 0; x < settings.width; x++)
        {
            for (int z = 0; z < settings.height; z++)
            {
                int index = settings.GridToIndex(x, z);
                if (index < 0 || index >= activeMap.Length) continue;

                float rawScore = activeMap[index];

                float colorEvaluationScore = (displayMode == HeatmapVisualizationMode.Threat) 
                    ? 1.0f - Mathf.Clamp01(rawScore) 
                    : Mathf.Clamp01(rawScore);

                Vector3 worldPos = settings.GridToWorld(x, z);
                worldPos.y += cellHeightOffset;

                Gizmos.color = scoreGraident.Evaluate(colorEvaluationScore);
                Gizmos.DrawCube(worldPos, cellSize);

                if (showCellScores && rawScore > 0.01f)
                {
                    Handles.Label(worldPos, rawScore.ToString("F2"), EditorStyles.miniLabel);
                }
            }
        }

        DrawHoverCellInspector();
    }

    private NativeArray<float> GetActiveMapData()
    {
        switch (displayMode)
        {
            case HeatmapVisualizationMode.Threat: return gridManager.ThreatMap;
            case HeatmapVisualizationMode.Cover: return gridManager.CoverMap;
            case HeatmapVisualizationMode.AllyDensity: return gridManager.AllyDensityMap;
            case HeatmapVisualizationMode.Combined: default: return gridManager.CombinedMap;
        }
    }

    private void DrawHoverCellInspector()
    {
        GridSettings settings = gridManager.Settings;

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector2Int gridPos = settings.WorldToGrid(hit.point);
            if (settings.IsValidGridPosition(gridPos.x, gridPos.y))
            {
                int index = settings.GridToIndex(gridPos.x, gridPos.y);
                Vector3 cellWorld = settings.GridToWorld(gridPos.x, gridPos.y);

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(cellWorld + Vector3.up * 0.1f, new Vector3(settings.cellSize, 0.2f, settings.cellSize));

                string inspectText = $"Cell ({gridPos.x}, {gridPos.y})\n" +
                                     $"Score: {gridManager.CombinedMap[index]:F2}\n" +
                                     $"Threat: {gridManager.ThreatMap[index]:F2}\n" +
                                     $"Cover: {gridManager.CoverMap[index]:F2}";
                Handles.Label(cellWorld + Vector3.up * 0.5f, inspectText, EditorStyles.boldLabel);
            }
        }
    }
}
#endif