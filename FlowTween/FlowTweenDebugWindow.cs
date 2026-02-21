using UnityEngine;
using UnityEditor;
using FlT;
using System.Collections.Generic;

public class FlowTweenDebugWindow : EditorWindow
{
    // ─── Tabs ─────────────────────────────────────────────────────────────────
    private enum Tab { Tweens, FixedTweens, Sequences, Settings }
    private Tab currentTab = Tab.Tweens;

    // ─── Scroll ───────────────────────────────────────────────────────────────
    private Vector2 tweenScroll;
    private Vector2 fixedScroll;
    private Vector2 sequenceScroll;

    // ─── Filters ──────────────────────────────────────────────────────────────
    private string searchFilter  = "";
    private bool   showPaused    = true;
    private bool   showPlaying   = true;
    private bool   showCompleted = false;

    // ─── Styles ───────────────────────────────────────────────────────────────
    private GUIStyle headerStyle;
    private GUIStyle cardStyle;
    private GUIStyle labelBoldStyle;
    private GUIStyle tagStyle;
    private bool stylesInitialized;

    // ─── Colors ───────────────────────────────────────────────────────────────
    private static readonly Color ColorPlaying   = new(0.27f, 0.78f, 0.47f);
    private static readonly Color ColorPaused    = new(0.95f, 0.77f, 0.20f);
    private static readonly Color ColorCompleted = new(0.60f, 0.60f, 0.60f);
    private static readonly Color ColorBar       = new(0.27f, 0.60f, 0.95f);
    private static readonly Color ColorBarBg     = new(0.18f, 0.18f, 0.18f);
    private static readonly Color ColorCard      = new(0.22f, 0.22f, 0.22f);
    private static readonly Color ColorHeader    = new(0.15f, 0.15f, 0.15f);

    // ─── Settings ─────────────────────────────────────────────────────────────
    private float refreshRate = 0.05f;
    private double lastRepaintTime;

    [MenuItem("Window/Analysis/FlowTween Debugger")]
    public static void Open()
    {
        var window = GetWindow<FlowTweenDebugWindow>("FlowTween Debugger");
        window.minSize = new Vector2(420, 300);
    }

    private void OnEnable()
    {
        stylesInitialized = false;
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;
        stylesInitialized = true;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleLeft
        };

