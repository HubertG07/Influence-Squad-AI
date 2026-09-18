#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class SquadAIInspectorWindow : EditorWindow
{
    private SquadCoordinator activeCoordinator;
    private InfluenceGridManager activeGridManager;
    
    private Label squadStatusLabel;
    private ProgressBar threatLevelBar;
    private ProgressBar clusterDensityBar;
    private Label roleBreakdownLabel;
    private ScrollView agentListScrollView;

    private List<AgentCardUI> cachedAgentCards = new List<AgentCardUI>();

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

        int activeCount = activeCoordinator.squadMembers.Count;

        while (cachedAgentCards.Count < activeCount)
        {
            AgentCardUI newCard = new AgentCardUI();
            cachedAgentCards.Add(newCard);
            agentListScrollView.Add(newCard.RootElement);
        }

        for (int i = 0; i < cachedAgentCards.Count; i++)
        {
            if (i < activeCount)
            {
                var agent = activeCoordinator.squadMembers[i];
                if (agent == null) continue;

                AgentStateMachine fsm = agent.GetComponent<AgentStateMachine>();
                string stateName = fsm != null ? fsm.CurrentStateType.ToString() : "N/A";

                cachedAgentCards[i].NameLabel.text = $"{agent.name} — Role: [{agent.CurrentRole}] — State: [{stateName}]";
                cachedAgentCards[i].PosLabel.text = $"Target Destination: {agent.TargetDestination}";
                cachedAgentCards[i].RootElement.style.display = DisplayStyle.Flex;
            }
            else
            {
                cachedAgentCards[i].RootElement.style.display = DisplayStyle.None;
            }
        }
    }

    private class AgentCardUI
    {
        public VisualElement RootElement;
        public Label NameLabel;
        public Label PosLabel;

        public AgentCardUI()
        {
            RootElement = new VisualElement();
            RootElement.style.paddingLeft = 8;
            RootElement.style.paddingRight = 8;
            RootElement.style.paddingTop = 6;
            RootElement.style.paddingBottom = 6;
            RootElement.style.marginBottom = 4;
            RootElement.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 0.8f);
            RootElement.style.borderBottomLeftRadius = 4;
            RootElement.style.borderBottomRightRadius = 4;

            NameLabel = new Label();
            NameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            PosLabel = new Label();
            PosLabel.style.fontSize = 11;

            RootElement.Add(NameLabel);
            RootElement.Add(PosLabel);
        }
    }
}
#endif