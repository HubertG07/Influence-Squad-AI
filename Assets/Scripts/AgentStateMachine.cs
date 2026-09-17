using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(TacticalAgentMovement), typeof(NavMeshAgent))]
public class AgentStateMachine : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private InfluenceQuerySytem querySystem;
    [SerializeField] private Transform threatTransform;

    private TacticalAgentMovement movement;
    private NavMeshAgent navAgent;
    private AgentStateContext context;

    private Dictionary<AgentStateType, IAgentState> stateRegistry;
    private IAgentState currentState;
    private AgentStateType currentStateType = AgentStateType.Idle;

    public AgentStateType CurrentStateType => currentStateType;

    void Awake()
    {
        movement = GetComponent<TacticalAgentMovement>();
        navAgent = GetComponent<NavMeshAgent>();

        context = new AgentStateContext
        {
            Transform = transform,
            NavAgent = navAgent,
            Movement = movement,
            QuerySytem = querySystem,
            ThreatTransform = threatTransform
        };

        stateRegistry = new Dictionary<AgentStateType, IAgentState>
        {
            { AgentStateType.Idle, new IdleState() },
            { AgentStateType.SeekingCover, new SeekingCoverState() },
            { AgentStateType.Supressing, new SuppressingState() },
            { AgentStateType.Flanking, new FlankingState() },
            { AgentStateType.Retreating, new RetreatingState() }
        };

        TransitionTo(AgentStateType.Idle);
    }

    void Update()
    {
        context.CurrentRole = movement.CurrentRole;
        context.ThreatTransform = threatTransform;

        currentState?.Update(context);
    }

    /// <summary>
    /// Swap the state without creating a new memory instance
    /// </summary>
    public void TransitionTo(AgentStateType newStateType)
    {
        if (stateRegistry.TryGetValue(newStateType, out IAgentState newState))
        {
            currentState?.Exit(context);
            currentState = newState;
            currentStateType = newStateType;
            currentState.Enter(context);
        }
    }

    public void SetThreatTransform(Transform threat)
    {
        threatTransform = threat;
    }
}