        cardStyle = new GUIStyle("box")
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin  = new RectOffset(4, 4, 3, 3)
        };

        labelBoldStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold
        };

        tagStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            padding   = new RectOffset(6, 6, 2, 2)
        };
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (EditorApplication.timeSinceStartup - lastRepaintTime >= refreshRate)
        {
            lastRepaintTime = EditorApplication.timeSinceStartup;
            Repaint();
        }
    }

    private void OnGUI()
    {
        InitStyles();

        if (!Application.isPlaying)
        {
            DrawNotPlaying();
            return;
        }

        DrawHeader();
        DrawTabs();
        DrawFilterBar();
        EditorGUILayout.Space(2);

        switch (currentTab)
        {
            case Tab.Tweens:      DrawTweenList(false); break;
            case Tab.FixedTweens: DrawTweenList(true);  break;
            case Tab.Sequences:   DrawSequenceList();   break;
            case Tab.Settings:    DrawSettings();       break;
        }
    }

    // ─── Not Playing ──────────────────────────────────────────────────────────
    private void DrawNotPlaying()
    {
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.HelpBox("Enter Play Mode to inspect active tweens.", MessageType.Info);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
    }

    // ─── Header ───────────────────────────────────────────────────────────────
    private void DrawHeader()
    {
        EditorGUI.DrawRect(new Rect(0, 0, position.width, 36), ColorHeader);

        EditorGUILayout.BeginHorizontal(GUILayout.Height(36));
        GUILayout.Space(10);
        GUILayout.Label("FlowTween Debugger", headerStyle, GUILayout.Height(36));
        GUILayout.FlexibleSpace();

        // Counts
        DrawCountBadge($"Tweens  {FlowTween.ActiveIdleCount}",    ColorPlaying);
        GUILayout.Space(4);
        DrawCountBadge($"Fixed  {FlowTween.ActiveFixedCount}",    ColorPaused);
        GUILayout.Space(4);
        DrawCountBadge($"Seq  {FlowTween.ActiveSequenceCount}",   ColorBar);
        GUILayout.Space(8);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCountBadge(string label, Color color)
    {
        var prev = GUI.color;
        GUI.color = color;
        GUILayout.Label(label, EditorStyles.miniButtonMid, GUILayout.Height(20));
        GUI.color = prev;
    }

    // ─── Tabs ─────────────────────────────────────────────────────────────────
    private void DrawTabs()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        DrawTabButton(Tab.Tweens,      "Update Tweens");
        DrawTabButton(Tab.FixedTweens, "Fixed Tweens");
        DrawTabButton(Tab.Sequences,   "Sequences");
        GUILayout.FlexibleSpace();
        DrawTabButton(Tab.Settings,    "Settings");
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTabButton(Tab tab, string label)
    {
        bool active = currentTab == tab;
        var style = active ? EditorStyles.toolbarButton : EditorStyles.toolbarButton;
        var prev = GUI.color;
        if (active) GUI.color = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button(label, style)) currentTab = tab;
        GUI.color = prev;
    }

    // ─── Filter Bar ───────────────────────────────────────────────────────────
    private void DrawFilterBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);
        GUILayout.Space(8);
        showPlaying   = GUILayout.Toggle(showPlaying,   "Playing",   EditorStyles.toolbarButton);
        showPaused    = GUILayout.Toggle(showPaused,    "Paused",    EditorStyles.toolbarButton);
        showCompleted = GUILayout.Toggle(showCompleted, "Completed", EditorStyles.toolbarButton);
        EditorGUILayout.EndHorizontal();
    }

    // ─── Tween List ───────────────────────────────────────────────────────────
    private readonly List<Tween> tweenSnapshot = new();

    private void DrawTweenList(bool isFixed)
    {
        tweenSnapshot.Clear();

        if (isFixed)
            FlowTween.ForEachActiveFixedTween(t => tweenSnapshot.Add(t));
        else
            FlowTween.ForEachActiveTween(t => tweenSnapshot.Add(t));

        var filtered = Filter(tweenSnapshot);

        ref Vector2 scroll = ref isFixed ? ref fixedScroll : ref tweenScroll;
        scroll = EditorGUILayout.BeginScrollView(scroll);

        if (filtered.Count == 0)
        {
            DrawEmptyState(isFixed ? "No active fixed tweens." : "No active tweens.");
        }
        else
        {
            foreach (var tween in filtered)
                DrawTweenCard(tween);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawTweenCard(Tween tween)
    {
        string targetName = tween.Target != null ? tween.Target.name : "No Target";
        if (!MatchesSearch(targetName, tween)) return;

        bool isPlaying   = tween.IsPlaying;
        bool isPaused    = tween.IsPaused;
        bool isCompleted = tween.IsCompleted;

        Color stateColor = isCompleted ? ColorCompleted : isPaused ? ColorPaused : ColorPlaying;
        string stateLabel = isCompleted ? "Completed" : isPaused ? "Paused" : "Playing";

        EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), new Color(0.3f, 0.3f, 0.3f));

        EditorGUILayout.BeginVertical(cardStyle);

        // Row 1 — Target + State tag
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(targetName, labelBoldStyle);
        GUILayout.FlexibleSpace();
        var prev = GUI.color;
        GUI.color = stateColor;
        GUILayout.Label(stateLabel, tagStyle, GUILayout.Width(70));
        GUI.color = prev;
        EditorGUILayout.EndHorizontal();

        // Row 2 — ID / Group
        EditorGUILayout.BeginHorizontal();
        string idStr    = tween.Id    != null              ? $"ID: {tween.Id}"       : "";
        string groupStr = !string.IsNullOrEmpty(tween.Group) ? $"Group: {tween.Group}" : "";
        GUILayout.Label($"{idStr}  {groupStr}".Trim(), EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        // Row 3 — Progress bar
        float progress = tween.Progress;
        DrawProgressBar(progress, $"{tween.Elapsed:0.00}s / {tween.Duration:0.00}s  ({progress * 100f:0}%)");

        // Row 4 — Controls
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Pause",    EditorStyles.miniButtonLeft,  GUILayout.Width(55))) tween.Pause();
        if (GUILayout.Button("Resume",   EditorStyles.miniButtonMid,   GUILayout.Width(60))) tween.Resume();
        if (GUILayout.Button("Complete", EditorStyles.miniButtonMid,   GUILayout.Width(70))) tween.Complete();
        if (GUILayout.Button("Kill",     EditorStyles.miniButtonRight, GUILayout.Width(45))) tween.Kill();
        GUILayout.FlexibleSpace();
        GUILayout.Label($"Scale: {tween.TimeScale:0.0x}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        GUILayout.Space(2);
    }

    // ─── Sequence List ────────────────────────────────────────────────────────
    private readonly List<Sequence> sequenceSnapshot = new();

    private void DrawSequenceList()
    {
        sequenceSnapshot.Clear();
        FlowTween.ForEachActiveSequence(s => sequenceSnapshot.Add(s));

        sequenceScroll = EditorGUILayout.BeginScrollView(sequenceScroll);

        if (sequenceSnapshot.Count == 0)
        {
            DrawEmptyState("No active sequences.");
        }
        else
        {
            foreach (var seq in sequenceSnapshot)
                DrawSequenceCard(seq);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSequenceCard(Sequence sequence)
    {
        bool isPaused    = sequence.IsPaused;
        bool isCompleted = sequence.IsCompleted;

        Color stateColor  = isCompleted ? ColorCompleted : isPaused ? ColorPaused : ColorPlaying;
        string stateLabel = isCompleted ? "Completed" : isPaused ? "Paused" : "Playing";

        float progress = sequence.TotalDuration > 0f
            ? Mathf.Clamp01(sequence.Elapsed / sequence.TotalDuration)
            : 0f;

        EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), new Color(0.3f, 0.3f, 0.3f));

        EditorGUILayout.BeginVertical(cardStyle);

        // Row 1 — Label + state
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Sequence", labelBoldStyle);
        GUILayout.FlexibleSpace();
        var prev = GUI.color;
        GUI.color = stateColor;
        GUILayout.Label(stateLabel, tagStyle, GUILayout.Width(70));
        GUI.color = prev;
        EditorGUILayout.EndHorizontal();

        // Row 2 — Duration info
        GUILayout.Label($"Total Duration: {sequence.TotalDuration:0.00}s", EditorStyles.miniLabel);

        // Row 3 — Progress bar
        DrawProgressBar(progress, $"{sequence.Elapsed:0.00}s / {sequence.TotalDuration:0.00}s  ({progress * 100f:0}%)");

        // Row 4 — Controls
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Pause",  EditorStyles.miniButtonLeft,  GUILayout.Width(55))) sequence.Pause();
        if (GUILayout.Button("Resume", EditorStyles.miniButtonMid,   GUILayout.Width(60))) sequence.Resume();
        if (GUILayout.Button("Kill",   EditorStyles.miniButtonRight, GUILayout.Width(45))) sequence.Kill();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        GUILayout.Space(2);
    }

    // ─── Settings ─────────────────────────────────────────────────────────────
    private void DrawSettings()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Window Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        refreshRate = EditorGUILayout.Slider("Refresh Rate (s)", refreshRate, 0.016f, 0.5f);

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Global Defaults", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        var newTransition = (Tween.TransitionType)EditorGUILayout.EnumPopup("Default Transition", FlowTween.DefaultTransition);
        if (newTransition != FlowTween.DefaultTransition)
            FlowTween.SetDefaultTransition(newTransition);

        var newEase = (Tween.EaseType)EditorGUILayout.EnumPopup("Default Ease", FlowTween.DefaultEase);
        if (newEase != FlowTween.DefaultEase)
            FlowTween.SetDefaultEase(newEase);

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Global Controls", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Pause All"))    FlowTween.PauseAll();
        if (GUILayout.Button("Resume All"))   FlowTween.ResumeAll();
        if (GUILayout.Button("Complete All")) FlowTween.CompleteAll();
        if (GUILayout.Button("Kill All"))
        {
            if (EditorUtility.DisplayDialog("Kill All Tweens", "Are you sure you want to kill all active tweens?", "Yes", "Cancel"))
                FlowTween.KillAll();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Pause Sequences"))  FlowTween.PauseSequences();
        if (GUILayout.Button("Resume Sequences")) FlowTween.ResumeSequences();
        if (GUILayout.Button("Kill Sequences"))
        {
            if (EditorUtility.DisplayDialog("Kill All Sequences", "Are you sure you want to kill all active sequences?", "Yes", "Cancel"))
                FlowTween.KillSequences();
        }
        EditorGUILayout.EndHorizontal();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    private void DrawProgressBar(float progress, string label)
    {
        Rect rect = GUILayoutUtility.GetRect(18, 16, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, ColorBarBg);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * progress, rect.height), ColorBar);
        EditorGUI.LabelField(rect, label, EditorStyles.centeredGreyMiniLabel);
    }

    private void DrawEmptyState(string message)
    {
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(message, EditorStyles.centeredGreyMiniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
    }

    private List<Tween> Filter(List<Tween> tweens)
    {
        var result = new List<Tween>();
        foreach (var t in tweens)
        {
            if (t.IsCompleted && !showCompleted) continue;
            if (t.IsPaused    && !showPaused)    continue;
            if (t.IsPlaying   && !showPlaying)   continue;
            result.Add(t);
        }
        return result;
    }

    private bool MatchesSearch(string targetName, Tween tween)
    {
        if (string.IsNullOrEmpty(searchFilter)) return true;
        string f = searchFilter.ToLower();
        if (targetName.ToLower().Contains(f)) return true;
        if (tween.Id    != null                   && tween.Id.ToString().ToLower().Contains(f))    return true;
        if (!string.IsNullOrEmpty(tween.Group)    && tween.Group.ToLower().Contains(f))            return true;
        return false;
    }
}