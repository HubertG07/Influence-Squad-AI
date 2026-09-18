#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

public class SquadAIInspectorWindow : EditorWindow
{
    private SquadCoordinator activeCoordinator;
    private InfluenceGridManager activeGridManager;
    
    private Label squadStatusLabel;
    private ProgressBar threatLevelBar;
    private ProgressBar clusterDensityBar;
    private Label roleBreakdownLabel;
    private ScrollView agentListScrollView;

    [MenuItem("Window/AI/Squad AI Inspector")]

    void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    public static void ShowWindow()
    {
        SquadAIInspectorWindow wnd = GetWindow<SquadAIInspectorWindow>();
        wnd.titleContent = new GUIContent("Squad AI Inspector");
        wnd.minSize = new Vector2(400, 500);
    }

    private void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        root.style.paddingLeft = 10;
        root.style.paddingRight = 10;
        root.style.paddingTop = 10;
        root.style.paddingBottom = 10;

        Label title = new Label("Tactical AI Squad Telemetry")
        {
            style =
            {
                fontSize = 18,
                unityFontStyleAndWeight = FontStyle.Bold,
                marginBottom = 10
            }
        };
        root.Add(title);

        HelpBox headerBox = new HelpBox("Live monitoring panel for Squad Coordinator, Utility Curve and Agent FSM State", HelpBoxMessageType.Info);
        root.Add(headerBox);

        squadStatusLabel = new Label("Squad Status: Unassigned");
        squadStatusLabel.style.marginTop = 8;
        squadStatusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        root.Add(squadStatusLabel);

        threatLevelBar = new ProgressBar { title = "Threat Urgency Curve" };
        threatLevelBar.style.marginTop = 5;
        root.Add(threatLevelBar);

        clusterDensityBar = new ProgressBar { title = "Squad Clustering Score" };
        clusterDensityBar.style.marginTop = 5;
        root.Add(clusterDensityBar);

        roleBreakdownLabel = new Label("Role Distribution: Flankers: 0 | Suppressors: 0 | Cover: 0");
        roleBreakdownLabel.style.marginTop = 10;
        roleBreakdownLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        root.Add(roleBreakdownLabel);

        Label agentHeader = new Label("Active Squad Members:")
        {
            style = { marginTop = 15, unityFontStyleAndWeight = FontStyle.Bold }
        };
        root.Add(agentHeader);

        agentListScrollView = new ScrollView();
        agentListScrollView.style.flexGrow = 1;
        agentListScrollView.style.marginTop = 5;
        root.Add(agentListScrollView);
    }

    private void OnInspectorUpdate()
    {
        if (!Application.isPlaying) return;

        if (activeCoordinator == null)
            activeCoordinator = FindAnyObjectByType<SquadCoordinator>();

        if (activeGridManager == null)
            activeGridManager = FindAnyObjectByType<InfluenceGridManager>();

        RefreshTelemetryData();
    }

    private void RefreshTelemetryData()
    {
        if (activeCoordinator == null)
        {
            squadStatusLabel.text = "Squad Status: No SquadCoordinator Found";
            return;
        }

        squadStatusLabel.text = $"Squad Status: Active ({activeCoordinator.squadMembers.Count} Units)";

        SquadTaticalContext context = activeCoordinator.LastTacticalContext;
        threatLevelBar.value = context.averageDistanceToThreat > 0 ? Mathf.Clamp01(1f - (context.averageDistanceToThreat / 30f)) * 100f : 0f;
        threatLevelBar.title = $"Threat Urgency: {threatLevelBar.value:F1}%";

        clusterDensityBar.value = Mathf.Clamp01(context.SquadClusteringScore) * 100f;
        clusterDensityBar.title = $"Squad Clustering: {clusterDensityBar.value:F1}%";

        int flankers = 0, suppressors = 0, defaultCover = 0;
        foreach (var agent in activeCoordinator.squadMembers)
        {
            if (agent == null) continue;
            switch (agent.CurrentRole)
            {
                case TacticalRole.Flanker: flankers++; break;
                case TacticalRole.Suppressor: suppressors++; break;
                default: defaultCover++; break;
            }
        }

        roleBreakdownLabel.text = $"Roles: Flankers: {flankers} | Suppressors: {suppressors} | Default Cover: {defaultCover}";

        agentListScrollView.Clear();
        foreach (var agent in activeCoordinator.squadMembers)
        {
            if (agent == null) continue;

            AgentStateMachine fsm = agent.GetComponent<AgentStateMachine>();
            string stateName = fsm != null ? fsm.CurrentStateType.ToString() : "N/A";

            VisualElement card = new VisualElement();
            card.style.paddingLeft = 8;
            card.style.paddingRight = 8;
            card.style.paddingTop = 6;
            card.style.paddingBottom = 6;
            card.style.marginBottom = 4;
            card.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 0.8f);
            card.style.borderBottomLeftRadius = 4;
            card.style.borderBottomRightRadius = 4;

            Label nameLabel = new Label($"{agent.name} — Role: [{agent.CurrentRole}] — State: [{stateName}]");
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            Label posLabel = new Label($"Target Destination: {agent.TargetDestination}");
            posLabel.style.fontSize = 11;

            card.Add(nameLabel);
            card.Add(posLabel);
            agentListScrollView.Add(card);
        }
    }
}
#endif