using UnityEngine;
using UnityEditor;
using FlT;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// FlowTween Settings Window
/// Open: Window ▶ FlowTween ▶ Settings  (Alt+Shift+S)
///
/// Tabs:  Runtime · Pool · Lifecycle · Debugger · About
///
/// Persistence:
///   Runtime settings  → FlowTweenSettings ScriptableObject (Assets/Resources/)
///   Editor-only prefs → EditorPrefs  (machine-local, not committed to VCS)
///
/// Live Apply: Save immediately calls FlowTween.ApplySettings() during Play Mode
/// so changes take effect on the very next frame — no re-entry required.
///
/// Creator: Ahmed GD
/// </summary>
public class FlowTweenSettingsWindow : EditorWindow
{
    // ═══════════════════════════════════════════════════════════════════════
    //  Window Entry Point
    // ═══════════════════════════════════════════════════════════════════════

    [MenuItem("Window/FlowTween/Settings  %#&s", false, 11)]
    public static void Open()
    {
        var w = GetWindow<FlowTweenSettingsWindow>(false, "FT  Settings");
        w.minSize = new Vector2(450f, 580f);
        w.Show();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Constants
    // ═══════════════════════════════════════════════════════════════════════

    private const string Version      = "1.0.0";
    private const string ResourcePath = "FlowTweenSettings";
    private const string AssetPath    = "Assets/Resources/FlowTweenSettings.asset";
    private const string PrefsPrefix  = "FlT.Settings.";
    private const int    CurveSamples = 90;
    private const int    PoolHistLen  = 150;
    private const float  PoolSampleRate = 0.2f;

    // EditorPrefs keys
    private const string PrefRefreshRate   = PrefsPrefix + "refreshRate";
    private const string PrefDensity       = PrefsPrefix + "density";
    private const string PrefCurvePreviews = PrefsPrefix + "curvePreviews";
    private const string PrefSparklines    = PrefsPrefix + "sparklines";
    private const string PrefTsSliders     = PrefsPrefix + "tsSliders";
    private const string PrefConfirmKill   = PrefsPrefix + "confirmKill";
    private const string PrefGraveyard     = PrefsPrefix + "graveyard";
    private const string PrefEventLog      = PrefsPrefix + "eventLog";
    private const string PrefHlNear        = PrefsPrefix + "hlNear";
    private const string PrefSlowThresh    = PrefsPrefix + "slowThresh";
    private const string PrefMaxCards      = PrefsPrefix + "maxCards";

    // ═══════════════════════════════════════════════════════════════════════
    //  Enums & Presets
    // ═══════════════════════════════════════════════════════════════════════

    private enum Tab { Runtime, Pool, Lifecycle, Debugger, About }

    private struct Preset
    {
        public string                 label, tooltip, icon;
        public Tween.TransitionType   trans;
        public Tween.EaseType         ease;
        public int                    prewarmT, prewarmS, minPool;
        public float                  shrinkI, shrinkP, gts;
    }

    private static readonly Preset[] Presets =
    {
        new Preset {
            label    = "Lightweight",
            tooltip  = "Mobile-first: smallest pool, fast shrink, simple linear easing.",
            icon     = "📱",
            trans    = Tween.TransitionType.Linear,  ease = Tween.EaseType.Out,
            prewarmT = 8,  prewarmS = 2,  minPool = 4,  shrinkI = 4f,  shrinkP = 0.50f, gts = 1f },

        new Preset {
            label    = "Balanced",
            tooltip  = "Desktop default: moderate pool, smooth quad easing.",
            icon     = "⚖",
            trans    = Tween.TransitionType.Quad,    ease = Tween.EaseType.InOut,
            prewarmT = 32, prewarmS = 8,  minPool = 10, shrinkI = 10f, shrinkP = 0.25f, gts = 1f },

        new Preset {
            label    = "High Fidelity",
            tooltip  = "Console / PC: large pool, elastic bounce, conservative shrink.",
            icon     = "✨",
            trans    = Tween.TransitionType.Elastic, ease = Tween.EaseType.Out,
            prewarmT = 64, prewarmS = 16, minPool = 16, shrinkI = 20f, shrinkP = 0.10f, gts = 1f },
    };

    // ═══════════════════════════════════════════════════════════════════════
    //  Settings State — Runtime
    // ═══════════════════════════════════════════════════════════════════════

    private Tween.TransitionType defTransition   = Tween.TransitionType.Linear;
    private Tween.EaseType       defEase         = Tween.EaseType.In;
    private float                globalTimeScale  = 1f;
    private bool                 killOnUnload     = true;
    private bool                 autoKillOrphans  = true;
    private int                  prewarmTweens    = 32;
    private int                  prewarmSeqs      = 8;
    private float                shrinkInterval   = 10f;
    private float                shrinkPercent    = 0.25f;
    private int                  minPoolSize      = 10;

    // Saved-to-disk snapshot — used for the diff view
    private Tween.TransitionType savedTransition  = Tween.TransitionType.Linear;
    private Tween.EaseType       savedEase        = Tween.EaseType.In;
    private float                savedGts         = 1f;
    private bool                 savedKillUnload  = true;
    private bool                 savedAutoKill    = true;
    private int                  savedPrewarmT    = 32;
    private int                  savedPrewarmS    = 8;
    private float                savedShrinkI     = 10f;
    private float                savedShrinkP     = 0.25f;
    private int                  savedMinPool     = 10;

    // ═══════════════════════════════════════════════════════════════════════
    //  Settings State — Editor Prefs
    // ═══════════════════════════════════════════════════════════════════════

    private float refreshRate       = 0.05f;
    private int   density           = 1;
    private bool  showCurvePreviews = true;
    private bool  showSparklines    = true;
    private bool  showTsSliders     = true;
    private bool  confirmKill       = true;
    private bool  enableGraveyard   = true;
    private bool  enableEventLog    = true;
    private bool  hlNearComplete    = true;
    private int   slowModeThreshold = 300;
    private int   maxVisibleCards   = 200;

    // ═══════════════════════════════════════════════════════════════════════
    //  UI State
    // ═══════════════════════════════════════════════════════════════════════

    private Tab     activeTab        = Tab.Runtime;
    private Vector2 scroll;
    private bool    dirty;
    private bool    appliedThisSession;
    private bool    showDiff;
    private int     activePresetIdx  = -1;   // -1 = custom / none

    // Curve animation
    private float   animT;
    private bool    animPlay         = true;
    private double  lastTime;

    // Curve cache
    private float[] curvePoints;
    private string  curveKey         = "";

    // Pool history (ring buffers, play-mode only)
    private readonly float[] tweenPoolHist = new float[PoolHistLen];
    private readonly float[] seqPoolHist   = new float[PoolHistLen];
    private readonly float[] hitRateHist   = new float[PoolHistLen];
    private readonly float[] allocHist     = new float[PoolHistLen]; // miss count delta
    private int    poolHistHead;
    private int    lastMissCount;
    private float  poolSampleAccum;
    private float  peakPoolSize;
    private float  peakHitRate;

    // Persistent texture cache — keyed by RGBA to avoid per-frame allocation
    private readonly Dictionary<Color, Texture2D> texCache = new();

    // Settings asset
    private FlowTweenSettings asset;

    // Styles (lazy, invalidated on domain reload via stylesReady flag)
    private GUIStyle sHeader, sSubHeader, sTab, sTabActive;
    private GUIStyle sLabelBold, sPresetBtn, sPresetBtnActive;
    private GUIStyle sCode, sVersionLabel, sDiffOld, sDiffNew;
    private bool     stylesReady;

    // ═══════════════════════════════════════════════════════════════════════
    //  Colour Palette
    // ═══════════════════════════════════════════════════════════════════════

    private static readonly Color CPanel      = new(0.12f, 0.12f, 0.12f);
    private static readonly Color CBg         = new(0.16f, 0.16f, 0.16f);
    private static readonly Color CCard       = new(0.18f, 0.18f, 0.18f);
    private static readonly Color CAccent     = new(0.28f, 0.62f, 0.98f);
    private static readonly Color CAccentDim  = new(0.18f, 0.40f, 0.65f);
    private static readonly Color CDivider    = new(0.26f, 0.26f, 0.26f);
    private static readonly Color CUnsaved    = new(0.98f, 0.65f, 0.15f);
    private static readonly Color CGreen      = new(0.27f, 0.82f, 0.50f);
    private static readonly Color CRed        = new(0.92f, 0.35f, 0.30f);
    private static readonly Color CWarn       = new(0.95f, 0.70f, 0.20f);
    private static readonly Color CCurve      = new(0.98f, 0.70f, 0.25f);
    private static readonly Color CCurveFill  = new(0.98f, 0.70f, 0.25f, 0.09f);
    private static readonly Color CCurveBg    = new(0.09f, 0.09f, 0.09f);
    private static readonly Color CGrid       = new(0.20f, 0.20f, 0.20f);
    private static readonly Color CSubH       = new(0.20f, 0.20f, 0.20f);
    private static readonly Color CDiffOld    = new(0.95f, 0.35f, 0.30f, 0.18f);
    private static readonly Color CDiffNew    = new(0.27f, 0.82f, 0.50f, 0.18f);
    private static readonly Color CCreator    = new(0.70f, 0.85f, 1.00f);

    // ═══════════════════════════════════════════════════════════════════════
    //  EditorWindow Lifecycle
    // ═══════════════════════════════════════════════════════════════════════

    private void OnEnable()
    {
        // Invalidate styles on domain reload
        stylesReady = false;

        LoadAll();
        SnapshotSaved();
        RebuildCurve();
        lastTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Tick;
        if (dirty)
            Debug.LogWarning("[FlowTween] Settings window closed with unsaved changes. Open the window and click Save & Apply.");

        // Dispose cached textures to avoid editor memory leak
        foreach (var tex in texCache.Values)
            if (tex != null) DestroyImmediate(tex);
        texCache.Clear();
    }

    private void Tick()
    {
        double now   = EditorApplication.timeSinceStartup;
        double delta = now - lastTime;
        lastTime = now;

        // Curve animation
        if (animPlay)
        {
            animT = (float)((now * 0.5) % 1.0);
            Repaint();
        }

        // Pool history sampling (play mode only)
        if (Application.isPlaying && activeTab == Tab.Pool)
        {
            poolSampleAccum += (float)delta;
            if (poolSampleAccum >= PoolSampleRate)
            {
                poolSampleAccum = 0f;
                float poolSz = FlowTween.TweenPoolSize;
                float hitR   = FlowTween.TweenPoolHitRate >= 0f ? FlowTween.TweenPoolHitRate : 0f;
                int   misses = FlowTween.TweenPoolMisses;

                tweenPoolHist[poolHistHead] = poolSz;
                seqPoolHist  [poolHistHead] = FlowTween.SequencePoolSize;
                hitRateHist  [poolHistHead] = hitR;
                allocHist    [poolHistHead] = Mathf.Max(0, misses - lastMissCount);

                lastMissCount = misses;
                poolHistHead  = (poolHistHead + 1) % PoolHistLen;

                if (poolSz  > peakPoolSize) peakPoolSize = poolSz;
                if (hitR    > peakHitRate)  peakHitRate  = hitR;

                Repaint();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  OnGUI
    // ═══════════════════════════════════════════════════════════════════════

    private void OnGUI()
    {
        EnsureStyles();
        EnsureAsset();

        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), CBg);

        DrawTopBar();
        DrawTabBar();
        HRule(CAccent, 2);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUIStyle.none, GUI.skin.verticalScrollbar);
        GUILayout.Space(10);

        switch (activeTab)
        {
            case Tab.Runtime:   DrawRuntime();   break;
            case Tab.Pool:      DrawPool();      break;
            case Tab.Lifecycle: DrawLifecycle(); break;
            case Tab.Debugger:  DrawDebugger();  break;
            case Tab.About:     DrawAbout();     break;
        }

        GUILayout.Space(56);
        EditorGUILayout.EndScrollView();

        DrawBottomBar();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Top Bar
    // ═══════════════════════════════════════════════════════════════════════

    private void DrawTopBar()
    {
        const float h = 48f;
        EditorGUI.DrawRect(new Rect(0, 0, position.width, h), CPanel);
        EditorGUI.DrawRect(new Rect(0, 0, 4, h), CAccent); // accent stripe

        GUILayout.BeginHorizontal(GUILayout.Height(h));
        GUILayout.Space(16);
        GUILayout.Label("FlowTween  ⚙  Settings", sHeader, GUILayout.Height(h));
        GUILayout.FlexibleSpace();

        // Status pills
        if (Application.isPlaying)   { Pill("● PLAY",     CGreen,   Color.black); GUILayout.Space(4); }
        if (dirty)                    { Pill("● Unsaved",  CUnsaved, Color.black); GUILayout.Space(4); }
        else if (appliedThisSession)  { Pill("✔ Applied",  CGreen,   Color.black); GUILayout.Space(4); }
        if (asset == null)            { Pill("No Asset",   CRed,     Color.white); GUILayout.Space(4); }

        // Version
        GUILayout.Label($"v{Version}", sVersionLabel, GUILayout.Height(h));
        GUILayout.Space(10);
        GUILayout.EndHorizontal();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Tab Bar
    // ═══════════════════════════════════════════════════════════════════════

    private void DrawTabBar()
    {
        const float h = 36f;
        EditorGUI.DrawRect(new Rect(0, 48, position.width, h), CPanel);

        GUILayout.BeginHorizontal(GUILayout.Height(h));
        GUILayout.Space(8);

        // Tab icons + labels
        string[] icons  = { "▶", "⊡", "♻", "🔍", "ℹ" };
        Tab[]    tabs   = { Tab.Runtime, Tab.Pool, Tab.Lifecycle, Tab.Debugger, Tab.About };
        for (int i = 0; i < tabs.Length; i++)
        {
            bool active = activeTab == tabs[i];
            var  style  = active ? sTabActive : sTab;
            if (GUILayout.Button($"{icons[i]}  {tabs[i]}", style, GUILayout.Height(32)))
                activeTab = tabs[i];

            if (active)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                EditorGUI.DrawRect(new Rect(r.x, r.yMax - 2, r.width, 2), CAccent);
            }
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Tab — Runtime
    // ═══════════════════════════════════════════════════════════════════════

    private void DrawRuntime()
    {
        SectionTitle("Runtime Defaults",
            "Applied to every new tween that doesn't set an explicit override.");

        // ── Presets ───────────────────────────────────────────────────────
        SubHeader("⚡  Presets");
        BeginCard();

        GUILayout.BeginHorizontal();
        for (int i = 0; i < Presets.Length; i++)
        {
            var p     = Presets[i];
            bool active = activePresetIdx == i;
            var  style  = active ? sPresetBtnActive : sPresetBtn;
            var  prev   = GUI.backgroundColor;
            GUI.backgroundColor = active ? CAccent : new Color(0.26f, 0.26f, 0.26f);

            if (GUILayout.Button(new GUIContent($"{p.icon}  {p.label}", p.tooltip), style, GUILayout.Height(26)))
            {
                ApplyPreset(i);
            }
            GUI.backgroundColor = prev;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        if (activePresetIdx >= 0)
        {
            var prev = GUI.color; GUI.color = new Color(1,1,1,0.5f);
            GUILayout.Label($"  Using preset: {Presets[activePresetIdx].label} — edit any field to customise.",
                            EditorStyles.miniLabel);
            GUI.color = prev;
        }
        EndCard();

        // ── Easing ────────────────────────────────────────────────────────
        SubHeader("◑  Easing");
        BeginCard();

        EditorGUI.BeginChangeCheck();
        var nt = (Tween.TransitionType)EditorGUILayout.EnumPopup(
            Tip("Transition", "Curve shape for all new tweens. Override per-tween with .Quad(), .Bounce(), etc."),
            defTransition);
        var ne = (Tween.EaseType)EditorGUILayout.EnumPopup(
            Tip("Ease Direction", "In / Out / InOut / OutIn — applied globally unless overridden."),
            defEase);
        if (EditorGUI.EndChangeCheck() && (nt != defTransition || ne != defEase))
        {
            defTransition  = nt; defEase = ne;
            activePresetIdx = -1;
            RebuildCurve(); MarkDirty();
        }

        GUILayout.Space(6);
        DrawCurvePreview();

        // Diff row for easing
        if (dirty && (defTransition != savedTransition || defEase != savedEase))
            DiffRow($"{savedTransition} / {savedEase}", $"{defTransition} / {defEase}");

        EndCard();

        // ── Time ──────────────────────────────────────────────────────────
        SubHeader("⏱  Time Scale");
        BeginCard();

        float nts = EditorGUILayout.Slider(
            Tip("Global Time Scale",
                "FlowTween-layer multiplier on top of Time.deltaTime. "
              + "Does NOT change Unity's Time.timeScale. "
              + "Tweens using SetUnscaledTime(true) are immune."),
            globalTimeScale, 0f, 4f);
        if (!Mathf.Approximately(nts, globalTimeScale)) { globalTimeScale = nts; activePresetIdx = -1; MarkDirty(); }

        // Status row
        if (Mathf.Approximately(globalTimeScale, 1f))
            StatusRow("✔", "Running at normal speed  (1×)", CGreen);
        else if (globalTimeScale < 1f)
            StatusRow("⚠", $"Tweens slowed to  {globalTimeScale:0.##}×", CWarn);
        else
            StatusRow("⚡", $"Tweens sped up to  {globalTimeScale:0.##}×", CAccent);

        // Quick-set buttons
        GUILayout.BeginHorizontal();
        GUILayout.Label("Quick:", EditorStyles.miniLabel, GUILayout.Width(38));
        foreach (float v in new[] { 0f, 0.25f, 0.5f, 1f, 1.5f, 2f, 3f })
        {
            bool cur = Mathf.Approximately(globalTimeScale, v);
            var  bg  = GUI.backgroundColor;
            GUI.backgroundColor = cur ? CAccent : new Color(0.28f, 0.28f, 0.28f);
            if (GUILayout.Button(v + "×", EditorStyles.miniButton, GUILayout.Width(36)))
            { globalTimeScale = v; activePresetIdx = -1; MarkDirty(); }
            GUI.backgroundColor = bg;
        }
        GUILayout.EndHorizontal();

        // Live Unity time scale comparison (play mode)
        if (Application.isPlaying)
        {
            GUILayout.Space(4);
            float unityTs = Time.timeScale;
            EditorGUILayout.LabelField(
                $"Unity Time.timeScale = {unityTs:0.##}×   →   effective tween speed = {unityTs * globalTimeScale:0.##}×",
                EditorStyles.miniLabel);
        }

        if (dirty && !Mathf.Approximately(globalTimeScale, savedGts))
            DiffRow($"{savedGts:0.##}×", $"{globalTimeScale:0.##}×");

        EndCard();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Tab — Pool
    // ═══════════════════════════════════════════════════════════════════════

    private void DrawPool()
    {
        SectionTitle("Object Pool",
            "Pre-warming and shrink tuning reduce GC pressure at runtime.");

        // ── Pre-warm ──────────────────────────────────────────────────────
        SubHeader("🔥  Pre-warm  (filled before first frame)");
        BeginCard();

        int npt = EditorGUILayout.IntSlider(Tip("Tween Count",    "Tween objects placed in pool at startup."), prewarmTweens, 0, 256);
        int nps = EditorGUILayout.IntSlider(Tip("Sequence Count", "Sequence objects placed in pool at startup."), prewarmSeqs, 0, 64);
        if (npt != prewarmTweens) { prewarmTweens = npt; MarkDirty(); }
        if (nps != prewarmSeqs)   { prewarmSeqs   = nps; MarkDirty(); }

        // Memory estimate — rough (~160 B / Tween, ~80 B / Sequence)
        int estBytes = prewarmTweens * 160 + prewarmSeqs * 80;
        StatusRow("≈", $"{estBytes / 1024f:0.0} KB reserved on startup", CAccent);

        if (dirty && (prewarmTweens != savedPrewarmT || prewarmSeqs != savedPrewarmS))
            DiffRow($"T:{savedPrewarmT}  S:{savedPrewarmS}", $"T:{prewarmTweens}  S:{prewarmSeqs}");

        EndCard();

        // ── Shrink Policy ─────────────────────────────────────────────────
        SubHeader("📉  Shrink Policy");
        BeginCard();

        int   nm = EditorGUILayout.IntSlider(Tip("Min Pool Size",    "Pool is never trimmed below this floor."), minPoolSize, 0, 64);
        float ni = EditorGUILayout.Slider(   Tip("Shrink Interval",  "Seconds between auto trim passes."), shrinkInterval, 0.5f, 60f);
        float np = EditorGUILayout.Slider(   Tip("Shrink Percent",   "Fraction of excess removed per pass. 0 = never, 1 = aggressive."), shrinkPercent, 0f, 1f);
        if (nm != minPoolSize)                       { minPoolSize    = nm; MarkDirty(); }
        if (!Mathf.Approximately(ni, shrinkInterval)){ shrinkInterval = ni; MarkDirty(); }
        if (!Mathf.Approximately(np, shrinkPercent)) { shrinkPercent  = np; MarkDirty(); }

        StatusRow("→", $"Trims up to {shrinkPercent*100f:0}% of excess every {shrinkInterval:0}s  (floor = {minPoolSize})", new Color(0.65f,0.65f,0.65f));

        if (dirty && (minPoolSize != savedMinPool || !Mathf.Approximately(shrinkInterval, savedShrinkI) || !Mathf.Approximately(shrinkPercent, savedShrinkP)))
            DiffRow($"min={savedMinPool}  every={savedShrinkI:0}s  {savedShrinkP*100:0}%",
                    $"min={minPoolSize}   every={shrinkInterval:0}s  {shrinkPercent*100:0}%");

        EndCard();

        // ── Live Stats ────────────────────────────────────────────────────
        SubHeader("📊  Live Stats" + (Application.isPlaying ? "" : "   (enter Play Mode)"));
        BeginCard();

        if (Application.isPlaying)
        {
            // Four stat tiles
            GUILayout.BeginHorizontal();
            StatTile("Active\nTweens",    FlowTween.ActiveCount.ToString(),         CAccent);
            StatTile("Active\nSequences", FlowTween.ActiveSequenceCount.ToString(), CAccent);
            StatTile("Pool\nSize",        FlowTween.TweenPoolSize.ToString(),       CGreen);
            StatTile("Total\nAllocs",     FlowTween.TweenPoolMisses.ToString(),     FlowTween.TweenPoolMisses > 100 ? CWarn : CGreen);
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            // Hit rate bars
            DrawHitBar("Tween Hit Rate",    FlowTween.TweenPoolHitRate,    peakHitRate);
            DrawHitBar("Sequence Hit Rate", FlowTween.SequencePoolHitRate, -1f);
            GUILayout.Space(4);

            // Graphs
            DrawGraph(tweenPoolHist, poolHistHead, $"Tween Pool Size  (peak {peakPoolSize:0})", CAccent,  40);
            DrawGraph(hitRateHist,   poolHistHead, "Tween Hit Rate",                            CGreen,   32);
            DrawGraph(allocHist,     poolHistHead, "New Allocs / Sample",                       CWarn,    28);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (SmallBtn("Reset Stats",       CRed))      FlowTween.ResetPoolStats();
            if (SmallBtn("Force Prewarm Now", CAccentDim)) FlowTween.Prewarm(prewarmTweens, prewarmSeqs);
            GUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Pool statistics and graphs are available during Play Mode.", MessageType.Info);
        }

        EndCard();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Tab — Lifecycle
    // ═══════════════════════════════════════════════════════════════════════

    private void DrawLifecycle()
    {
        SectionTitle("Lifecycle & Safety",
            "Controls how FlowTween reacts to scene transitions and destroyed targets.");

        SubHeader("🔄  Scene Transitions");
        BeginCard();

        bool nku = Tog("Kill Tweens On Scene Unload",
            "Tweens whose Target belongs to an unloaded scene are automatically killed. "
          + "Disable only if you manage tween lifetimes entirely by hand.", killOnUnload);
        if (nku != killOnUnload) { killOnUnload = nku; MarkDirty(); }

        if (!killOnUnload)
            EditorGUILayout.HelpBox("You are responsible for killing tweens before their scene unloads. "
                                  + "Failing to do so will cause MissingReferenceExceptions.", MessageType.Warning);

        if (dirty && killOnUnload != savedKillUnload)
            DiffRow(savedKillUnload ? "enabled" : "disabled", killOnUnload ? "enabled" : "disabled");
        EndCard();

        SubHeader("👻  Orphan Detection");
        BeginCard();

        bool nao = Tog("Auto-Kill Null Targets",
            "When a tween's Target is destroyed mid-play the tween is killed on the next tick "
          + "and returned to the pool. Costs one UnityObject null-check per active tween per frame.",
            autoKillOrphans);
        if (nao != autoKillOrphans) { autoKillOrphans = nao; MarkDirty(); }

        if (dirty && autoKillOrphans != savedAutoKill)
            DiffRow(savedAutoKill ? "enabled" : "disabled", autoKillOrphans ? "enabled" : "disabled");
        EndCard();

        // ── Diff summary ──────────────────────────────────────────────────
        if (dirty)
        {
            SubHeader("📋  Pending Changes");
            BeginCard();
            bool any = false;
            if (defTransition != savedTransition || defEase != savedEase)
            { DiffRow($"Ease: {savedTransition}/{savedEase}", $"Ease: {defTransition}/{defEase}"); any = true; }
            if (!Mathf.Approximately(globalTimeScale, savedGts))
            { DiffRow($"GTS: {savedGts:0.##}×", $"GTS: {globalTimeScale:0.##}×"); any = true; }
            if (killOnUnload != savedKillUnload)
            { DiffRow($"KillOnUnload: {savedKillUnload}", $"KillOnUnload: {killOnUnload}"); any = true; }
            if (autoKillOrphans != savedAutoKill)
            { DiffRow($"AutoKill: {savedAutoKill}", $"AutoKill: {autoKillOrphans}"); any = true; }
            if (prewarmTweens != savedPrewarmT || prewarmSeqs != savedPrewarmS)
            { DiffRow($"Prewarm T:{savedPrewarmT}/S:{savedPrewarmS}", $"Prewarm T:{prewarmTweens}/S:{prewarmSeqs}"); any = true; }
            if (minPoolSize != savedMinPool || !Mathf.Approximately(shrinkInterval, savedShrinkI) || !Mathf.Approximately(shrinkPercent, savedShrinkP))
            { DiffRow($"Shrink: min={savedMinPool} every={savedShrinkI:0}s {savedShrinkP*100:0}%",
                      $"Shrink: min={minPoolSize}  every={shrinkInterval:0}s {shrinkPercent*100:0}%"); any = true; }
            if (!any)
                GUILayout.Label("  No changes detected.", EditorStyles.miniLabel);
            EndCard();
        }

        SubHeader("🎛  Global Controls" + (Application.isPlaying ? "" : "  (enter Play Mode)"));
        BeginCard();

        if (Application.isPlaying)
        {
            GUILayout.BeginHorizontal();
            PillInline($"{FlowTween.ActiveCount} tweens",          CAccent,    Color.white);
            GUILayout.Space(4);
            PillInline($"{FlowTween.ActiveSequenceCount} sequences", CAccentDim, Color.white);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            DangerBtn("Kill All Tweens",    FlowTween.KillAll);
            DangerBtn("Pause All Tweens",   FlowTween.PauseAll);
            SafeBtn  ("Resume All",         FlowTween.ResumeAll);
            SafeBtn  ("Complete All",       FlowTween.CompleteAll);
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            DangerBtn("Kill All Sequences",   FlowTween.KillSequences);
            DangerBtn("Pause All Sequences",  FlowTween.PauseSequences);
            SafeBtn  ("Resume All Sequences", FlowTween.ResumeSequences);
            GUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Global control buttons are available during Play Mode.", MessageType.Info);
        }

        EndCard();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Tab — Debugger
    // ═══════════════════════════════════════════════════════════════════════

    private void DrawDebugger()
    {
        SectionTitle("Debugger Preferences",
            "Affect only the FlowTween Debugger window. Stored in EditorPrefs — not included in builds.");

        SubHeader("⚡  Performance");
        BeginCard();

        float nrr  = EditorGUILayout.Slider(Tip("Repaint Rate (s)",      "Minimum seconds between repaints. 0 = every editor frame."), refreshRate, 0f, 1f);
        int   nslo  = EditorGUILayout.IntSlider(Tip("Slow-Mode Threshold","Above this tween count the debugger auto-slows its repaint."), slowModeThreshold, 10, 2000);
        int   nmx   = EditorGUILayout.IntSlider(Tip("Max Visible Cards",  "Virtual scroll cap. 0 = unlimited."), maxVisibleCards, 0, 2000);

        if (!Mathf.Approximately(nrr, refreshRate)) { refreshRate       = nrr;  MarkDirty(); }
        if (nslo != slowModeThreshold)               { slowModeThreshold = nslo; MarkDirty(); }
        if (nmx  != maxVisibleCards)                 { maxVisibleCards   = nmx;  MarkDirty(); }
        EndCard();

        SubHeader("🎨  Display");
        BeginCard();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Card Density", GUILayout.Width(100));
        int nd = GUILayout.Toolbar(density, new[] { "Compact", "Comfortable", "Spacious" });
        GUILayout.EndHorizontal();
        if (nd != density) { density = nd; MarkDirty(); }

        GUILayout.Space(6);
        bool nc = Tog("Curve Previews",        "Show ease-curve thumbnails on tween cards.", showCurvePreviews);
        bool ns = Tog("Sparklines",             "Show per-tween progress sparkline history.", showSparklines);
        bool nt = Tog("TimeScale Sliders",      "Show per-tween time-scale sliders on cards.", showTsSliders);
        bool nh = Tog("Highlight Near Complete","Turn cards orange as they near completion.", hlNearComplete);

        if (nc != showCurvePreviews) { showCurvePreviews = nc; MarkDirty(); }
        if (ns != showSparklines)    { showSparklines    = ns; MarkDirty(); }
        if (nt != showTsSliders)     { showTsSliders     = nt; MarkDirty(); }
        if (nh != hlNearComplete)    { hlNearComplete    = nh; MarkDirty(); }
        EndCard();

        SubHeader("🛡  Safety");
        BeginCard();
        bool nck = Tog("Confirm Before Kill", "Show a confirmation dialog when Kill is triggered from the debugger.", confirmKill);
        bool ngr = Tog("Enable Graveyard",    "Keep a rolling log of recently completed/killed tweens.", enableGraveyard);
        bool nel = Tog("Enable Event Log",    "Record Start / Complete / Kill / Loop events in the Event Log tab.", enableEventLog);

        if (nck != confirmKill)     { confirmKill     = nck; MarkDirty(); }
        if (ngr != enableGraveyard) { enableGraveyard = ngr; MarkDirty(); }
        if (nel != enableEventLog)  { enableEventLog  = nel; MarkDirty(); }

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        if (SmallBtn("Reset All Debugger Prefs", CRed))
        {
            if (EditorUtility.DisplayDialog("Reset Debugger Prefs",
                "This will reset ALL FlowTween debugger EditorPrefs to their defaults.", "Reset", "Cancel"))
            {
                ResetEditorPrefsToDefaults();
                MarkDirty();
            }
        }
        GUILayout.EndHorizontal();
        EndCard();

        GUILayout.Space(4);
        var bgPrev = GUI.backgroundColor;
        GUI.backgroundColor = CAccentDim;
        if (GUILayout.Button("Open FlowTween Debugger  →", GUILayout.Height(30)))
        {
            // Use the exact MenuItem path declared on FlowTweenDebugWindow.
            // This is more reliable than Type.GetType() which requires knowing
            // the compiled assembly name (which can vary per project).
            const string debuggerMenuPath = "Window/Analysis/FlowTween Debugger";
            if (!EditorApplication.ExecuteMenuItem(debuggerMenuPath))
                Debug.LogWarning("[FlowTween] Could not open the debugger. Make sure FlowTweenDebugWindow.cs is inside an Editor folder.");
        }
        GUI.backgroundColor = bgPrev;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Tab — About
    // ═══════════════════════════════════════════════════════════════════════

    private void DrawAbout()
    {
        SectionTitle("About FlowTween", "");

        // ── Identity card ─────────────────────────────────────────────────
        BeginCard();

        // Title + version banner
        EditorGUI.DrawRect(GUILayoutUtility.GetRect(1, 6, GUILayout.ExpandWidth(true)),
                           new Color(0, 0, 0, 0)); // spacer

        GUILayout.BeginHorizontal();
        GUILayout.Label("⚡ FlowTween", sHeader);
        GUILayout.FlexibleSpace();
        var verStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11, normal = { textColor = CAccent },
            alignment = TextAnchor.MiddleRight
        };
        GUILayout.Label($"v{Version}", verStyle, GUILayout.Width(60));
        GUILayout.EndHorizontal();

        GUILayout.Space(2);
        GUILayout.Label("Lightweight, zero-dependency, pooled tweening for Unity.",
                        EditorStyles.wordWrappedLabel);

        GUILayout.Space(10);
        HRule(CDivider);
        GUILayout.Space(8);

        // Creator — highlighted
        GUILayout.BeginHorizontal();
        GUILayout.Label("Creator:", sLabelBold, GUILayout.Width(110));
        var creatorStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            normal   = { textColor = CCreator }
        };
        GUILayout.Label("Ahmed GD", creatorStyle);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        HRule(CDivider);
        GUILayout.Space(6);

        // Tech details
        KV("Namespace",      "FlT");
        KV("Entry Point",    "FlowTween  (MonoBehaviour, auto-spawned via RuntimeInitializeOnLoad)");
        KV("Settings Asset", AssetPath);
        KV("Shortcut",       "Window ▶ FlowTween ▶ Settings   (Alt+Shift+S)");
        KV("Easing",         "12 transition types  ×  4 ease directions  +  AnimationCurve");
        KV("Interpolators",  "Transform, RectTransform, CanvasGroup, Material, Light  +  FlowVirtual");
        KV("Pool Types",     "Tween  +  Sequence  +  12 typed struct interpolators");
        EndCard();

        // ── Quick Reference ───────────────────────────────────────────────
        SubHeader("📖  Quick Reference");
        BeginCard();
        DrawCode(
@"// ── Basic (struct extension, zero GC) ─────────────────────────────────────
transform.TweenPosition(Vector3.up * 3f, 0.6f).EaseOut().Quad();
transform.TweenScale(Vector3.one * 1.2f, 0.4f).Spring().EaseOut();

// ── Callbacks ───────────────────────────────────────────────────────────────
transform.TweenRotation(Quaternion.Euler(0,180,0), 1f)
    .OnStart(() => Debug.Log(""start""))
    .OnComplete(() => Debug.Log(""done""))
    .SetDelay(0.2f);

// ── Virtual (no target required) ────────────────────────────────────────────
FlowVirtual.Float(0f, 1f, 0.4f, v => canvasGroup.alpha = v);
FlowVirtual.Color(Color.clear, Color.white, 0.5f, c => img.color = c);

// ── Chaining ────────────────────────────────────────────────────────────────
transform.TweenPosition(Vector3.up, 0.3f)
    .Then(transform.TweenPosition(Vector3.zero, 0.3f));

// ── Sequence ────────────────────────────────────────────────────────────────
FlowTween.Sequence()
    .Append(transform.TweenScale(Vector3.one * 1.2f, 0.25f).Bounce().EaseOut())
    .Join(img.TweenAlpha(0.5f, 0.25f))
    .AppendInterval(0.1f)
    .Append(transform.TweenScale(Vector3.one, 0.2f).Quad().EaseIn())
    .SetLoops(3)
    .OnComplete(() => Debug.Log(""sequence done""))
    .Play();

// ── Groups & IDs ─────────────────────────────────────────────────────────────
transform.TweenPosition(dest, 1f).SetGroup(""UI"").SetId(""fade"");
FlowTween.KillGroup(""UI"");
FlowTween.KillById(""fade"");"
        );
        EndCard();

        // ── Architecture Overview ─────────────────────────────────────────
        SubHeader("🏗  Architecture");
        BeginCard();
        GUILayout.Label(
@"FlowTween (MonoBehaviour singleton)
  ├─ activeTweens  [ ]  ──►  Tween.Update(dt, unscaledDt)
  ├─ activeFixedTweens [ ]   Tween.Update(fixedDt, ...)
  ├─ activeSequences [ ]  ──►  Sequence.Update(dt)
  ├─ tweenPool  Stack<Tween>        ← ResetData() on return
  ├─ sequencePool  Stack<Sequence>  ← ResetData() on return
  └─ groups  Dict<string, HashSet<Tween>>

Tween
  ├─ ITweenInterpolator  (StructTweenInterpolator / FloatInterpolator / ...)
  ├─ EaseMath.Evaluate(t, transition, ease)
  └─ AnimationCurve  (optional override)

FlowTweenSettings  (ScriptableObject)
  └─ Loaded via Resources.Load at RuntimeInitializeOnLoad
     → FlowTween.ApplySettings() applies all fields live",
            EditorStyles.miniLabel);
        EndCard();

        // ── Import / Export ───────────────────────────────────────────────
        SubHeader("💾  Settings File");
        BeginCard();
        GUILayout.Label(
            "Export saves a human-readable JSON you can share, diff, or commit to VCS.\n"
          + "Import reads that file back and marks all fields as pending (still needs Save).",
            EditorStyles.wordWrappedMiniLabel);
        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        if (SmallBtn("⬆  Export JSON", CAccentDim)) ExportJSON();
        if (SmallBtn("⬇  Import JSON", CAccentDim)) ImportJSON();
        if (asset != null && SmallBtn("Ping Asset",   new Color(0.3f,0.3f,0.3f)))
            EditorGUIUtility.PingObject(asset);
        GUILayout.EndHorizontal();
        EndCard();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Bottom Bar
    // ═══════════════════════════════════════════════════════════════════════

    private void DrawBottomBar()
    {
        const float bh = 46f;
        float by = position.height - bh;
        EditorGUI.DrawRect(new Rect(0, by,     position.width, 1),      CDivider);
        EditorGUI.DrawRect(new Rect(0, by + 1, position.width, bh - 1), CPanel);

        GUILayout.BeginArea(new Rect(10, by + 9, position.width - 20, bh - 12));
        GUILayout.BeginHorizontal();

        // Revert
        EditorGUI.BeginDisabledGroup(!dirty);
        if (GUILayout.Button("↺  Revert", GUILayout.Width(86), GUILayout.Height(28)))
        { LoadAll(); SnapshotSaved(); dirty = false; RebuildCurve(); activePresetIdx = -1; Repaint(); }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(4);
        if (GUILayout.Button("⬆ Export", GUILayout.Width(70), GUILayout.Height(28))) ExportJSON();
        if (GUILayout.Button("⬇ Import", GUILayout.Width(70), GUILayout.Height(28))) ImportJSON();

        GUILayout.FlexibleSpace();

        if (asset == null)
        {
            var pc = GUI.color; GUI.color = CRed;
            GUILayout.Label("Asset missing — will be created on Save", EditorStyles.miniLabel, GUILayout.Height(28));
            GUI.color = pc; GUILayout.Space(8);
        }
        else if (dirty)
        {
            var pc = GUI.color; GUI.color = CUnsaved;
            GUILayout.Label("● Unsaved changes", EditorStyles.miniLabel, GUILayout.Height(28));
            GUI.color = pc; GUILayout.Space(8);
        }

        var bg = GUI.backgroundColor;
        GUI.backgroundColor = dirty ? CAccent : CAccentDim;
        if (GUILayout.Button(dirty ? "✔  Save & Apply" : "✔  Saved",
                             GUILayout.Width(130), GUILayout.Height(28)))
            SaveAll();
        GUI.backgroundColor = bg;

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Curve Preview
    // ═══════════════════════════════════════════════════════════════════════

    private void DrawCurvePreview()
    {
        GUILayout.Label("Curve Preview", EditorStyles.miniLabel);
        Rect r = GUILayoutUtility.GetRect(1, 96, GUILayout.ExpandWidth(true));
        r.x += 2; r.width -= 4; r.height = 96;

        EditorGUI.DrawRect(r, CCurveBg);

        // Grid
        Handles.color = CGrid;
        for (int n = 1; n < 4; n++)
        {
            Handles.DrawLine(new Vector3(r.x + n/4f*r.width, r.y), new Vector3(r.x + n/4f*r.width, r.yMax));
            Handles.DrawLine(new Vector3(r.x, r.y + n/4f*r.height), new Vector3(r.xMax, r.y + n/4f*r.height));
        }

        // Border
        Handles.color = CDivider;
        Handles.DrawLine(new Vector3(r.xMin, r.yMin), new Vector3(r.xMax, r.yMin));
        Handles.DrawLine(new Vector3(r.xMin, r.yMax), new Vector3(r.xMax, r.yMax));
        Handles.DrawLine(new Vector3(r.xMin, r.yMin), new Vector3(r.xMin, r.yMax));
        Handles.DrawLine(new Vector3(r.xMax, r.yMin), new Vector3(r.xMax, r.yMax));

        // Axis labels
        var axisStyle = new GUIStyle(EditorStyles.miniLabel)
        { normal = { textColor = new Color(0.5f,0.5f,0.5f) }, fontSize = 9 };
        EditorGUI.LabelField(new Rect(r.x + 1,       r.yMax - 13, 20, 12), "0",   axisStyle);
        EditorGUI.LabelField(new Rect(r.xMax - 14,   r.yMax - 13, 20, 12), "1",   axisStyle); // x=1
        EditorGUI.LabelField(new Rect(r.x + 1,       r.y,          20, 12), "1",   axisStyle); // y=1
        EditorGUI.LabelField(new Rect(r.x + r.width/2f - 6, r.yMax - 13, 20, 12), "t",  axisStyle);

        // Overshoot zone indicator for Bounce/Elastic/Spring/Back
        bool hasOvershoot = defTransition is Tween.TransitionType.Bounce
                         or Tween.TransitionType.Elastic
                         or Tween.TransitionType.Spring
                         or Tween.TransitionType.Back;
        if (hasOvershoot)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y - 10, r.width, 10),
                               new Color(0.98f, 0.70f, 0.25f, 0.06f));
            EditorGUI.LabelField(new Rect(r.x + 2, r.y - 11, 80, 11),
                                 "overshoot", axisStyle);
        }

        if (curvePoints == null) RebuildCurve();

        // Fill + line
        if (curvePoints != null)
        {
            float sliceW = r.width / (curvePoints.Length - 1) + 1;
            for (int i = 0; i < curvePoints.Length; i++)
            {
                float nx = i / (float)(curvePoints.Length - 1);
                float ny = 1f - Mathf.Clamp01(curvePoints[i]);
                float px = r.x + nx * r.width;
                float py = r.y + ny * r.height;
                EditorGUI.DrawRect(new Rect(px, py, sliceW, r.yMax - py), CCurveFill);
            }

            Handles.color = CCurve;
            for (int i = 1; i < curvePoints.Length; i++)
            {
                float nx0 = (i-1)/(float)(curvePoints.Length-1), nx1 = i/(float)(curvePoints.Length-1);
                float ny0 = 1f-Mathf.Clamp01(curvePoints[i-1]),  ny1 = 1f-Mathf.Clamp01(curvePoints[i]);
                Handles.DrawLine(
                    new Vector3(r.x + nx0*r.width, r.y + ny0*r.height),
                    new Vector3(r.x + nx1*r.width, r.y + ny1*r.height));
            }
        }

        // Animated dot
        if (animPlay && curvePoints != null)
        {
            int   idx = Mathf.Clamp(Mathf.RoundToInt(animT * (curvePoints.Length - 1)), 0, curvePoints.Length - 1);
            float dy  = 1f - Mathf.Clamp01(curvePoints[idx]);
            float px  = r.x + animT * r.width;
            float py  = r.y + dy    * r.height;
            EditorGUI.DrawRect(new Rect(px-5, py-5, 10, 10), new Color(1,1,1,0.2f));
            EditorGUI.DrawRect(new Rect(px-3, py-3, 6,  6),  CCurve);
        }

        GUILayout.Space(3);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(animPlay ? "■ Stop" : "▶ Play", EditorStyles.miniButton, GUILayout.Width(56)))
            animPlay = !animPlay;
        GUILayout.FlexibleSpace();
        string lbl = defTransition == Tween.TransitionType.Linear ? "Linear" : $"{defTransition}  {defEase}";
        GUILayout.Label(lbl, EditorStyles.centeredGreyMiniLabel);
        GUILayout.EndHorizontal();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Graph
    // ═══════════════════════════════════════════════════════════════════════

    private void DrawGraph(float[] data, int head, string label, Color col, float height)
    {
        GUILayout.Label(label, EditorStyles.miniLabel);
        Rect r = GUILayoutUtility.GetRect(1, height, GUILayout.ExpandWidth(true));
        r.x += 2; r.width -= 4;
        EditorGUI.DrawRect(r, CCurveBg);

        float maxVal = 0.001f;
        for (int i = 0; i < data.Length; i++) if (data[i] > maxVal) maxVal = data[i];

        Handles.color = col;
        for (int i = 1; i < data.Length; i++)
        {
            int   pi = (head + i - 1) % data.Length;
            int   ci = (head + i)     % data.Length;
            float x0 = r.x + (i-1) / (float)(data.Length-1) * r.width;
            float x1 = r.x +  i    / (float)(data.Length-1) * r.width;
            float y0 = r.yMax - (data[pi] / maxVal) * r.height;
            float y1 = r.yMax - (data[ci] / maxVal) * r.height;
            Handles.DrawLine(new Vector3(x0, y0), new Vector3(x1, y1));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Hit Rate Bar
    // ═══════════════════════════════════════════════════════════════════════

    private void DrawHitBar(string label, float rate, float peak)
    {
        if (rate < 0f) { GUILayout.Label($"{label}: n/a", EditorStyles.miniLabel); return; }
        GUILayout.Label($"{label}: {rate*100f:0.0}%{(peak >= 0f ? $"  (peak {peak*100f:0.0}%)" : "")}", EditorStyles.miniLabel);
        Rect r = GUILayoutUtility.GetRect(1, 8, GUILayout.ExpandWidth(true));
        r.x += 2; r.width -= 4;
        EditorGUI.DrawRect(r, new Color(0.18f, 0.18f, 0.18f));
        Color bc = rate > 0.8f ? CGreen : rate > 0.5f ? CWarn : CRed;
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width * Mathf.Clamp01(rate), r.height), bc);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Stat Tile
    // ═══════════════════════════════════════════════════════════════════════

    private void StatTile(string label, string value, Color accent)
    {
        GUILayout.BeginVertical("HelpBox", GUILayout.Width((position.width - 36) / 4f));
        var valStyle = new GUIStyle(EditorStyles.boldLabel)
        { fontSize = 16, normal = { textColor = accent }, alignment = TextAnchor.MiddleCenter };
        GUILayout.Label(value, valStyle, GUILayout.Height(26));
        GUILayout.Label(label, new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 9 });
        GUILayout.EndVertical();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Diff Row
    // ═══════════════════════════════════════════════════════════════════════

    private void DiffRow(string old, string nw)
    {
        GUILayout.BeginHorizontal();

        EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 18, GUILayout.Width(0)), new Color(0,0,0,0)); // spacer

        // Old value
        var bgPrev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0,0,0,0);
        var rOld = GUILayoutUtility.GetRect(new GUIContent(old), sDiffOld, GUILayout.MinWidth(40));
        EditorGUI.DrawRect(rOld, CDiffOld);
        EditorGUI.LabelField(rOld, "  — " + old, sDiffOld);

        GUILayout.Label("→", EditorStyles.miniLabel, GUILayout.Width(16));

        // New value
        var rNew = GUILayoutUtility.GetRect(new GUIContent(nw), sDiffNew, GUILayout.MinWidth(40));
        EditorGUI.DrawRect(rNew, CDiffNew);
        EditorGUI.LabelField(rNew, "  + " + nw, sDiffNew);
        GUI.backgroundColor = bgPrev;

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(2);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Persistence
    // ═══════════════════════════════════════════════════════════════════════

    private void LoadAll()
    {
        asset = Resources.Load<FlowTweenSettings>(ResourcePath);
        if (asset != null)
        {
            defTransition   = asset.defaultTransition;
            defEase         = asset.defaultEase;
            globalTimeScale = asset.globalTimeScale;
            killOnUnload    = asset.killOnSceneUnload;
            autoKillOrphans = asset.autoKillOrphans;
            prewarmTweens   = asset.prewarmTweens;
            prewarmSeqs     = asset.prewarmSequences;
            shrinkInterval  = asset.shrinkInterval;
            shrinkPercent   = asset.shrinkPercent;
            minPoolSize     = asset.minPoolSize;
        }

        refreshRate       = EditorPrefs.GetFloat(PrefRefreshRate,   0.05f);
        density           = EditorPrefs.GetInt  (PrefDensity,       1);
        showCurvePreviews = EditorPrefs.GetBool (PrefCurvePreviews, true);
        showSparklines    = EditorPrefs.GetBool (PrefSparklines,    true);
        showTsSliders     = EditorPrefs.GetBool (PrefTsSliders,     true);
        confirmKill       = EditorPrefs.GetBool (PrefConfirmKill,   true);
        enableGraveyard   = EditorPrefs.GetBool (PrefGraveyard,     true);
        enableEventLog    = EditorPrefs.GetBool (PrefEventLog,      true);
        hlNearComplete    = EditorPrefs.GetBool (PrefHlNear,        true);
        slowModeThreshold = EditorPrefs.GetInt  (PrefSlowThresh,    300);
        maxVisibleCards   = EditorPrefs.GetInt  (PrefMaxCards,      200);
    }

    private void SnapshotSaved()
    {
        savedTransition = defTransition;
        savedEase       = defEase;
        savedGts        = globalTimeScale;
        savedKillUnload = killOnUnload;
        savedAutoKill   = autoKillOrphans;
        savedPrewarmT   = prewarmTweens;
        savedPrewarmS   = prewarmSeqs;
        savedShrinkI    = shrinkInterval;
        savedShrinkP    = shrinkPercent;
        savedMinPool    = minPoolSize;
    }

    private void SaveAll()
    {
        EnsureAsset();

        asset.defaultTransition = defTransition;
        asset.defaultEase       = defEase;
        asset.globalTimeScale   = globalTimeScale;
        asset.killOnSceneUnload = killOnUnload;
        asset.autoKillOrphans   = autoKillOrphans;
        asset.prewarmTweens     = prewarmTweens;
        asset.prewarmSequences  = prewarmSeqs;
        asset.shrinkInterval    = shrinkInterval;
        asset.shrinkPercent     = shrinkPercent;
        asset.minPoolSize       = minPoolSize;

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        EditorPrefs.SetFloat(PrefRefreshRate,   refreshRate);
        EditorPrefs.SetInt  (PrefDensity,       density);
        EditorPrefs.SetBool (PrefCurvePreviews, showCurvePreviews);
        EditorPrefs.SetBool (PrefSparklines,    showSparklines);
        EditorPrefs.SetBool (PrefTsSliders,     showTsSliders);
        EditorPrefs.SetBool (PrefConfirmKill,   confirmKill);
        EditorPrefs.SetBool (PrefGraveyard,     enableGraveyard);
        EditorPrefs.SetBool (PrefEventLog,      enableEventLog);
        EditorPrefs.SetBool (PrefHlNear,        hlNearComplete);
        EditorPrefs.SetInt  (PrefSlowThresh,    slowModeThreshold);
        EditorPrefs.SetInt  (PrefMaxCards,      maxVisibleCards);

        // Push to live runtime immediately
        if (Application.isPlaying)
        {
            FlowTween.ApplySettings(asset);
            appliedThisSession = true;
        }

        SnapshotSaved();
        dirty = false;
        Repaint();
        Debug.Log("[FlowTween] Settings saved → " + AssetPath);
    }

    private void EnsureAsset()
    {
        if (asset != null) return;
        asset = Resources.Load<FlowTweenSettings>(ResourcePath);
        if (asset != null) return;

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        asset = ScriptableObject.CreateInstance<FlowTweenSettings>();
        AssetDatabase.CreateAsset(asset, AssetPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[FlowTween] Created settings asset → " + AssetPath);
    }

    private void ResetEditorPrefsToDefaults()
    {
        refreshRate = 0.05f; density = 1; showCurvePreviews = true;
        showSparklines = true; showTsSliders = true; confirmKill = true;
        enableGraveyard = true; enableEventLog = true; hlNearComplete = true;
        slowModeThreshold = 300; maxVisibleCards = 200;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Presets
    // ═══════════════════════════════════════════════════════════════════════

    private void ApplyPreset(int idx)
    {
        var p = Presets[idx];
        defTransition   = p.trans;    defEase        = p.ease;
        prewarmTweens   = p.prewarmT; prewarmSeqs    = p.prewarmS;
        minPoolSize     = p.minPool;  shrinkInterval = p.shrinkI;
        shrinkPercent   = p.shrinkP;  globalTimeScale = p.gts;
        activePresetIdx = idx;
        RebuildCurve();
        MarkDirty();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Import / Export JSON
    // ═══════════════════════════════════════════════════════════════════════

    [System.Serializable]
    private class SettingsJson
    {
        public string version;
        public string defaultTransition;
        public string defaultEase;
        public float  globalTimeScale;
        public bool   killOnSceneUnload;
        public bool   autoKillOrphans;
        public int    prewarmTweens;
        public int    prewarmSequences;
        public float  shrinkInterval;
        public float  shrinkPercent;
        public int    minPoolSize;
    }

    private void ExportJSON()
    {
        string path = EditorUtility.SaveFilePanel("Export FlowTween Settings", "", "FlowTweenSettings", "json");
        if (string.IsNullOrEmpty(path)) return;

        var m = new SettingsJson
        {
            version           = Version,
            defaultTransition = defTransition.ToString(),
            defaultEase       = defEase.ToString(),
            globalTimeScale   = globalTimeScale,
            killOnSceneUnload = killOnUnload,
            autoKillOrphans   = autoKillOrphans,
            prewarmTweens     = prewarmTweens,
            prewarmSequences  = prewarmSeqs,
            shrinkInterval    = shrinkInterval,
            shrinkPercent     = shrinkPercent,
            minPoolSize       = minPoolSize,
        };
        File.WriteAllText(path, JsonUtility.ToJson(m, true));
        Debug.Log("[FlowTween] Settings exported → " + path);
    }

    private void ImportJSON()
    {
        string path = EditorUtility.OpenFilePanel("Import FlowTween Settings", "", "json");
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        try
        {
            var m = JsonUtility.FromJson<SettingsJson>(File.ReadAllText(path));
            if (System.Enum.TryParse(m.defaultTransition, out Tween.TransitionType t)) defTransition   = t;
            if (System.Enum.TryParse(m.defaultEase,       out Tween.EaseType e))       defEase         = e;
            globalTimeScale = m.globalTimeScale;
            killOnUnload    = m.killOnSceneUnload;
            autoKillOrphans = m.autoKillOrphans;
            prewarmTweens   = m.prewarmTweens;
            prewarmSeqs     = m.prewarmSequences;
            shrinkInterval  = m.shrinkInterval;
            shrinkPercent   = m.shrinkPercent;
            minPoolSize     = m.minPoolSize;
            activePresetIdx = -1;
            RebuildCurve();
            MarkDirty();
            Debug.Log("[FlowTween] Settings imported ← " + path);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[FlowTween] Import failed: " + ex.Message);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Curve Cache
    // ═══════════════════════════════════════════════════════════════════════

    private void RebuildCurve()
    {
        string key = $"{defTransition}_{defEase}";
        if (key == curveKey && curvePoints != null) return;
        curveKey    = key;
        curvePoints = new float[CurveSamples];
        for (int i = 0; i < CurveSamples; i++)
            curvePoints[i] = EaseMath.Evaluate(i / (float)(CurveSamples - 1), defTransition, defEase);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Drawing Helpers
    // ═══════════════════════════════════════════════════════════════════════

    private void SectionTitle(string t, string sub)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(12);
        GUILayout.BeginVertical();
        GUILayout.Label(t, sHeader);
        if (!string.IsNullOrEmpty(sub))
        {
            var pc = GUI.color; GUI.color = new Color(1,1,1,0.45f);
            GUILayout.Label(sub, EditorStyles.wordWrappedMiniLabel);
            GUI.color = pc;
        }
        GUILayout.Space(4);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void SubHeader(string label)
    {
        GUILayout.Space(4);
        Rect r = GUILayoutUtility.GetRect(1, 22, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, CSubH);
        EditorGUI.DrawRect(new Rect(r.x, r.y, 3, r.height), CAccent);
        EditorGUI.LabelField(new Rect(r.x + 10, r.y + 1, r.width - 10, r.height), label, sSubHeader);
        GUILayout.Space(4);
    }

    private void BeginCard()
    {
        EditorGUILayout.BeginVertical("HelpBox");
        GUILayout.Space(2);
    }

    private void EndCard()
    {
        GUILayout.Space(2);
        EditorGUILayout.EndVertical();
        GUILayout.Space(4);
    }

    private void HRule(Color c, float h = 1f)
    {
        EditorGUI.DrawRect(GUILayoutUtility.GetRect(1, h, GUILayout.ExpandWidth(true)), c);
    }

    private void StatusRow(string icon, string msg, Color col)
    {
        GUILayout.BeginHorizontal();
        var pc = GUI.color; GUI.color = col;
        GUILayout.Label(icon + "  " + msg, EditorStyles.miniLabel, GUILayout.Height(18));
        GUI.color = pc;
        GUILayout.EndHorizontal();
    }

    private void KV(string key, string val)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(key + ":", sLabelBold, GUILayout.Width(120));
        GUILayout.Label(val, EditorStyles.wordWrappedLabel);
        GUILayout.EndHorizontal();
    }

    private void DrawCode(string code)
    {
        EditorGUILayout.TextArea(code, sCode);
    }

    private void Pill(string label, Color bg, Color fg)
    {
        var bgPrev = GUI.backgroundColor;
        GUI.backgroundColor = bg;
        var s = new GUIStyle(EditorStyles.miniButton)
        { normal = { textColor = fg }, fontSize = 10, padding = new RectOffset(6,6,1,1), fixedHeight = 18 };
        GUILayout.Label(label, s);
        GUI.backgroundColor = bgPrev;
    }

    private void PillInline(string label, Color bg, Color fg)
    {
        GUILayout.Label(label, new GUIStyle(EditorStyles.miniLabel)
        { normal = { textColor = fg, background = Tex(bg) }, padding = new RectOffset(6,6,1,1) });
    }

    private bool SmallBtn(string label, Color bg)
    {
        var prev = GUI.backgroundColor;
        GUI.backgroundColor = bg;
        bool clicked = GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Height(22));
        GUI.backgroundColor = prev;
        return clicked;
    }

    private void DangerBtn(string label, System.Action action)
    {
        var prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.60f, 0.15f, 0.15f);
        if (GUILayout.Button(label, GUILayout.Height(24))) action?.Invoke();
        GUI.backgroundColor = prev;
    }

    private void SafeBtn(string label, System.Action action)
    {
        var prev = GUI.backgroundColor;
        GUI.backgroundColor = CAccentDim;
        if (GUILayout.Button(label, GUILayout.Height(24))) action?.Invoke();
        GUI.backgroundColor = prev;
    }

    private static GUIContent Tip(string l, string t) => new GUIContent(l, t);
    private static bool Tog(string l, string t, bool v) => EditorGUILayout.Toggle(new GUIContent(l, t), v);
    private void MarkDirty() { dirty = true; Repaint(); }

    /// <summary>Returns a cached 1×1 Texture2D for the given colour. Never leaks.</summary>
    private Texture2D Tex(Color col)
    {
        if (!texCache.TryGetValue(col, out var tex) || tex == null)
        {
            tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            texCache[col] = tex;
        }
        return tex;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Style Initialisation  (invalidated on domain reload)
    // ═══════════════════════════════════════════════════════════════════════

    private void EnsureStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        sHeader = new GUIStyle(EditorStyles.boldLabel)
        { fontSize = 15, normal = { textColor = new Color(0.92f, 0.92f, 0.92f) } };

        sSubHeader = new GUIStyle(EditorStyles.boldLabel)
        { fontSize = 11, normal = { textColor = CAccent } };

        sTab = new GUIStyle(EditorStyles.toolbarButton)
        { fontSize = 11, padding = new RectOffset(12,12,0,0), normal = { textColor = new Color(0.72f,0.72f,0.72f) } };

        sTabActive = new GUIStyle(sTab)
        { fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };

        sLabelBold = new GUIStyle(EditorStyles.label)
        { fontStyle = FontStyle.Bold };

        sPresetBtn = new GUIStyle(EditorStyles.miniButton)
        { fontSize = 10, padding = new RectOffset(9,9,3,3) };

        sPresetBtnActive = new GUIStyle(sPresetBtn)
        { fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };

        sCode = new GUIStyle(EditorStyles.textArea)
        {
            fontSize = 10, wordWrap = false, richText = false,
            normal   = { textColor = new Color(0.78f, 0.78f, 0.78f) },
            font     = EditorStyles.miniLabel.font,
        };

        sVersionLabel = new GUIStyle(EditorStyles.miniLabel)
        { normal = { textColor = new Color(0.45f, 0.45f, 0.45f) }, alignment = TextAnchor.MiddleRight };

        sDiffOld = new GUIStyle(EditorStyles.miniLabel)
        { normal = { textColor = new Color(0.95f, 0.55f, 0.50f) }, fontStyle = FontStyle.Bold };

        sDiffNew = new GUIStyle(EditorStyles.miniLabel)
        { normal = { textColor = new Color(0.50f, 0.92f, 0.60f) }, fontStyle = FontStyle.Bold };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  FlowTweenSettings  ScriptableObject
//  Path: Assets/Resources/FlowTweenSettings.asset
//  Loaded at runtime: Resources.Load<FlowTweenSettings>("FlowTweenSettings")
// ═══════════════════════════════════════════════════════════════════════════
