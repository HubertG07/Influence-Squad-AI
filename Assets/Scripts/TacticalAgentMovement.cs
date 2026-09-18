using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TacticalAgentMovement : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InfluenceQuerySytem querySytem;

    [Header("Movement Settings")]
    [SerializeField] private float searchRadius = 10f;
    [SerializeField] private float repathInterval = 1.0f; // Frequency stagger
    [SerializeField] private float scoreThresholdDelta = 0.25f; // Min score increase to justify moving
    [SerializeField] private float arrivalDistance = 1.5f;
    [SerializeField] private float navMeshSampleDistance = 1.5f;

    [Header("Debug Visuals")]
    [SerializeField] private bool showDebugGizmos = true;

    private NavMeshAgent navAgent;
    private Vector3 currentTargetPosition;
    private float currentTargetScore = -1f;
    private float nextRepathTime;
    private bool isMovingToTarget = false;

    public bool HasTargetDestination => isMovingToTarget;
    public Vector3 TargetDestination => currentTargetPosition;

    private List<(Vector3 pos, float score)> lastEvaluatedCandidates;

    private TacticalRole currentRole = TacticalRole.Default;
    public TacticalRole CurrentRole => currentRole;

    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();

        // Stagger the timer randomly so agents dont query at the same time
        nextRepathTime = Time.time + Random.Range(0f, repathInterval);

        lastEvaluatedCandidates = new List<(Vector3 pos, float score)>();
    }

    void Update()
    {
        if (querySytem == null) return;

        if (isMovingToTarget && !navAgent.pathPending && navAgent.hasPath)
        {
            if (navAgent.remainingDistance <= arrivalDistance)
            {
                isMovingToTarget = false;
            }
        }

        if (Time.time >= nextRepathTime)
        {
            EvaluateAndMove();
            nextRepathTime = Time.time + repathInterval;
        }
    }

    private void EvaluateAndMove()
    {
        float currentSpotScore = querySytem.GetScoreAtPosition(transform.position);

        if (isMovingToTarget)
        {
            currentTargetScore = querySytem.GetScoreAtPosition(currentTargetPosition);
        }
        else
        {
            currentTargetScore = currentSpotScore;
        }

        bool isExposedInDanger = currentSpotScore < 0.35f;

        // Query for a new best position
        if (querySytem.TryFindBestPosition(
            transform.position,
            searchRadius,
            navMeshSampleDistance,
            out Vector3 candiatePosition,
            out float candidateScore,
            ref lastEvaluatedCandidates
        ))
        {
            float pathDistance = Vector3.Distance(transform.position, candiatePosition);
            float distancePenalty = pathDistance * 0.01f;
            float adjustedCandiadateScore = candidateScore - distancePenalty;

            // Emergency Override
            float requiredDelta = isExposedInDanger ? 0.05f : scoreThresholdDelta;

            if (adjustedCandiadateScore > (currentTargetScore + requiredDelta))
            {
                currentTargetPosition = candiatePosition;
                currentTargetScore = candidateScore;
                isMovingToTarget = true;

                navAgent.stoppingDistance = candidateScore > 0.8f ? 0.4f : arrivalDistance;

                navAgent.SetDestination(currentTargetPosition);
            }
        }
    }


    /// <summary>
    /// Called by SquadCoordinator to assign new tactical instructions
    /// </summary>
    public void SetTacticalRole(TacticalRole role)
    {
        currentRole = role;

        switch (currentRole)
        {
            case TacticalRole.Flanker:
                searchRadius = 15f; // Wider search radius
                scoreThresholdDelta = 0.1f; // respond faster
                break;
            case TacticalRole.Suppressor:
                searchRadius = 8f;
                scoreThresholdDelta = 0.25f; // Hold cover
                break;
            case TacticalRole.Default:
            default:
                searchRadius = 10f;
                scoreThresholdDelta = 0.2f;
                break;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;

        if (lastEvaluatedCandidates != null)
        {
            for (int i = 0; i < lastEvaluatedCandidates.Count; i++)
            {
                var (pos, score) = lastEvaluatedCandidates[i];
                Gizmos.color = Color.Lerp(Color.red, Color.green, score);
                Gizmos.DrawWireSphere(pos, 0.25f);
            }
        }

        if (isMovingToTarget && navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(currentTargetPosition, 0.4f);
            Gizmos.DrawLine(transform.position, currentTargetPosition);
        }
    }
}
