using UnityEngine;

public class IdleState : IAgentState
{
    public void Enter(AgentStateContext context) { }
    public void Update(AgentStateContext context)
    {
        if (context.ThreatTransform == null) return;

        switch (context.CurrentRole)
        {
            case TacticalRole.Flanker:
                context.Transform.GetComponent<AgentStateMachine>().TransitionTo(AgentStateType.Flanking);
                break;
            case TacticalRole.Suppressor:
                context.Transform.GetComponent<AgentStateMachine>().TransitionTo(AgentStateType.Supressing);
                break;
            default:
                context.Transform.GetComponent<AgentStateMachine>().TransitionTo(AgentStateType.SeekingCover);
                break;
        }
    }

    public void Exit(AgentStateContext context) { }
}

public class SeekingCoverState : IAgentState
{
    public void Enter(AgentStateContext context) { }

    public void Update(AgentStateContext context)
    {
        if (context.CurrentRole == TacticalRole.Flanker)
        {
            context.Transform.GetComponent<AgentStateMachine>().TransitionTo(AgentStateType.Flanking);
            return;
        }
        if (context.CurrentRole == TacticalRole.Suppressor)
        {
            context.Transform.GetComponent<AgentStateMachine>().TransitionTo(AgentStateType.Supressing);
            return;
        }
        
        if (context.ThreatTransform != null)
        {
            Vector3 lookDir = context.ThreatTransform.position - context.Transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                context.Transform.rotation = Quaternion.Slerp(context.Transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
            }
        }
    }

    public void Exit(AgentStateContext context) { }
}

public class SuppressingState : IAgentState
{
    public void Enter(AgentStateContext context) { }

    public void Update(AgentStateContext context)
    {
        if (context.CurrentRole != TacticalRole.Suppressor)
        {
            context.Transform.GetComponent<AgentStateMachine>().TransitionTo(AgentStateType.SeekingCover);
            return;
        }

        if (context.ThreatTransform != null)
        {
            Vector3 lookDir = context.ThreatTransform.position - context.Transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                context.Transform.rotation = Quaternion.Slerp(context.Transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
            }
        }
    }

    public void Exit(AgentStateContext context) { }
}

public class FlankingState : IAgentState
{
    public void Enter(AgentStateContext context) { }

    public void Update(AgentStateContext context)
    {
        if (context.CurrentRole != TacticalRole.Flanker)
        {
            context.Transform.GetComponent<AgentStateMachine>().TransitionTo(AgentStateType.SeekingCover);
            return;
        }

        if (context.NavAgent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 moveDir = context.NavAgent.velocity.normalized;
            moveDir.y = 0;
            context.Transform.rotation = Quaternion.Slerp(context.Transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 8f);
        }
    }

    public void Exit(AgentStateContext context) { }
}

public class RetreatingState : IAgentState
{
    public void Enter(AgentStateContext context) { }

    public void Update(AgentStateContext context)
    {
        if (context.ThreatTransform != null)
        {
            Vector3 awayDir = context.Transform.position - context.ThreatTransform.position;
            awayDir.y = 0;
            if (awayDir.sqrMagnitude > 0.01f)
            {
                context.Transform.rotation = Quaternion.Slerp(context.Transform.rotation, Quaternion.LookRotation(awayDir), Time.deltaTime * 6f);
            }
        }
    }

    public void Exit(AgentStateContext context) { }
}