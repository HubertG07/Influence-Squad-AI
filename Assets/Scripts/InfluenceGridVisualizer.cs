using UnityEngine;

[RequireComponent(typeof(InfluenceGridManager))]
public class InfluenceGridVisualizer : MonoBehaviour
{
    private InfluenceGridManager gridManager;

    [Header("Visualization Settings")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private float gizmoHeightOffset = 0.1f;
    [SerializeField] private Gradient mapGradient;

    private void Awake()
    {
        gridManager = GetComponent<InfluenceGridManager>();
        SetupDefaultGradient();
    }

    private void SetupDefaultGradient()
    {
        if (mapGradient != null && mapGradient.colorKeys.Length > 0) return;

        // High threat/Low score is Red, to Safe/High score of Green
        mapGradient = new Gradient();
        var colors = new GradientColorKey[] { new(Color.red, 0f), new(Color.yellow, 0.5f), new(Color.green, 1f) };
        var alphas = new GradientAlphaKey[] { new(0.6f, 0f), new(0.6f, 1f) };
        mapGradient.SetKeys(colors, alphas);
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying || gridManager == null) return;

        var settings = gridManager.Settings;
        var combinedMap = gridManager.CombinedMap;

        if (!combinedMap.IsCreated) return;

        Vector3 cubeSize = new Vector3(settings.cellSize * 0.9f, 0.05f, settings.cellSize * 0.9f);
        
        for (int i = 0; i < settings.TotalCells; i++)
        {
            float score = combinedMap[i];

            // Convert the cell index back to world position
            var gridPos = settings.IndexToGrid(i);
            Vector3 worldPos = settings.GridToWorld(gridPos.x, gridPos.y);
            worldPos.y += gizmoHeightOffset;

            // Map the colour based on the cell score
            Gizmos.color = mapGradient.Evaluate(score);
            Gizmos.DrawCube(worldPos, cubeSize);
        }
    }
}
