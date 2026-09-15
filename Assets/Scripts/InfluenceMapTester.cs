using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class InfluenceMapTester : MonoBehaviour
{
    [SerializeField] private InfluenceGridManager gridManager;
    [SerializeField] private Transform threatSource;

    [Header("Ally Testing Setup")]
    [SerializeField] private Transform[] allyTransforms;

    [Header("Threat Parameters")]
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private float maxAngle = 60f;

    private NativeArray<float3> allyPositionsBuffer;
    private JobHandle updateHandle;

    void Start()
    {
        InitializeAllyBuffer();
    }

    void OnValidate()
    {
        // Reallocate the buffer if the number of allies changes in the Inspector while playing
        if (Application.isPlaying)
        {
            InitializeAllyBuffer();
        }
    }

    private void InitializeAllyBuffer()
    {
        if (allyPositionsBuffer.IsCreated)
        {
            allyPositionsBuffer.Dispose();
        }

        int allyCount  = allyTransforms != null ? allyTransforms.Length : 0;

        allyPositionsBuffer = new NativeArray<float3>(Mathf.Max(1, allyCount), Allocator.Persistent);
    }

    private void Update()
    {
        if (gridManager == null || threatSource == null) return;

        int activeAllyCount = 0;
        if (allyTransforms != null)
        {
            for (int i = 0; i < allyTransforms.Length; i++)
            {
                if (allyTransforms[i] != null)
                {
                    allyPositionsBuffer[activeAllyCount] = allyTransforms[i].position;
                    activeAllyCount++;
                }
            }
        }

        var activeAllySlice = allyPositionsBuffer.GetSubArray(0, activeAllyCount);

        updateHandle = gridManager.UpdateInfluenceMap(
            threatSource.position,
            threatSource.forward,
            maxDistance,
            maxAngle,
            activeAllySlice
        );
    }

    void LateUpdate()
    {
        updateHandle.Complete();
    }

    public void Dispose()
    {
        if (allyPositionsBuffer.IsCreated)
        {
            allyPositionsBuffer.Dispose();
        }
    }

    void OnDestroy()
    {
        Dispose();
    }
}
