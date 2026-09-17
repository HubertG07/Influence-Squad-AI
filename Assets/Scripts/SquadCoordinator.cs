using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class SquadCoordinator : MonoBehaviour
{
    [Header("Dependenices")]
    [SerializeField] private InfluenceGridManager gridManager;
    [SerializeField] private Transform threatTransform;

    [Header("Squad Settings")]
    [SerializeField] private List<TacticalAgentMovement> squadMembers = new List<TacticalAgentMovement>();
    [SerializeField] private float assessmentInterval = 0.5f;
    [SerializeField] private float roleChangeCooldown = 3.0f; // Timer to prevent role flickering

    private float nextAssessmentTime;
    private SquadTaticalContext currentContext;
    private Dictionary<TacticalAgentMovement, float> roleChangeTimers = new Dictionary<TacticalAgentMovement, float>();

    private NativeArray<float3> memberPositionsBuffer;

    void Awake()
    {
        InitializeBuffers();
    }

    private void InitializeBuffers()
    {
        if (squadMembers.Count > 0)
        {
            memberPositionsBuffer = new NativeArray<float3>(squadMembers.Count, Allocator.Persistent);
        }
    }

    void Update()
    {
        if (Time.time >= nextAssessmentTime)
        {
            EvaluateSquadTactics();
            UpdateGridAllyPositions();
            nextAssessmentTime = Time.time + assessmentInterval;
        }
    }

    /// <summary>
    /// Evaluate the squad-wide context and assign roles
    /// </summary>
    private void EvaluateSquadTactics()
    {
        if (squadMembers.Count == 0 | threatTransform == null) return;

        float totalDist = 0f;
        Vector3 squadCenter = Vector3.zero;

        for (int i = 0; i < squadMembers.Count; i++)
        {
            Vector3 pos = squadMembers[i].transform.position;
            squadCenter += pos;
            totalDist += Vector3.Distance(pos, threatTransform.position);
        }

        squadCenter /= squadMembers.Count;
        currentContext.totalActiveMembers = squadMembers.Count;
        currentContext.averageDistanceToThreat = totalDist / squadMembers.Count;

        // Proximity Urgency
        float proximityScore = 1.0f - UtilityEngine.Logistic(currentContext.averageDistanceToThreat, midpoint: 15f, steepness: 0.3f);

        currentContext.flankUtlity = UtilityEngine.Exponential(proximityScore, min: 0.2f, max: 1.0f, exponent: 1.5f);
        currentContext.supressUtility = UtilityEngine.Linear(proximityScore, min: 0.1f, max: 0.9f);

        AllocateRoles();
    }

    /// <summary>
    /// Distribute the roles for a balanced composition
    /// </summary>
    private void AllocateRoles()
    {
        bool hasFlanker = false;

        for (int i = 0; i < squadMembers.Count; i++)
        {
            TacticalAgentMovement agent = squadMembers[i];

            // Hystersis check
            if (roleChangeTimers.TryGetValue(agent, out float lastChangeTime))
            {
                if (Time.time - lastChangeTime < roleChangeCooldown)
                {
                    if (agent.CurrentRole == TacticalRole.Flanker) hasFlanker = true;
                    continue;
                }
            }

            if (currentContext.flankUtlity > 0.6f && !hasFlanker)
            {
                AssignRole(agent, TacticalRole.Flanker);
                hasFlanker = true;
            }
            else if (currentContext.supressUtility > 0.4f)
            {
                AssignRole(agent, TacticalRole.Suppressor);
            }
            else
            {
                AssignRole(agent, TacticalRole.Default);
            }
        }
    }

    private void AssignRole(TacticalAgentMovement agent, TacticalRole newRole)
    {
        if (agent.CurrentRole != newRole)
        {
            agent.SetTacticalRole(newRole);
            roleChangeTimers[agent] = Time.time;
        }
    }

    /// <summary>
    /// Sync the active squad positions into native arrays for ally density calls
    /// </summary>
    private void UpdateGridAllyPositions()
    {
        if (!memberPositionsBuffer.IsCreated || memberPositionsBuffer.Length != squadMembers.Count)
        {
            if (memberPositionsBuffer.IsCreated) memberPositionsBuffer.Dispose();
            memberPositionsBuffer = new NativeArray<float3>(squadMembers.Count, Allocator.Persistent);
        }

        for (int i = 0; i < squadMembers.Count; i++)
        {
            memberPositionsBuffer[i] = squadMembers[i].transform.position;
        }

        if (gridManager != null && threatTransform != null)
        {
            JobHandle handle = gridManager.UpdateInfluenceMap(
                threatTransform.position,
                threatTransform.forward,
                maxDistance: 25f,
                maxAngle: 90f,
                memberPositionsBuffer
            );
            handle.Complete();
        }
    }

    public void RegisterAgent(TacticalAgentMovement agent)
    {
        if (!squadMembers.Contains(agent))
        {
            squadMembers.Add(agent);
        }
    }

    public void UnRegisterAgent(TacticalAgentMovement agent)
    {
        if (squadMembers.Contains(agent))
        {
            squadMembers.Remove(agent);
        }
    }

    void OnDestroy()
    {
        if (memberPositionsBuffer.IsCreated)
        {
            memberPositionsBuffer.Dispose();
        }
    }
}

