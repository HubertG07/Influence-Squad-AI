#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[RequireComponent(typeof(TacticalAgentMovement))]
public class OverheadAgentGizmo : MonoBehaviour
{
    [Header("Gizmo Display Settings")]
    [SerializeField] private bool drawTargetLine = true;
    [SerializeField] private bool drawRoleHeader = true;
    [SerializeField] private float labelVerticalOffset = 2.2f;

    private TacticalAgentMovement movement;
    private AgentStateMachine fsm;

    void Awake()
    {
        movement = GetComponent<TacticalAgentMovement>();
        fsm = GetComponent<AgentStateMachine>();
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (movement == null) movement = GetComponent<TacticalAgentMovement>();
        if (fsm == null) fsm = GetComponent<AgentStateMachine>();

        Vector3 headPosition = transform.position + Vector3.up * labelVerticalOffset;

        if (drawRoleHeader)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor= GetRoleColor(movement.CurrentRole) }
            };

            string statusText = $"[{movement.CurrentRole}]\nState: {fsm?.CurrentStateType}";
            Handles.Label(headPosition, statusText, style);
        }

        if (drawTargetLine && movement.HasTargetDestination)
        {
            Gizmos.color = GetRoleColor(movement.CurrentRole);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, movement.TargetDestination + Vector3.up * 0.5f);
            Gizmos.DrawWireSphere(movement.TargetDestination + Vector3.up * 0.5f, 0.35f);
        }
    }

    private Color GetRoleColor(TacticalRole role)
    {
        switch (role)
        {
            case TacticalRole.Flanker: return Color.cyan;
            case TacticalRole.Suppressor: return Color.red;
            default: return Color.green;
        }
    }
}
#endif