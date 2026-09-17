using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Data context shared with state instances
/// </summary>
public class AgentStateContext
{
    public Transform Transform;
    public NavMeshAgent NavAgent;
    public TacticalAgentMovement Movement;
    public InfluenceQuerySytem QuerySytem;
    public Transform ThreatTransform;
    public TacticalRole CurrentRole;
}

public interface IAgentState
{
    void Enter(AgentStateContext context);
    void Update(AgentStateContext context);
    void Exit(AgentStateContext context);
}

public enum AgentStateType
{
    Idle,
    SeekingCover,
    Supressing,
    Flanking,
    Retreating
}