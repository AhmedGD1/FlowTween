using UnityEngine;
using UnityEditor;
using FlT;
using System.Collections.Generic;
using System.Text;
using System;
using System.Text.RegularExpressions;

/// <summary>
/// FlowTween Debugger — comprehensive runtime inspector.
/// Open:  Window ▶ Analysis ▶ FlowTween Debugger  or  Alt+Shift+T
///
/// Performance: virtual scrolling renders only visible cards regardless of tween count.
/// Tabs: Update | Fixed | Sequences | Groups | Event Log | Ease Ref | Profiler | Settings
/// </summary>
public class FlowTweenDebugWindow : EditorWindow
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  Enums & Constants
    // ═══════════════════════════════════════════════════════════════════════════

    private enum Tab         { Tweens, FixedTweens, Sequences, Groups, Pool, EventLog, EaseRef, Profiler, Settings }
    private enum SortMode    { Default, Name, Progress, Duration, Group, TimeScale, Remaining }
    private enum DensityMode { Compact, Comfortable, Spacious }
    public  enum EventKind   { Start, Complete, Kill, Loop, Pause, Resume, Warning }

    private const int   HistorySize        = 128;
    private const int   ProfilerHistory    = 256;
    private const int   GraveyardMax       = 50;
    private const int   EventLogMax        = 200;
    private const float ProfilerSampleRate = 0.1f;
    private const int   CurveSamples       = 64;
    private const float NearCompleteThresh = 0.85f; // progress at which bar turns orange

    // Card height estimates for virtual scrolling (collapsed state)
    private float CardEstH     => density == DensityMode.Compact ? 56f  : density == DensityMode.Comfortable ? 74f  : 96f;
    private float ProgressBarH => density == DensityMode.Compact ? 11f  : density == DensityMode.Comfortable ? 14f  : 18f;
    private float CurvePreviewH=> density == DensityMode.Compact ? 32f  : density == DensityMode.Comfortable ? 44f  : 60f;
    private float SparklineH   => density == DensityMode.Compact ? 22f  : density == DensityMode.Comfortable ? 30f  : 40f;
    private float CardSpacing  => density == DensityMode.Compact ?  2f  : density == DensityMode.Comfortable ?  4f  :  8f;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Inner Types
    // ═══════════════════════════════════════════════════════════════════════════

    private struct EventEntry
    {
        public float     time;
        public EventKind kind;
        public string    targetName, detail, group;
        public object    id;
    }

    private struct GraveyardEntry
    {
        public float  killedAt, duration, elapsedAtDeath;
        public string targetName, cause, group;
        public object id;
    }

    private struct TweenWarning
    {
        public string message;
        public Color  color;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  State
    // ═══════════════════════════════════════════════════════════════════════════

    private Tab         currentTab = Tab.Tweens;
    private SortMode    sortMode   = SortMode.Default;
    private bool        sortAsc    = true;
    private DensityMode density    = DensityMode.Comfortable;

    private Vector2 tweenScroll, fixedScroll, sequenceScroll;
    private Vector2 groupScroll, eventLogScroll, easeRefScroll;
    private Vector2 profilerScroll, settingsScroll, graveyardScroll;

    // Filters
    private string searchFilter  = "";
    private bool   useRegex      = false;
    private bool   showPaused    = true;
    private bool   showPlaying   = true;
    private bool   showCompleted = false;
    private bool   showWarnings  = true;
    private string groupFilter   = "";

    // Regex
    private Regex  compiledRegex;
    private bool   regexValid = true;

    // Card state
    private readonly HashSet<int> expandedCards = new();
    private readonly HashSet<int> pinnedTweens  = new();
    private readonly HashSet<int> selectedTweens= new();  // multi-select
    private bool multiSelectMode = false;

    // Warning cache: hash → (frameStamp, list)
    // Only recomputed once per refresh cycle, not once per OnGUI call.
    private readonly Dictionary<int, List<TweenWarning>> warningCache     = new();
    private int warningCacheFrame = -1;

    // Filtered + sorted list cache — only rebuilt when dirty
    private readonly List<Tween> filteredIdle  = new();
    private readonly List<Tween> filteredFixed = new();
    private bool filteredDirty = true;
    private int  lastIdleCount = -1, lastFixedCount = -1;
    private string lastSearch = "", lastGroupFilter = "";
    private bool lastShowPaused, lastShowPlaying, lastShowCompleted, lastShowWarnings;
    private SortMode lastSortMode; private bool lastSortAsc;

    // Progress history (only sampled at profiler rate, not every frame)
    private readonly Dictionary<int, float[]> tweenHistory     = new();
    private readonly Dictionary<int, int>      tweenHistoryHead = new();
    private readonly Dictionary<int, float>    tweenPeakProg    = new();

    // Event log
    private readonly List<EventEntry> eventLog = new();
    private bool   eventLogAutoScroll      = true;
    private bool   eventLogShowStart       = true;
    private bool   eventLogShowComplete    = true;
    private bool   eventLogShowKill        = true;
    private bool   eventLogShowLoop        = true;
    private bool   eventLogShowWarning     = true;
    private bool   eventLogShowPauseResume = false;
    private string eventLogSearch          = "";

    // Graveyard
    private readonly List<GraveyardEntry> graveyard = new();
    private bool showGraveyard = false;

    // Pool Inspector
    private Vector2 poolScroll;
    private readonly float[] tweenPoolSizeHistory    = new float[ProfilerHistory];
    private readonly float[] sequencePoolSizeHistory = new float[ProfilerHistory];
    private readonly float[] tweenPoolHitHistory     = new float[ProfilerHistory];
    private readonly float[] seqPoolHitHistory       = new float[ProfilerHistory];
    private int    poolProfilerHead;
    private float  peakTweenPoolSize, peakSeqPoolSize;
    private float  poolSampleAccum;
    private const float PoolSampleRate = 0.15f;

    // Freeze snapshot — captures a moment in time for side-by-side comparison
    private List<(string name, float progress, float elapsed, float duration, string group)> _frozenSnapshot = null;
    private float _frozenAt = -1f;
    private bool  showFreezePanel = false;

    // Profiler
    private readonly float[] tweenCountHistory    = new float[ProfilerHistory];
    private readonly float[] fixedCountHistory    = new float[ProfilerHistory];
    private readonly float[] sequenceCountHistory = new float[ProfilerHistory];
    private readonly float[] fpsHistory           = new float[ProfilerHistory];
    private int    profilerHead;
    private double lastProfilerSample;
    private float  peakTweenCount, peakFixedCount, peakSequenceCount, peakFps;
    private bool   showPeakWatermarks = true;
    private bool   profilerPaused     = false;

    // Ease gallery
    private float easeGalleryT       = 0f;
    private bool  easeGalleryAnimate = true;

    // Settings
    private float  refreshRate           = 0.05f;
    private bool   slowModeOnHighCount    = true;   // auto-slow repaint above threshold
    private int    slowModeThreshold      = 300;    // tween count that triggers slow mode
    private bool   tooManyTweensNow       = false;  // set each Update
    private double lastRepaintTime;
    private bool   showCurvePreviews     = true;
    private bool   showSparklines        = true;
    private bool   showTimeScaleSliders  = true;
    private bool   confirmKill           = true;
    private bool   enableGraveyard       = true;
    private bool   enableEventLog        = true;
    private bool   highlightNearComplete = true;
    private int    maxVisibleCards       = 200;  // virtual scroll cap (0 = unlimited)

    // Snapshots (reused each repaint)
    private readonly List<Tween>    tweenSnapshot    = new();
    private readonly List<Sequence> sequenceSnapshot = new();

    // Cached virtual-scroll split lists — reused every draw, no per-frame alloc
    private readonly List<Tween> _pinnedListCache = new();
    private readonly List<Tween> _normalListCache = new();

    // Per-card measured heights — populated on Repaint, used for accurate virtual scroll
    private readonly Dictionary<int, float> _measuredCardH = new();

    // Curve cache
    private readonly Dictionary<string, float[]> curveCache = new();

    // Styles (lazy)
    private GUIStyle headerStyle, cardStyle, labelBoldStyle, tagStyle;
    private GUIStyle miniCenteredLabel, sectionHeaderStyle, expandButtonStyle;
    private GUIStyle selectedCardStyle;
    private bool     stylesInitialized;

    // Colors
    private static readonly Color ColPlaying      = new(0.27f, 0.82f, 0.50f);
    private static readonly Color ColPaused       = new(0.95f, 0.77f, 0.20f);
    private static readonly Color ColCompleted    = new(0.55f, 0.55f, 0.55f);
    private static readonly Color ColBar          = new(0.28f, 0.62f, 0.98f);
    private static readonly Color ColBarNear      = new(0.98f, 0.65f, 0.15f);  // near-complete bar
    private static readonly Color ColBarBg        = new(0.15f, 0.15f, 0.15f);
    private static readonly Color ColHeader       = new(0.13f, 0.13f, 0.13f);
    private static readonly Color ColDivider      = new(0.28f, 0.28f, 0.28f);
    private static readonly Color ColSparkline    = new(0.40f, 0.75f, 1.00f);
    private static readonly Color ColCurve        = new(0.98f, 0.70f, 0.25f);
    private static readonly Color ColCurveBg      = new(0.11f, 0.11f, 0.11f);
    private static readonly Color ColSeqBar       = new(0.55f, 0.40f, 0.90f);
    private static readonly Color ColFpsBad       = new(0.95f, 0.30f, 0.25f);
    private static readonly Color ColFpsOk        = new(0.95f, 0.70f, 0.20f);
    private static readonly Color ColFpsGood      = new(0.27f, 0.82f, 0.50f);
    private static readonly Color ColGrid         = new(0.20f, 0.20f, 0.20f);
    private static readonly Color ColWarn         = new(0.98f, 0.45f, 0.15f);
    private static readonly Color ColPinned       = new(0.98f, 0.85f, 0.20f);
    private static readonly Color ColSelected     = new(0.28f, 0.50f, 0.85f, 0.35f);
    private static readonly Color ColWatermark    = new(1.00f, 0.30f, 0.30f, 0.50f);
    private static readonly Color ColAccent       = new(0.28f, 0.62f, 0.98f);

    private static readonly Color[] GroupColors =
    {
        new(0.95f,0.45f,0.30f), new(0.30f,0.75f,0.90f), new(0.80f,0.55f,0.95f),
        new(0.40f,0.90f,0.60f), new(0.95f,0.80f,0.25f), new(0.90f,0.40f,0.65f),
        new(0.45f,0.85f,0.90f), new(0.70f,0.90f,0.40f),
    };
    private readonly Dictionary<string, Color> groupColorMap = new();

    // Reflection
    // ── Reflection cache — works with original unmodified Tween.cs / Sequence.cs ──
    // Every Dbg* access goes through R_* helpers below, so this file compiles and
    // works correctly without requiring any changes to the library source files.
    private static System.Reflection.FieldInfo    _transField, _easeField, _curveField;
    private static System.Reflection.FieldInfo    _interpField;
    private static System.Reflection.FieldInfo    _pendingTweenField;
    private static System.Reflection.FieldInfo    _poppedField, _startedField, _relativeField;
    private static System.Reflection.FieldInfo    _useUnscaledTimeField, _useSpeedBaseField;
    private static System.Reflection.FieldInfo    _loopsField, _currentLoopField;
    private static System.Reflection.FieldInfo    _playbackDirectionField, _delayElapsedField;
    private static System.Reflection.PropertyInfo _seqElapsedProp;
    private static System.Reflection.FieldInfo    _seqStepsField, _seqCallbacksField;
    private static System.Reflection.FieldInfo    _seqActiveStepIndexField;
    private static System.Reflection.FieldInfo    _seqLoopsField, _seqCurrentLoopField;
    private static System.Reflection.FieldInfo    _stepTweenField, _stepStartTimeField;

    private static void CacheReflection()
    {
        var pf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var pb = System.Reflection.BindingFlags.Public    | System.Reflection.BindingFlags.Instance;
        var tf = typeof(Tween);
        _transField             = tf.GetField("transition",        pf);
        _easeField              = tf.GetField("ease",              pf);
        _curveField             = tf.GetField("customCurve",       pf);
        _interpField            = tf.GetField("interpolator",      pf);
        _pendingTweenField      = tf.GetField("pendingTween",      pf);
        _poppedField            = tf.GetField("popped",            pf);
        _startedField           = tf.GetField("started",           pf);
        _relativeField          = tf.GetField("relative",          pf);
        _useUnscaledTimeField   = tf.GetField("useUnscaledTime",   pf);
        _useSpeedBaseField      = tf.GetField("useSpeedBase",      pf);
        _loopsField             = tf.GetField("loops",             pf);
        _currentLoopField       = tf.GetField("currentLoop",       pf);
        _playbackDirectionField = tf.GetField("playbackDirection", pf);
        _delayElapsedField      = tf.GetField("delayElapsed",      pf);

        var sf = typeof(Sequence);
        _seqElapsedProp          = sf.GetProperty("Elapsed");
        _seqStepsField           = sf.GetField("steps",            pf);
        _seqCallbacksField       = sf.GetField("callbacks",        pf);
        _seqActiveStepIndexField = sf.GetField("activeStepIndex",  pf);
        _seqLoopsField           = sf.GetField("loops",            pf);
        _seqCurrentLoopField     = sf.GetField("currentLoop",      pf);

        // SequenceStep nested type — may be public or internal depending on version
        var stepType = sf.GetNestedType("SequenceStep", pf | pb);
        if (stepType != null)
        {
            _stepTweenField     = stepType.GetField("tween",     pb);
            _stepStartTimeField = stepType.GetField("startTime", pb);
        }
    }

    // ── Tween reflection accessors (R_ prefix) ────────────────────────────────
    private string R_InterpolatorTypeName(Tween t)
    {
        var interp = _interpField?.GetValue(t);
        return interp != null ? interp.GetType().Name.Replace("Interpolator", "") : "";
    }
    private object  R_Interpolator(Tween t)       => _interpField?.GetValue(t);
    private Tween   R_PendingTween(Tween t)        => _pendingTweenField?.GetValue(t) as Tween;
    private bool    R_HasPending(Tween t)          => R_PendingTween(t) != null;
    private bool    R_IsPopped(Tween t)            => _poppedField           != null && (bool)_poppedField.GetValue(t);
    private bool    R_IsStarted(Tween t)           => _startedField          != null && (bool)_startedField.GetValue(t);
    private bool    R_IsRelative(Tween t)          => _relativeField         != null && (bool)_relativeField.GetValue(t);
    private bool    R_UseUnscaled(Tween t)         => _useUnscaledTimeField  != null && (bool)_useUnscaledTimeField.GetValue(t);
    private bool    R_UseSpeedBase(Tween t)        => _useSpeedBaseField     != null && (bool)_useSpeedBaseField.GetValue(t);
    private int     R_Loops(Tween t)               => _loopsField            != null ? (int)_loopsField.GetValue(t)             : 0;
    private int     R_CurrentLoop(Tween t)         => _currentLoopField      != null ? (int)_currentLoopField.GetValue(t)       : 0;
    private float   R_PlaybackDir(Tween t)         => _playbackDirectionField != null ? (float)_playbackDirectionField.GetValue(t) : 1f;
    private float   R_DelayElapsed(Tween t)        => _delayElapsedField     != null ? (float)_delayElapsedField.GetValue(t)    : 0f;

    // ── Sequence reflection accessors ─────────────────────────────────────────
    private System.Collections.IList R_SeqSteps(Sequence s)     => _seqStepsField?.GetValue(s)     as System.Collections.IList;
    private System.Collections.IList R_SeqCallbacks(Sequence s) => _seqCallbacksField?.GetValue(s) as System.Collections.IList;

    // Callbacks are stored as (float time, Action callback) value tuples — extract time via reflection
    private float R_CallbackTime(object cb)
    {
        if (cb == null) return 0f;
        var f = cb.GetType().GetField("Item1") ?? cb.GetType().GetField("time");
        return f != null ? (float)f.GetValue(cb) : 0f;
    }
    private int R_SeqActiveStep(Sequence s)  => _seqActiveStepIndexField != null ? (int)_seqActiveStepIndexField.GetValue(s) : 0;
    private int R_SeqLoops(Sequence s)       => _seqLoopsField       != null ? (int)_seqLoopsField.GetValue(s)       : 0;
    private int R_SeqCurrentLoop(Sequence s) => _seqCurrentLoopField != null ? (int)_seqCurrentLoopField.GetValue(s) : 0;
    private Tween R_StepTween(object step)      => _stepTweenField?.GetValue(step) as Tween;
    private float R_StepStartTime(object step)  => _stepStartTimeField != null ? (float)_stepStartTimeField.GetValue(step) : 0f;

    // Warned set (deduplicate event log entries)
    private readonly HashSet<int> warnedTweens = new();

    // Duplicate-property conflict detection
    private readonly Dictionary<(int, string), List<Tween>> _conflictMap = new();
    private readonly HashSet<int> _conflictingTweenHashes = new();
    private bool showConflictsOnly = false;

    // Per-group profiler history
    private readonly Dictionary<string, float[]> groupCountHistory = new();
    private readonly Dictionary<string, int>     groupHistoryHead  = new();
    private Vector2 groupProfilerScroll;
    private bool    showGroupProfiler = false;

    // ═══════════════════════════════════════════════════════════════════════════
    //  Window Lifecycle
    // ═══════════════════════════════════════════════════════════════════════════

    [MenuItem("Window/Analysis/FlowTween Debugger #&t")]
    public static void Open()
    {
        var w = GetWindow<FlowTweenDebugWindow>("FlowTween Debugger");
        w.minSize = new Vector2(480, 360);
        w.titleContent = new GUIContent("⚡ FlowTween",
            EditorGUIUtility.IconContent("d_UnityEditor.ProfilerWindow").image);
    }

    private void OnEnable()
    {
        stylesInitialized = false;
        CacheReflection();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        tweenHistory.Clear(); tweenHistoryHead.Clear();
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode) return;
        graveyard.Clear(); eventLog.Clear();
        tweenHistory.Clear(); tweenHistoryHead.Clear(); tweenPeakProg.Clear();
        warningCache.Clear(); warningCacheFrame = -1;
        filteredDirty = true;
        peakTweenCount = peakFixedCount = peakSequenceCount = peakFps = 0f;
        Array.Clear(tweenCountHistory,    0, ProfilerHistory);
        Array.Clear(fixedCountHistory,    0, ProfilerHistory);
        Array.Clear(sequenceCountHistory, 0, ProfilerHistory);
        Array.Clear(fpsHistory,           0, ProfilerHistory);
        profilerHead = 0;
        // Pool history reset
        peakTweenPoolSize = peakSeqPoolSize = 0f;
        Array.Clear(tweenPoolSizeHistory,    0, ProfilerHistory);
        Array.Clear(sequencePoolSizeHistory, 0, ProfilerHistory);
        Array.Clear(tweenPoolHitHistory,     0, ProfilerHistory);
        Array.Clear(seqPoolHitHistory,       0, ProfilerHistory);
        poolProfilerHead = 0;
        poolSampleAccum  = 0f;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        double now = EditorApplication.timeSinceStartup;

        // On tween/sequence tabs we need fast updates; other tabs can be slow.
        bool isTweenTab = currentTab == Tab.Tweens || currentTab == Tab.FixedTweens
                       || currentTab == Tab.Sequences || currentTab == Tab.Groups;
        // Auto slow-mode: when there are many tweens, cap repaint rate to protect FPS
        tooManyTweensNow  = slowModeOnHighCount && FlowTween.ActiveCount >= slowModeThreshold;
        bool tooManyTweens = tooManyTweensNow;
        float effectiveRate = isTweenTab
            ? (tooManyTweens ? Mathf.Max(refreshRate, 0.15f) : refreshRate)
            : Mathf.Max(refreshRate, 0.25f);

        if (now - lastRepaintTime >= effectiveRate)
        {
            lastRepaintTime = now;
            if (easeGalleryAnimate) easeGalleryT = (float)(now % 2.0) * 0.5f;
            warningCacheFrame = -1;
            // Only mark filter dirty when tween counts actually changed —
            // avoids iterating 1000 tweens on every repaint when counts are stable.
            int idleNow  = FlowTween.ActiveIdleCount;
            int fixedNow = FlowTween.ActiveFixedCount;
            if (idleNow != lastIdleCount || fixedNow != lastFixedCount)
                filteredDirty = true;
            Repaint();
        }

        if (!profilerPaused && now - lastProfilerSample >= ProfilerSampleRate)
        {
            lastProfilerSample = now;
            SampleProfiler();
            // Record sparkline history only when on a tween tab.
            if (isTweenTab)
            {
                FlowTween.ForEachActiveTween(t      => RecordHistory(t.GetHashCode(), t.Progress));
                FlowTween.ForEachActiveFixedTween(t => RecordHistory(t.GetHashCode(), t.Progress));
            }
        }

        // Pool history sampled independently (slightly slower rate is fine)
        poolSampleAccum += (float)(now - lastRepaintTime + refreshRate); // approximate dt
        if (poolSampleAccum >= PoolSampleRate)
        {
            poolSampleAccum = 0f;
            SamplePoolHistory();
            RebuildConflictMap();
            SampleGroupHistory();
        }
    }

    private void SampleProfiler()
    {
        int idle = FlowTween.ActiveIdleCount, fx = FlowTween.ActiveFixedCount, sq = FlowTween.ActiveSequenceCount;
        float fps = Time.deltaTime > 0f ? 1f / Time.deltaTime : 0f;
        tweenCountHistory[profilerHead]    = idle;
        fixedCountHistory[profilerHead]    = fx;
        sequenceCountHistory[profilerHead] = sq;
        fpsHistory[profilerHead]           = fps;
        profilerHead = (profilerHead + 1) % ProfilerHistory;
        if (idle > peakTweenCount)    peakTweenCount    = idle;
        if (fx   > peakFixedCount)    peakFixedCount    = fx;
        if (sq   > peakSequenceCount) peakSequenceCount = sq;
        if (fps  > peakFps)           peakFps           = fps;
    }

    private void SamplePoolHistory()
    {
        float tp = FlowTween.TweenPoolSize;
        float sp = FlowTween.SequencePoolSize;
        tweenPoolSizeHistory[poolProfilerHead]    = tp;
        sequencePoolSizeHistory[poolProfilerHead] = sp;
        tweenPoolHitHistory[poolProfilerHead]     = FlowTween.TweenPoolHitRate    >= 0f ? FlowTween.TweenPoolHitRate    * 100f : 0f;
        seqPoolHitHistory[poolProfilerHead]       = FlowTween.SequencePoolHitRate >= 0f ? FlowTween.SequencePoolHitRate * 100f : 0f;
        poolProfilerHead = (poolProfilerHead + 1) % ProfilerHistory;
        if (tp > peakTweenPoolSize) peakTweenPoolSize = tp;
        if (sp > peakSeqPoolSize)   peakSeqPoolSize   = sp;
    }

    // ── Conflict Detection ────────────────────────────────────────────────────
    // Detects multiple tweens driving the same property on the same object.

    private void RebuildConflictMap()
    {
        _conflictMap.Clear();
        _conflictingTweenHashes.Clear();

        void Scan(Tween t)
        {
            if (t.Target == null || !t.Target) return;
            string interpType = R_InterpolatorTypeName(t);
            if (string.IsNullOrEmpty(interpType)) return;

            var key = (t.Target.GetInstanceID(), interpType);
            if (!_conflictMap.TryGetValue(key, out var list))
            { list = new List<Tween>(); _conflictMap[key] = list; }
            list.Add(t);
        }

        FlowTween.ForEachActiveTween(Scan);
        FlowTween.ForEachActiveFixedTween(Scan);

        foreach (var kvp in _conflictMap)
            if (kvp.Value.Count > 1)
                foreach (var t in kvp.Value)
                    _conflictingTweenHashes.Add(t.GetHashCode());
    }

    // ── Group Profiler Sampling ───────────────────────────────────────────────

    private void SampleGroupHistory()
    {
        var current = new Dictionary<string, int>();

        void Count(Tween t)
        {
            string g = string.IsNullOrEmpty(t.Group) ? "(ungrouped)" : t.Group;
            current.TryGetValue(g, out int c);
            current[g] = c + 1;
        }

        FlowTween.ForEachActiveTween(Count);
        FlowTween.ForEachActiveFixedTween(Count);

        foreach (var kvp in current)
        {
            string g = kvp.Key;
            if (!groupCountHistory.ContainsKey(g))
            {
                groupCountHistory[g] = new float[ProfilerHistory];
                groupHistoryHead[g]  = 0;
            }
            int head = groupHistoryHead[g];
            groupCountHistory[g][head] = kvp.Value;
            groupHistoryHead[g] = (head + 1) % ProfilerHistory;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Filter / Sort Cache
    // ═══════════════════════════════════════════════════════════════════════════

    private void RebuildFilteredIfDirty()
    {
        bool filterParamsChanged =
            searchFilter != lastSearch || groupFilter != lastGroupFilter
            || showPaused != lastShowPaused || showPlaying != lastShowPlaying
            || showCompleted != lastShowCompleted || showWarnings != lastShowWarnings
            || sortMode != lastSortMode || sortAsc != lastSortAsc;

        bool countsChanged =
            FlowTween.ActiveIdleCount  != lastIdleCount
            || FlowTween.ActiveFixedCount != lastFixedCount;

        bool changed = filteredDirty || filterParamsChanged || countsChanged;
        if (!changed) return;

        lastSearch = searchFilter; lastGroupFilter = groupFilter;
        lastShowPaused = showPaused; lastShowPlaying = showPlaying;
        lastShowCompleted = showCompleted; lastShowWarnings = showWarnings;
        lastSortMode = sortMode; lastSortAsc = sortAsc;
        lastIdleCount = FlowTween.ActiveIdleCount;
        lastFixedCount = FlowTween.ActiveFixedCount;
        filteredDirty = false;

        // Only rebuild the list needed for the active tab to halve work.
        bool needIdle  = currentTab == Tab.Tweens  || currentTab == Tab.Groups;
        bool needFixed = currentTab == Tab.FixedTweens || currentTab == Tab.Groups;
        // Always rebuild both when filter params changed (user typed in search box etc.)
        if (filterParamsChanged) { needIdle = true; needFixed = true; }

        if (needIdle)
        {
            tweenSnapshot.Clear();
            FlowTween.ForEachActiveTween(t => tweenSnapshot.Add(t));
            filteredIdle.Clear();
            filteredIdle.AddRange(FilterList(tweenSnapshot));
            SortList(filteredIdle);
            CollectGroups(filteredIdle);
        }

        if (needFixed)
        {
            tweenSnapshot.Clear();
            FlowTween.ForEachActiveFixedTween(t => tweenSnapshot.Add(t));
            filteredFixed.Clear();
            filteredFixed.AddRange(FilterList(tweenSnapshot));
            SortList(filteredFixed);
            CollectGroups(filteredFixed);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Style Init
    // ═══════════════════════════════════════════════════════════════════════════

    private void InitStyles()
    {
        if (stylesInitialized) return;
        stylesInitialized = true;
        headerStyle        = new GUIStyle(EditorStyles.boldLabel)  { fontSize = 13, alignment = TextAnchor.MiddleLeft };
        cardStyle          = new GUIStyle("box")                   { padding = new RectOffset(10,10,7,7), margin = new RectOffset(5,5,3,3) };
        labelBoldStyle     = new GUIStyle(EditorStyles.label)      { fontStyle = FontStyle.Bold };
        tagStyle           = new GUIStyle(EditorStyles.miniLabel)  { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(6,6,2,2), fontStyle = FontStyle.Bold };
        miniCenteredLabel  = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 9 };
        sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)  { fontSize = 11 };
        expandButtonStyle  = new GUIStyle(EditorStyles.miniButton) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(2,2,1,1) };
        selectedCardStyle  = new GUIStyle("box")
        {
            padding  = new RectOffset(10,10,7,7),
            margin   = new RectOffset(5,5,3,3),
            normal   = { background = MakeTex(2, 2, ColSelected) },
        };
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var pix = new Color[w * h]; for (int i = 0; i < pix.Length; i++) pix[i] = col;
        var t = new Texture2D(w, h); t.SetPixels(pix); t.Apply(); return t;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Main GUI
    // ═══════════════════════════════════════════════════════════════════════════

    private void OnGUI()
    {
        InitStyles();
        if (!Application.isPlaying) { DrawNotPlaying(); return; }

        // Rebuild filter cache once per repaint if needed
        RebuildFilteredIfDirty();

        DrawHeader();
        DrawTabs();
        switch (currentTab)
        {
            case Tab.Tweens:      DrawFilterBar(); GUILayout.Space(2); DrawTweenListVirtual(false); break;
            case Tab.FixedTweens: DrawFilterBar(); GUILayout.Space(2); DrawTweenListVirtual(true);  break;
            case Tab.Sequences:   DrawFilterBar(); GUILayout.Space(2); DrawSequenceList();          break;
            case Tab.Groups:      DrawGroupInspector();   break;
            case Tab.Pool:        DrawPoolTab();          break;
            case Tab.EventLog:    DrawEventLogTab();      break;
            case Tab.EaseRef:     DrawEaseReferenceTab(); break;
            case Tab.Profiler:    DrawProfilerTab();      break;
            case Tab.Settings:    DrawSettingsTab();      break;
        }
    }

    private void DrawNotPlaying()
    {
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
        EditorGUILayout.HelpBox("Enter Play Mode to use the FlowTween Debugger.", MessageType.Info);
        GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
    }

    // ─── Header ───────────────────────────────────────────────────────────────

    private void DrawHeader()
    {
        EditorGUI.DrawRect(new Rect(0, 0, position.width, 38), ColHeader);
        EditorGUILayout.BeginHorizontal(GUILayout.Height(38));
        GUILayout.Space(10);
        GUILayout.Label("⚡ FlowTween Debugger", headerStyle, GUILayout.Height(38));
        GUILayout.FlexibleSpace();
        float fps = Time.deltaTime > 0f ? 1f / Time.deltaTime : 0f;
        Badge($"FPS {fps:0}",                fps >= 55 ? ColFpsGood : fps >= 30 ? ColFpsOk : ColFpsBad);
        Badge($"Idle {FlowTween.ActiveIdleCount}",    ColPlaying);
        Badge($"Fixed {FlowTween.ActiveFixedCount}",  ColPaused);
        Badge($"Seq {FlowTween.ActiveSequenceCount}", ColSeqBar);
        Badge($"∑ {FlowTween.ActiveCount}",           ColBar);
        // Pool badges
        Badge($"Pool⊕ {FlowTween.TweenPoolSize}", ColPoolTween);
        if (FlowTween.TweenPoolHitRate >= 0f)
        {
            float hr = FlowTween.TweenPoolHitRate;
            Badge($"{hr*100f:0}%hit", hr >= 0.8f ? ColPoolHit : hr >= 0.5f ? ColPaused : ColPoolMiss);
        }
        if (tooManyTweensNow) Badge("🐢 SLOW", ColWarn);
        GUILayout.Space(6);
        var prev = GUI.color; GUI.color = new Color(0.8f, 0.8f, 0.8f);
        if (GUILayout.Button("½×", EditorStyles.miniButtonLeft,  GUILayout.Width(24), GUILayout.Height(20))) Time.timeScale = 0.5f;
        if (GUILayout.Button("1×", EditorStyles.miniButtonMid,   GUILayout.Width(24), GUILayout.Height(20))) Time.timeScale = 1.0f;
        if (GUILayout.Button("2×", EditorStyles.miniButtonRight, GUILayout.Width(24), GUILayout.Height(20))) Time.timeScale = 2.0f;
        GUI.color = prev;
        GUILayout.Space(4);
        EditorGUILayout.EndHorizontal();
        EditorGUI.DrawRect(new Rect(0, 37, position.width, 2), ColAccent);
    }

    private void Badge(string label, Color color)
    {
        var p = GUI.color; GUI.color = color;
        GUILayout.Label(label, EditorStyles.miniButtonMid, GUILayout.Height(20));
        GUI.color = p;
    }

    // ─── Tabs ─────────────────────────────────────────────────────────────────

    private void DrawTabs()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        DrawTabBtn(Tab.Tweens,      "Update");
        DrawTabBtn(Tab.FixedTweens, "Fixed");
        DrawTabBtn(Tab.Sequences,   "Sequences");
        DrawTabBtn(Tab.Groups,      "Groups");
        DrawTabBtn(Tab.Pool,        $"Pool");
        DrawTabBtn(Tab.EventLog,    $"Log({eventLog.Count})");
        DrawTabBtn(Tab.EaseRef,     "Ease Ref");
        DrawTabBtn(Tab.Profiler,    "Profiler");
        GUILayout.FlexibleSpace();
        DrawTabBtn(Tab.Settings, "⚙");
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTabBtn(Tab tab, string label)
    {
        var p = GUI.backgroundColor;
        if (currentTab == tab) GUI.backgroundColor = new Color(0.38f, 0.68f, 1f);
        if (GUILayout.Button(label, EditorStyles.toolbarButton)) currentTab = tab;
        GUI.backgroundColor = p;
    }

    // ─── Filter Bar ───────────────────────────────────────────────────────────

    private void DrawFilterBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // Regex toggle
        var rp = GUI.backgroundColor;
        if (useRegex) GUI.backgroundColor = regexValid ? new Color(0.5f,0.9f,0.5f) : new Color(1f,0.5f,0.5f);
        if (GUILayout.Button(".*", EditorStyles.toolbarButton, GUILayout.Width(22))) { useRegex = !useRegex; filteredDirty = true; }
        GUI.backgroundColor = rp;

        EditorGUI.BeginChangeCheck();
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(110));
        if (EditorGUI.EndChangeCheck()) { filteredDirty = true; if (useRegex) CompileRegex(); }
        if (!string.IsNullOrEmpty(searchFilter) && GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(18)))
        { searchFilter = ""; compiledRegex = null; filteredDirty = true; }

        GUILayout.Label("Grp:", EditorStyles.miniLabel, GUILayout.Width(26));
        EditorGUI.BeginChangeCheck();
        groupFilter = EditorGUILayout.TextField(groupFilter, EditorStyles.toolbarSearchField, GUILayout.Width(70));
        if (EditorGUI.EndChangeCheck()) filteredDirty = true;

        GUILayout.Space(4);
        EditorGUI.BeginChangeCheck();
        showPlaying   = GUILayout.Toggle(showPlaying,  "▶",  EditorStyles.toolbarButton, GUILayout.Width(24));
        showPaused    = GUILayout.Toggle(showPaused,   "⏸", EditorStyles.toolbarButton, GUILayout.Width(24));
        showCompleted = GUILayout.Toggle(showCompleted,"✓",  EditorStyles.toolbarButton, GUILayout.Width(24));
        showWarnings  = GUILayout.Toggle(showWarnings, "⚠",  EditorStyles.toolbarButton, GUILayout.Width(24));
        var cp = GUI.backgroundColor;
        if (showConflictsOnly) GUI.backgroundColor = ColFpsBad;
        if (GUILayout.Toggle(showConflictsOnly, "⚡", EditorStyles.toolbarButton, GUILayout.Width(24)) != showConflictsOnly)
        { showConflictsOnly = !showConflictsOnly; filteredDirty = true; }
        GUI.backgroundColor = cp;
        if (EditorGUI.EndChangeCheck()) filteredDirty = true;

        GUILayout.Space(4);
        EditorGUI.BeginChangeCheck();
        sortMode = (SortMode)EditorGUILayout.EnumPopup(sortMode, EditorStyles.toolbarPopup, GUILayout.Width(80));
        if (GUILayout.Button(sortAsc ? "↑" : "↓", EditorStyles.toolbarButton, GUILayout.Width(20))) sortAsc = !sortAsc;
        if (EditorGUI.EndChangeCheck()) filteredDirty = true;

        GUILayout.FlexibleSpace();

        // Multi-select mode toggle
        var mp = GUI.backgroundColor;
        if (multiSelectMode) GUI.backgroundColor = new Color(0.8f, 0.6f, 1f);
        if (GUILayout.Button("☑ Select", EditorStyles.toolbarButton, GUILayout.Width(58)))
        { multiSelectMode = !multiSelectMode; if (!multiSelectMode) selectedTweens.Clear(); }
        GUI.backgroundColor = mp;

        density = (DensityMode)EditorGUILayout.EnumPopup(density, EditorStyles.toolbarPopup, GUILayout.Width(82));
        if (GUILayout.Button("📋", EditorStyles.toolbarButton, GUILayout.Width(26))) CopyReportToClipboard();
        EditorGUILayout.EndHorizontal();

        // Multi-select bulk action bar
        if (multiSelectMode && selectedTweens.Count > 0)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"  {selectedTweens.Count} selected:", EditorStyles.miniLabel, GUILayout.Width(90));
            if (GUILayout.Button("▶ Resume",   EditorStyles.toolbarButton)) BulkAction(t => t.Resume());
            if (GUILayout.Button("⏸ Pause",   EditorStyles.toolbarButton)) BulkAction(t => t.Pause());
            if (GUILayout.Button("✓ Complete", EditorStyles.toolbarButton)) BulkAction(t => t.Complete());
            if (GUILayout.Button("✕ Kill",     EditorStyles.toolbarButton))
            {
                if (!confirmKill || EditorUtility.DisplayDialog("Kill Selected", $"Kill {selectedTweens.Count} tweens?","Kill","Cancel"))
                    BulkAction(t => { AddToGraveyard(t,"Killed"); t.Kill(); });
            }
            if (GUILayout.Button("⊗ Scale 0.5×", EditorStyles.toolbarButton)) BulkAction(t => t.SetTimeScale(t.TimeScale * 0.5f));
            if (GUILayout.Button("⊕ Scale 2×",   EditorStyles.toolbarButton)) BulkAction(t => t.SetTimeScale(t.TimeScale * 2f));
            if (GUILayout.Button("Reset Scale",   EditorStyles.toolbarButton)) BulkAction(t => t.SetTimeScale(1f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Deselect All", EditorStyles.toolbarButton, GUILayout.Width(82))) selectedTweens.Clear();
            EditorGUILayout.EndHorizontal();
        }
    }

    private void BulkAction(Action<Tween> action)
    {
        var all = new List<Tween>();
        FlowTween.ForEachActiveTween(t      => { if (selectedTweens.Contains(t.GetHashCode())) all.Add(t); });
        FlowTween.ForEachActiveFixedTween(t => { if (selectedTweens.Contains(t.GetHashCode())) all.Add(t); });
        foreach (var t in all) action(t);
        filteredDirty = true;
    }

    private void CompileRegex()
    {
        if (string.IsNullOrEmpty(searchFilter)) { compiledRegex = null; regexValid = true; return; }
        try { compiledRegex = new Regex(searchFilter, RegexOptions.IgnoreCase | RegexOptions.Compiled); regexValid = true; }
        catch { compiledRegex = null; regexValid = false; }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Virtual-Scroll Tween List
    //
    //  Strategy: compute the total virtual height of all cards using CardEstH,
    //  then only call DrawTweenCard() for items whose estimated Y falls within
    //  [scrollY, scrollY + viewHeight].  Items outside get a GUILayout.Space()
    //  spacer instead.  Expanded cards are always rendered (they are rare).
    // ═══════════════════════════════════════════════════════════════════════════

    private void DrawTweenListVirtual(bool isFixed)
    {
        var filtered = isFixed ? filteredFixed : filteredIdle;
        int rawCount = isFixed ? lastFixedCount : lastIdleCount;

        // Summary bar
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"{filtered.Count}/{Mathf.Max(rawCount,0)} {(isFixed?"fixed":"idle")} tweens", EditorStyles.miniLabel);
        if (filtered.Count > maxVisibleCards && maxVisibleCards > 0)
        {
            var wc = GUI.color; GUI.color = ColWarn;
            GUILayout.Label($"  (showing {maxVisibleCards} — raise cap in Settings)", EditorStyles.miniLabel);
            GUI.color = wc;
        }
        if (showPeakWatermarks && peakTweenCount > 0) GUILayout.Label($"  Peak:{(isFixed?peakFixedCount:peakTweenCount):0}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        if (pinnedTweens.Count > 0) { var p = GUI.color; GUI.color = ColPinned; GUILayout.Label($"📌 {pinnedTweens.Count}", EditorStyles.miniLabel); GUI.color = p; }
        GUILayout.Space(6);
        // Freeze snapshot button
        var fp = GUI.backgroundColor;
        if (_frozenSnapshot != null) GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
        if (GUILayout.Button(_frozenSnapshot == null ? "❄ Freeze" : "❄ Frozen", EditorStyles.toolbarButton, GUILayout.Width(62)))
        {
            if (_frozenSnapshot != null) { _frozenSnapshot = null; showFreezePanel = false; }
            else
            {
                _frozenSnapshot = new List<(string, float, float, float, string)>();
                var all = new List<Tween>();
                FlowTween.ForEachActiveTween(t => all.Add(t));
                FlowTween.ForEachActiveFixedTween(t => all.Add(t));
                foreach (var t in all)
                    _frozenSnapshot.Add((t.Target != null ? t.Target.name : "NoTarget", t.Progress, t.Elapsed, t.Duration, t.Group ?? ""));
                _frozenAt = Time.time;
                showFreezePanel = true;
            }
        }
        GUI.backgroundColor = fp;
        EditorGUILayout.EndHorizontal();

        // Freeze comparison panel
        if (_frozenSnapshot != null && showFreezePanel)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"❄ Snapshot at t={_frozenAt:0.00}s  ({_frozenSnapshot.Count} tweens)", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Hide", EditorStyles.miniButton, GUILayout.Width(36))) showFreezePanel = false;
            if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(38))) { _frozenSnapshot = null; showFreezePanel = false; }
            if (GUILayout.Button("Export JSON", EditorStyles.miniButton, GUILayout.Width(76))) ExportSnapshotJSON();
            EditorGUILayout.EndHorizontal();
            // Show first 10 entries as a quick table
            int showN = Mathf.Min(_frozenSnapshot.Count, 10);
            for (int si = 0; si < showN; si++)
            {
                var (sname, sprog, sel, sdur, sgrp) = _frozenSnapshot[si];
                // Try to find live counterpart
                float liveProg = -1f;
                FlowTween.ForEachActiveTween(t => { if (t.Target != null && t.Target.name == sname) liveProg = t.Progress; });
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(sname, EditorStyles.miniLabel, GUILayout.Width(140));
                GUILayout.Label($"{sprog*100f:0.0}%", EditorStyles.miniLabel, GUILayout.Width(38));
                if (liveProg >= 0f)
                {
                    float delta = liveProg - sprog;
                    var dc = GUI.color;
                    GUI.color = delta > 0f ? ColPlaying : delta < 0f ? ColFpsBad : ColCompleted;
                    GUILayout.Label($"Δ{delta*100f:+0.0;-0.0;0}%", EditorStyles.miniLabel, GUILayout.Width(50));
                    GUI.color = dc;
                }
                else { GUILayout.Label("gone", EditorStyles.miniLabel, GUILayout.Width(50)); }
                if (!string.IsNullOrEmpty(sgrp)) { var gc2 = GUI.color; GUI.color = GetGroupColor(sgrp); GUILayout.Label($"[{sgrp}]", EditorStyles.miniLabel); GUI.color = gc2; }
                EditorGUILayout.EndHorizontal();
            }
            if (_frozenSnapshot.Count > showN) GUILayout.Label($"  … {_frozenSnapshot.Count - showN} more (export JSON for full list)", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        // Choose which scroll position ref to use
        ref Vector2 scroll = ref isFixed ? ref fixedScroll : ref tweenScroll;

        // Total virtual height
        int cap      = (maxVisibleCards > 0 && filtered.Count > maxVisibleCards) ? maxVisibleCards : filtered.Count;
        float totalH = 0f;
        for (int i = 0; i < cap; i++)
            totalH += expandedCards.Contains(filtered[i].GetHashCode()) ? CardEstH * 5f : (CardEstH + CardSpacing);

        if (filtered.Count == 0)
        {
            EditorGUILayout.BeginScrollView(scroll);
            DrawEmptyState(isFixed ? "No active fixed tweens." : "No active idle tweens.");
            EditorGUILayout.EndScrollView();
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        float viewH   = position.height - 100f;
        float scrollY = scroll.y;
        float cursor  = 0f;

        // Reuse cached lists to avoid per-frame GC allocations
        _pinnedListCache.Clear();
        _normalListCache.Clear();
        for (int i = 0; i < cap; i++)
        {
            var t = filtered[i];
            if (pinnedTweens.Contains(t.GetHashCode())) _pinnedListCache.Add(t);
            else _normalListCache.Add(t);
        }

        foreach (var t in _pinnedListCache) { DrawTweenCard(t); cursor += CardEstH + CardSpacing; }

        // Virtual scroll — only draw cards in the visible viewport
        for (int i = 0; i < _normalListCache.Count; i++)
        {
            var t = _normalListCache[i];
            bool isExpanded = expandedCards.Contains(t.GetHashCode());
            float cardH = isExpanded ? CardEstH * 5f : CardEstH + CardSpacing;

            if (cursor + cardH < scrollY)
            {
                // Above viewport: emit a spacer, no GUILayout card work at all
                GUILayout.Space(cardH);
            }
            else if (cursor > scrollY + viewH)
            {
                // Below viewport: one bulk spacer for all remaining items
                float remaining = 0f;
                for (int j = i; j < _normalListCache.Count; j++)
                    remaining += expandedCards.Contains(_normalListCache[j].GetHashCode()) ? CardEstH * 5f : CardEstH + CardSpacing;
                GUILayout.Space(remaining);
                break;
            }
            else
            {
                DrawTweenCard(t);
            }
            cursor += cardH;
        }

        // Graveyard foldout
        if (enableGraveyard && graveyard.Count > 0)
        {
            GUILayout.Space(8);
            showGraveyard = EditorGUILayout.Foldout(showGraveyard, $"⚰ Graveyard ({graveyard.Count})", true);
            if (showGraveyard) DrawGraveyard();
        }

        EditorGUILayout.EndScrollView();
    }

    // ─── Warning cache helpers ─────────────────────────────────────────────────

    private List<TweenWarning> GetWarningsCached(Tween t)
    {
        // Rebuild the entire cache once per repaint frame
        if (warningCacheFrame != Time.frameCount)
        {
            warningCache.Clear();
            warningCacheFrame = Time.frameCount;
        }
        int hash = t.GetHashCode();
        if (!warningCache.TryGetValue(hash, out var list))
        {
            list = ComputeWarnings(t);
            warningCache[hash] = list;
        }
        return list;
    }

    private List<TweenWarning> ComputeWarnings(Tween t)
    {
        var list = new List<TweenWarning>(0);
        if (t.Duration  <= 0f)                                  list.Add(new TweenWarning { message = "Zero/negative duration!",              color = ColWarn });
        if (float.IsNaN(t.Elapsed) || float.IsNaN(t.Duration)) list.Add(new TweenWarning { message = "NaN in Elapsed/Duration",              color = ColFpsBad });
        if (t.Duration  > 60f)                                  list.Add(new TweenWarning { message = $"Very long tween ({t.Duration:0.0}s)", color = new Color(0.9f,0.6f,0.2f) });
        if (t.TimeScale <= 0f && t.IsPlaying)                   list.Add(new TweenWarning { message = "TimeScale=0, will never progress",     color = ColWarn });
        if (t.Progress  < 0.001f && t.Elapsed > 2f && t.IsPlaying && t.Delay <= 0f)
                                                                list.Add(new TweenWarning { message = "Possibly stuck at 0%",                 color = ColWarn });
        // Only warn about no target if there's also no interpolator — virtual tweens
        // (FlowVirtual.Float etc.) legitimately have no Unity Object target, they use
        // an Action callback instead. Check via reflection so this works without updated Tween.cs.
        bool hasInterpolator = R_InterpolatorTypeName(t) != null ||
                               (_interpField != null && _interpField.GetValue(t) != null);
        if (t.Target == null && t.IsPlaying && !hasInterpolator)
                                                                list.Add(new TweenWarning { message = "No target and no interpolator assigned", color = new Color(0.7f,0.7f,0.2f) });
        // Detect destroyed Unity object that hasn't been cleaned up yet
        if (t.Target != null && !t.Target)                      list.Add(new TweenWarning { message = "Target has been destroyed! (pending cleanup)", color = ColFpsBad });
        return list;
    }

    private bool IsTweenConflicting(Tween t) => _conflictingTweenHashes.Contains(t.GetHashCode());

    private void CheckWarnings(Tween t)
    {
        if (t.Duration <= 0f)       MaybeLogWarn(t, "Zero duration");
        if (float.IsNaN(t.Elapsed)) MaybeLogWarn(t, "NaN elapsed");
    }

    private void MaybeLogWarn(Tween t, string msg)
    {
        int key = HashCode.Combine(t.GetHashCode(), msg.GetHashCode());
        if (warnedTweens.Add(key)) LogEvent(EventKind.Warning, t, msg);
    }

    // ─── Tween Card ───────────────────────────────────────────────────────────

    private void DrawTweenCard(Tween tween)
    {
        // Get interpolator type via reflection — works even without the updated Tween.cs.
        // DbgInterpolatorType is used as a fast path if available (updated Tween.cs),
        // otherwise fall back to reading the private field directly.
        string interpTypeName = GetInterpolatorTypeName(tween);

        // Virtual tweens (FlowVirtual.*) have no Unity Object target — use the interpolator
        // type as the display name so the card is identifiable rather than showing "No Target"
        string targetName = tween.Target != null && tween.Target
            ? tween.Target.name
            : !string.IsNullOrEmpty(interpTypeName)
                ? $"~ virtual ({interpTypeName})"
                : "— No Target —";
        if (!MatchesSearch(targetName, tween)) return;

        int   hash     = tween.GetHashCode();
        bool  expanded = expandedCards.Contains(hash);
        bool  pinned   = pinnedTweens.Contains(hash);
        bool  selected = selectedTweens.Contains(hash);
        var   warnings = GetWarningsCached(tween);
        if (warnings.Count > 0 && !showWarnings) return;
        if (showConflictsOnly && !IsTweenConflicting(tween)) return;

        float progress   = tween.Progress;
        Color stateColor = tween.IsCompleted ? ColCompleted : tween.IsPaused ? ColPaused : ColPlaying;
        string stateLabel= tween.IsCompleted ? "Done" : tween.IsPaused ? "Paused" : "Playing";
        bool  isConflict = IsTweenConflicting(tween);
        Color barColor   = isConflict ? ColFpsBad
                         : warnings.Count > 0 ? ColWarn
                         : (highlightNearComplete && progress >= NearCompleteThresh && progress < 1f) ? ColBarNear
                         : ColBar;

        EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), ColDivider);

        // Selection highlight background
        if (selected)
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true)), ColSelected);

        // Pinned left accent stripe
        if (pinned)
        {
            Rect ar = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(new Rect(ar.x, ar.y, 3, 80), ColPinned);
        }

        var style = selected ? selectedCardStyle : cardStyle;
        EditorGUILayout.BeginVertical(style);

        // ── Row 1: control buttons + name + badges ────────────────────────────
        EditorGUILayout.BeginHorizontal();

        // Multi-select checkbox
        if (multiSelectMode)
        {
            bool nowSel = EditorGUILayout.Toggle(selected, GUILayout.Width(16));
            if (nowSel != selected) { if (nowSel) selectedTweens.Add(hash); else selectedTweens.Remove(hash); }
        }

        if (SmallBtn(expanded ? "▼" : "▶"))
        { if (expanded) expandedCards.Remove(hash); else expandedCards.Add(hash); }

        var pc = GUI.color; GUI.color = pinned ? ColPinned : new Color(0.5f, 0.5f, 0.5f);
        if (SmallBtn("📌")) { if (pinned) pinnedTweens.Remove(hash); else pinnedTweens.Add(hash); }
        GUI.color = pc;

        if (tween.Target != null && SmallBtn("⦿")) EditorGUIUtility.PingObject(tween.Target);

        if (warnings.Count > 0) { var wc = GUI.color; GUI.color = ColWarn; GUILayout.Label("⚠", tagStyle, GUILayout.Width(18)); GUI.color = wc; }
        if (isConflict)         { var wc = GUI.color; GUI.color = ColFpsBad; GUILayout.Label("⚡CONFLICT", tagStyle, GUILayout.Width(62)); GUI.color = wc; }
        if (R_HasPending(tween)){ var wc = GUI.color; GUI.color = new Color(0.6f,0.85f,1f); GUILayout.Label("⛓CHAIN", tagStyle, GUILayout.Width(50)); GUI.color = wc; }
        if (R_UseUnscaled(tween)) { var wc = GUI.color; GUI.color = ColPaused; GUILayout.Label("⌚", tagStyle, GUILayout.Width(18)); GUI.color = wc; }
        if (R_UseSpeedBase(tween))    { var wc = GUI.color; GUI.color = ColPaused; GUILayout.Label("⚡spd", tagStyle, GUILayout.Width(34)); GUI.color = wc; }
        if (R_IsPopped(tween))        { var wc = GUI.color; GUI.color = ColCompleted; GUILayout.Label("⏳pend", tagStyle, GUILayout.Width(42)); GUI.color = wc; }

        // Highlight name when it matches the search filter
        if (!string.IsNullOrEmpty(searchFilter) && !useRegex &&
            targetName.ToLower().Contains(searchFilter.ToLower()))
        {
            var hc = GUI.color; GUI.color = new Color(1f, 0.95f, 0.4f);
            GUILayout.Label(targetName, labelBoldStyle);
            GUI.color = hc;
        }
        else
        {
            GUILayout.Label(targetName, labelBoldStyle);
        }
        GUILayout.FlexibleSpace();

        // Group badge (clickable → set group filter)
        if (!string.IsNullOrEmpty(tween.Group))
        {
            var gb = GUI.backgroundColor; GUI.backgroundColor = GetGroupColor(tween.Group);
            if (GUILayout.Button(tween.Group, tagStyle, GUILayout.Width(Mathf.Min(tween.Group.Length*7+14,90)), GUILayout.Height(16)))
            { groupFilter = tween.Group; filteredDirty = true; }
            GUI.backgroundColor = gb; GUILayout.Space(3);
        }

        // Interpolator type mini-badge
        string interpType = R_InterpolatorTypeName(tween);
        if (!string.IsNullOrEmpty(interpType))
        {
            string shortType = interpType.Replace("Interpolator","").Replace("StructTweenInterpolator","Struct<…>");
            var it = GUI.color; GUI.color = new Color(0.6f,0.6f,0.8f);
            GUILayout.Label(shortType, EditorStyles.miniLabel, GUILayout.Width(Mathf.Min(shortType.Length*6+4,80)));
            GUI.color = it;
        }

        // Copy as code button
        if (SmallBtn("{ }")) GenerateCopyCode(tween);

        var sc = GUI.color; GUI.color = stateColor;
        GUILayout.Label(stateLabel, tagStyle, GUILayout.Width(50), GUILayout.Height(16));
        GUI.color = sc;
        EditorGUILayout.EndHorizontal();

        // Inline warnings (only when expanded)
        if (warnings.Count > 0 && expanded)
            foreach (var w in warnings) { var wc = GUI.color; GUI.color = w.color; GUILayout.Label($"  ⚠ {w.message}", EditorStyles.miniLabel); GUI.color = wc; }

        // ID / Delay row
        if (tween.Id != null || tween.Delay > 0f)
        {
            EditorGUILayout.BeginHorizontal();
            if (tween.Id    != null) GUILayout.Label($"ID: {tween.Id}", EditorStyles.miniLabel);
            if (tween.Delay > 0f)   GUILayout.Label($"Delay: {tween.Delay:0.00}s", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal();
        }

        // Progress bar
        DrawProgressBar(progress, $"{tween.Elapsed:0.000}s / {tween.Duration:0.000}s  ({progress*100f:0}%)", barColor);

        // Per-tween scrub (expanded only)
        if (expanded)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Scrub", EditorStyles.miniLabel, GUILayout.Width(36));
            float ne = EditorGUILayout.Slider(tween.Elapsed, 0f, Mathf.Max(tween.Duration, 0.001f));
            if (Math.Abs(ne - tween.Elapsed) > 0.0001f) tween.Elapsed = ne;
            EditorGUILayout.EndHorizontal();
        }

        // TimeScale slider
        if (showTimeScaleSliders && density != DensityMode.Compact)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("×Scale", EditorStyles.miniLabel, GUILayout.Width(38));
            float ns = EditorGUILayout.Slider(tween.TimeScale, 0f, 5f);
            if (Math.Abs(ns - tween.TimeScale) > 0.001f) tween.SetTimeScale(ns);
            if (GUILayout.Button("1×", EditorStyles.miniButtonRight, GUILayout.Width(24))) tween.SetTimeScale(1f);
            EditorGUILayout.EndHorizontal();
        }

        // Controls
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("▶",  EditorStyles.miniButtonLeft,  GUILayout.Width(26))) { tween.Resume();   LogEvent(EventKind.Resume,   tween, "Resumed"); }
        if (GUILayout.Button("⏸", EditorStyles.miniButtonMid,   GUILayout.Width(26))) { tween.Pause();    LogEvent(EventKind.Pause,    tween, "Paused"); }
        if (GUILayout.Button("↩",  EditorStyles.miniButtonMid,   GUILayout.Width(26)))   tween.Restart();
        if (GUILayout.Button("✓",  EditorStyles.miniButtonMid,   GUILayout.Width(26))) { tween.Complete(); LogEvent(EventKind.Complete, tween, "Force complete"); AddToGraveyard(tween,"Completed"); }
        if (GUILayout.Button("✕",  EditorStyles.miniButtonRight, GUILayout.Width(26)))
        {
            if (!confirmKill || EditorUtility.DisplayDialog("Kill Tween", $"Kill '{targetName}'?","Kill","Cancel"))
            { AddToGraveyard(tween,"Killed"); LogEvent(EventKind.Kill,tween,"Killed"); tween.Kill(); filteredDirty = true; }
        }
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{progress:P0}", EditorStyles.miniLabel);
        if (density != DensityMode.Compact) GUILayout.Label($"  rem:{(tween.Duration-tween.Elapsed):0.00}s", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        if (expanded) DrawExpandedDetails(tween, hash);

        EditorGUILayout.EndVertical();
        GUILayout.Space(CardSpacing);
    }

    private bool SmallBtn(string label) =>
        GUILayout.Button(label, expandButtonStyle, GUILayout.Width(20), GUILayout.Height(16));

    // ─── Expanded card details ─────────────────────────────────────────────────

    private void DrawExpandedDetails(Tween tween, int hash)
    {
        EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), ColDivider);
        GUILayout.Space(4);
        float halfW = (position.width - 30f) * 0.47f;
        EditorGUILayout.BeginHorizontal();

        // Left column — metadata
        EditorGUILayout.BeginVertical(GUILayout.Width(halfW));
        DetailRow("Update Mode",   tween.UpdateMode.ToString());
        DetailRow("Duration",      $"{tween.Duration:0.000}s");
        DetailRow("Elapsed",       $"{tween.Elapsed:0.000}s");
        DetailRow("Remaining",     $"{(tween.Duration - tween.Elapsed):0.000}s");
        DetailRow("Delay",         $"{tween.Delay:0.000}s  (elapsed {R_DelayElapsed(tween):0.000}s)");
        DetailRow("TimeScale",     $"{tween.TimeScale:0.00}×");
        DetailRow("Loops",         R_Loops(tween) == -1 ? "∞ infinite" : R_Loops(tween) == 0 ? "none" : $"{R_CurrentLoop(tween)}/{R_Loops(tween)}");
        DetailRow("Playback Dir",  R_PlaybackDir(tween) > 0f ? "→ Forward" : "← Reverse");
        DetailRow("Started",       R_IsStarted(tween) ? "Yes" : "Not yet");
        DetailRow("Target Type",   tween.Target != null ? tween.Target.GetType().Name : "null");
        if (tween.Id   != null)                     DetailRow("ID",    tween.Id.ToString());
        if (!string.IsNullOrEmpty(tween.Group))     DetailRow("Group", tween.Group);
        tweenPeakProg.TryGetValue(hash, out float peak);
        peak = tweenPeakProg[hash] = Mathf.Max(peak, tween.Progress);
        DetailRow("Peak Progress", $"{peak:P1}");

        // Flags row
        GUILayout.Space(3);
        var flagsRow = new System.Text.StringBuilder();
        if (R_IsRelative(tween))          flagsRow.Append("Relative  ");
        if (R_UseUnscaled(tween))  flagsRow.Append("UnscaledTime  ");
        if (R_UseSpeedBase(tween))     flagsRow.Append("SpeedBased  ");
        if (R_IsPopped(tween))         flagsRow.Append("Popped(pending)  ");
        if (flagsRow.Length > 0)
        {
            var fc = GUI.color; GUI.color = new Color(0.85f,0.75f,1f);
            GUILayout.Label($"Flags: {flagsRow}", EditorStyles.miniLabel);
            GUI.color = fc;
        }

        // Interpolator value description
        string interpDesc = R_InterpolatorTypeName(tween) != null
            ? GetInterpDescription(tween)
            : "—";
        GUILayout.Space(2);
        var ic = GUI.color; GUI.color = new Color(0.7f,0.9f,1f);
        GUILayout.Label($"Interpolating: {interpDesc}", EditorStyles.miniLabel);
        GUI.color = ic;

        // Conflict warning
        if (IsTweenConflicting(tween))
        {
            GUILayout.Space(2);
            var wc = GUI.color; GUI.color = ColFpsBad;
            GUILayout.Label("⚡ CONFLICT: Another tween is driving the same property on this target!", EditorStyles.miniLabel);
            GUI.color = wc;
            // List the other conflicting tweens
            string interpType = R_InterpolatorTypeName(tween);
            if (tween.Target != null && !string.IsNullOrEmpty(interpType))
            {
                var key = (tween.Target.GetInstanceID(), interpType);
                if (_conflictMap.TryGetValue(key, out var conflicts))
                    foreach (var c in conflicts)
                        if (c != tween) { var gc = GUI.color; GUI.color = ColWarn; GUILayout.Label($"   ↕ also: hash={c.GetHashCode()} {c.Elapsed:0.000}/{c.Duration:0.000}s", EditorStyles.miniLabel); GUI.color = gc; }
            }
        }

        // Then() chain
        if (R_HasPending(tween))
        {
            GUILayout.Space(2);
            var cc = GUI.color; GUI.color = new Color(0.6f,0.85f,1f);
            GUILayout.Label("⛓ Then() chain:", EditorStyles.miniLabel);
            Tween pending = R_PendingTween(tween);
            int depth = 0;
            while (pending != null && depth < 8)
            {
                // Target name — pending tweens may have no target if created via GetTweenRaw/FlowVirtual
                string pname = pending.Target != null && pending.Target
                    ? pending.Target.name
                    : null;

                // Interpolator description — this is what actually tells us what it tweens
                string chainInterpDesc = GetInterpDescription(pending);
                bool hasInterp         = !string.IsNullOrEmpty(chainInterpDesc) && chainInterpDesc != "—";

                // Transition / ease from reflection
                var ptrans = _transField != null ? (Tween.TransitionType)_transField.GetValue(pending) : Tween.TransitionType.Linear;
                var pease  = _easeField  != null ? (Tween.EaseType)_easeField.GetValue(pending)        : Tween.EaseType.In;

                // State label
                string stateLabel = R_IsPopped(pending)   ? "⏳ waiting"
                                  : pending.IsCompleted   ? "✓ done"
                                  : pending.IsPaused      ? "⏸ paused"
                                  :                         "▶ active";

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16);

                // Depth connector
                GUI.color = new Color(0.4f, 0.65f, 0.9f);
                GUILayout.Label($"[{depth+1}]", EditorStyles.miniLabel, GUILayout.Width(24));

                // Target name if available, otherwise the interp type
                GUI.color = pname != null ? Color.white : new Color(0.65f, 0.65f, 0.65f);
                string displayName = pname ?? (R_InterpolatorTypeName(pending)?.Replace("Interpolator","") ?? "virtual");
                GUILayout.Label(displayName, EditorStyles.miniLabel, GUILayout.Width(90));

                // Interpolator value (from → to)
                if (hasInterp)
                {
                    GUI.color = new Color(0.7f, 0.92f, 1f);
                    GUILayout.Label(chainInterpDesc, EditorStyles.miniLabel, GUILayout.Width(160));
                }

                // Duration
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                GUILayout.Label($"{pending.Duration:0.000}s", EditorStyles.miniLabel, GUILayout.Width(50));

                // Ease
                GUI.color = ColCurve;
                GUILayout.Label($"{ptrans}/{pease}", EditorStyles.miniLabel, GUILayout.Width(80));

                // State
                GUI.color = R_IsPopped(pending) ? new Color(0.6f,0.85f,1f)
                          : pending.IsCompleted  ? ColCompleted
                          : pending.IsPaused     ? ColPaused
                          :                        ColPlaying;
                GUILayout.Label(stateLabel, EditorStyles.miniLabel, GUILayout.Width(64));

                GUI.color = cc;
                EditorGUILayout.EndHorizontal();

                pending = R_PendingTween(pending);
                depth++;
            }
            if (depth == 8) GUILayout.Label("   … (chain truncated at 8)", EditorStyles.miniLabel);
            GUI.color = cc;
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(6);

        // Right column — curve + sparkline
        EditorGUILayout.BeginVertical();
        if (showCurvePreviews)
        {
            GUILayout.Label("Easing Curve", EditorStyles.miniLabel);
            Rect cr = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(CurvePreviewH), GUILayout.ExpandWidth(true));
            DrawEaseCurvePreview(cr, tween);
        }
        if (showSparklines)
        {
            GUILayout.Space(4);
            GUILayout.Label("Progress History", EditorStyles.miniLabel);
            Rect sr = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(SparklineH), GUILayout.ExpandWidth(true));
            DrawSparkline(sr, hash);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4);
    }

    private string GetInterpolatorTypeName(Tween tween) => R_InterpolatorTypeName(tween);

    private string GetInterpDescription(Tween tween)
    {
        var interp = R_Interpolator(tween);
        if (interp == null) return "—";

        // Try DbgValueDescription if TweenInterpolator.cs was updated
        var prop = interp.GetType().GetProperty("DbgValueDescription",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (prop != null) return prop.GetValue(interp) as string ?? "—";

        // Fallback: just type name
        return interp.GetType().Name.Replace("Interpolator", "");
    }

    // ─── Copy as Code ─────────────────────────────────────────────────────────

    private void GenerateCopyCode(Tween tween)
    {
        var trans  = _transField != null ? (Tween.TransitionType)_transField.GetValue(tween) : Tween.TransitionType.Linear;
        var ease   = _easeField  != null ? (Tween.EaseType)_easeField.GetValue(tween)        : Tween.EaseType.In;
        bool hasCurve = _curveField != null && _curveField.GetValue(tween) != null;

        string targetName = tween.Target != null ? tween.Target.name : "target";
        string interpDesc = GetInterpDescription(tween);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"// FlowTween — captured at t={Time.time:0.00}s");
        sb.AppendLine($"// Interpolating: {interpDesc}");

        // Build a FlowVirtual or extension call approximation
        // We can only approximate — we don't know the exact extension method name
        sb.Append($"FlowTween.GetTweenRaw({tween.Duration:0.000}f)");

        if (tween.Delay > 0f)     sb.Append($"\n    .SetDelay({tween.Delay:0.000}f)");
        if (Math.Abs(tween.TimeScale - 1f) > 0.001f) sb.Append($"\n    .SetTimeScale({tween.TimeScale:0.000}f)");
        if (R_Loops(tween) != 0)  sb.Append($"\n    .SetLoops({R_Loops(tween)})");
        if (R_IsRelative(tween))     sb.Append("\n    .SetRelative()");
        if (R_UseUnscaled(tween)) sb.Append("\n    .SetUnscaledTime()");
        if (!string.IsNullOrEmpty(tween.Group)) sb.Append($"\n    .SetGroup(\"{tween.Group}\")");
        if (tween.Id != null)     sb.Append($"\n    .SetId({tween.Id})");
        if (!hasCurve)
        {
            if (trans != Tween.TransitionType.Linear) sb.Append($"\n    .SetTransition(Tween.TransitionType.{trans})");
            sb.Append($"\n    .SetEase(Tween.EaseType.{ease})");
        }
        sb.Append(";");

        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log($"[FlowTween] Code copied:\n{sb}");
    }

    private void DetailRow(string label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(82));
        var p = GUI.color; GUI.color = new Color(0.9f, 0.9f, 0.9f);
        GUILayout.Label(value, EditorStyles.miniLabel);
        GUI.color = p;
        EditorGUILayout.EndHorizontal();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Graveyard
    // ═══════════════════════════════════════════════════════════════════════════

    private void AddToGraveyard(Tween t, string cause)
    {
        if (!enableGraveyard) return;
        graveyard.Insert(0, new GraveyardEntry
        {
            killedAt = Time.time, targetName = t.Target != null ? t.Target.name : "NoTarget",
            cause = cause, group = t.Group, id = t.Id,
            duration = t.Duration, elapsedAtDeath = t.Elapsed,
        });
        if (graveyard.Count > GraveyardMax) graveyard.RemoveAt(graveyard.Count - 1);
    }

    private void DrawGraveyard()
    {
        graveyardScroll = EditorGUILayout.BeginScrollView(graveyardScroll, GUILayout.MaxHeight(130));
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"Last {graveyard.Count} ended tweens", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(46))) graveyard.Clear();
        EditorGUILayout.EndHorizontal();
        foreach (var g in graveyard)
        {
            EditorGUILayout.BeginHorizontal();
            Color cc = g.cause == "Killed" ? ColFpsBad : ColCompleted;
            var p = GUI.color; GUI.color = cc;
            GUILayout.Label($"[{g.cause,-9}]", EditorStyles.miniLabel, GUILayout.Width(78)); GUI.color = p;
            GUILayout.Label($"{g.targetName,-20}", EditorStyles.miniLabel, GUILayout.Width(140));
            GUILayout.Label($"{g.elapsedAtDeath:0.00}/{g.duration:0.00}s", EditorStyles.miniLabel, GUILayout.Width(80));
            GUILayout.Label($"t={g.killedAt:0.0}s", EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(g.group)) { GUI.color = GetGroupColor(g.group); GUILayout.Label(g.group, EditorStyles.miniLabel); GUI.color = Color.white; }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Sequence List
    // ═══════════════════════════════════════════════════════════════════════════

    private void DrawSequenceList()
    {
        sequenceSnapshot.Clear();
        FlowTween.ForEachActiveSequence(s => sequenceSnapshot.Add(s));

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"{sequenceSnapshot.Count} sequences", EditorStyles.miniLabel);
        if (showPeakWatermarks && peakSequenceCount > 0) GUILayout.Label($"  Peak:{peakSequenceCount:0}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal();

        sequenceScroll = EditorGUILayout.BeginScrollView(sequenceScroll);
        if (sequenceSnapshot.Count == 0) DrawEmptyState("No active sequences.");
        else for (int i = 0; i < sequenceSnapshot.Count; i++) DrawSequenceCard(sequenceSnapshot[i], i);
        EditorGUILayout.EndScrollView();
    }

    private void DrawSequenceCard(Sequence seq, int index)
    {
        float progress   = seq.TotalDuration > 0f ? Mathf.Clamp01(seq.Elapsed / seq.TotalDuration) : 0f;
        Color stateColor = seq.IsCompleted ? ColCompleted : seq.IsPaused ? ColPaused : ColPlaying;
        string stateLbl  = seq.IsCompleted ? "Done" : seq.IsPaused ? "Paused" : "Playing";
        int seqHash      = seq.GetHashCode();
        bool expanded    = expandedCards.Contains(seqHash);

        EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), ColDivider);
        EditorGUILayout.BeginVertical(cardStyle);

        // ── Header row ───────────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        if (SmallBtn(expanded ? "▼" : "▶")) { if (expanded) expandedCards.Remove(seqHash); else expandedCards.Add(seqHash); }
        GUILayout.Label($"Sequence #{index+1}", labelBoldStyle);
        GUILayout.Space(6);
        var mc = GUI.color; GUI.color = new Color(0.7f, 0.7f, 0.7f);
        GUILayout.Label($"{R_SeqSteps(seq)?.Count ?? 0} steps  •  {R_SeqCallbacks(seq)?.Count ?? 0} callbacks", EditorStyles.miniLabel);
        if (R_SeqLoops(seq) != 0)
        {
            GUI.color = ColPaused;
            GUILayout.Label(R_SeqLoops(seq) == -1 ? "  ∞ loops" : $"  loop {R_SeqCurrentLoop(seq)}/{R_SeqLoops(seq)}", EditorStyles.miniLabel);
        }
        GUI.color = mc;
        GUILayout.FlexibleSpace();
        var sc = GUI.color; GUI.color = stateColor;
        GUILayout.Label(stateLbl, tagStyle, GUILayout.Width(50), GUILayout.Height(16));
        GUI.color = sc;
        EditorGUILayout.EndHorizontal();

        // ── Time info row ─────────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"Duration: {seq.TotalDuration:0.000}s", EditorStyles.miniLabel);
        GUILayout.Space(8);
        GUILayout.Label($"Elapsed: {seq.Elapsed:0.000}s", EditorStyles.miniLabel);
        GUILayout.Space(8);
        GUILayout.Label($"Remaining: {(seq.TotalDuration - seq.Elapsed):0.000}s", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        // ── Overall progress bar ──────────────────────────────────────────────
        Color seqBarColor = (highlightNearComplete && progress >= NearCompleteThresh && progress < 1f) ? ColBarNear : ColSeqBar;
        DrawProgressBar(progress, $"{progress*100f:0.0}%", seqBarColor);

        // ── Scrub ─────────────────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Scrub", EditorStyles.miniLabel, GUILayout.Width(36));
        float newE = EditorGUILayout.Slider(seq.Elapsed, 0f, Mathf.Max(seq.TotalDuration, 0.001f));
        if (Math.Abs(newE - seq.Elapsed) > 0.001f) _seqElapsedProp?.SetValue(seq, newE);
        EditorGUILayout.EndHorizontal();

        // ── Timeline with step blocks ─────────────────────────────────────────
        Rect tlRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(expanded ? 60f : 24f), GUILayout.ExpandWidth(true));
        DrawSequenceTimeline(tlRect, seq, expanded);

        // ── Expanded step list ────────────────────────────────────────────────
        if (expanded) DrawSequenceStepList(seq);

        // ── Controls ─────────────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("▶",  EditorStyles.miniButtonLeft,  GUILayout.Width(30))) seq.Resume();
        if (GUILayout.Button("⏸", EditorStyles.miniButtonMid,   GUILayout.Width(30))) seq.Pause();
        if (GUILayout.Button("✕",  EditorStyles.miniButtonRight, GUILayout.Width(30)))
        {
            if (!confirmKill || EditorUtility.DisplayDialog("Kill Sequence","Kill this sequence?","Kill","Cancel")) seq.Kill();
        }
        GUILayout.FlexibleSpace();
        GUILayout.Label($"{progress:P1}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        GUILayout.Space(CardSpacing);
    }

    private void DrawSequenceStepList(Sequence seq)
    {
        var steps     = R_SeqSteps(seq);
        var callbacks = R_SeqCallbacks(seq);
        if ((steps == null || steps.Count == 0) && (callbacks == null || callbacks.Count == 0)) return;

        EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), ColDivider);
        GUILayout.Space(3);

        // Steps
        if (steps != null)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                var step  = steps[i];
                bool isActive = i == R_SeqActiveStep(seq);
                float stepProgress = R_StepTween(step) != null
                    ? Mathf.Clamp01(R_StepTween(step).Progress)
                    : 0f;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(12);

                // Active indicator
                var ac = GUI.color;
                GUI.color = isActive ? ColPlaying : seq.Elapsed < R_StepStartTime(step) ? ColCompleted : ColPaused;
                GUILayout.Label(isActive ? "▶" : seq.Elapsed > R_StepStartTime(step) + (R_StepTween(step)?.Duration ?? 0f) ? "✓" : "○",
                    EditorStyles.miniLabel, GUILayout.Width(14));
                GUI.color = ac;

                // Step name
                string stepName = R_StepTween(step)?.Target != null ? R_StepTween(step).Target.name : "NoTarget";
                GUILayout.Label($"[{i}] {stepName}", EditorStyles.miniLabel, GUILayout.Width(110));

                // Start time
                var tc = GUI.color; GUI.color = new Color(0.6f,0.6f,0.6f);
                GUILayout.Label($"@{R_StepStartTime(step):0.000}s", EditorStyles.miniLabel, GUILayout.Width(60));
                GUI.color = tc;

                // Duration
                float dur = R_StepTween(step)?.Duration ?? 0f;
                GUILayout.Label($"{dur:0.000}s", EditorStyles.miniLabel, GUILayout.Width(52));

                // Mini progress bar
                DrawInlineProgressBar(stepProgress, 70f, isActive ? ColPlaying : ColCompleted);

                // Interpolator type
                string itype = R_InterpolatorTypeName(R_StepTween(step));
                if (!string.IsNullOrEmpty(itype))
                {
                    string short_ = itype.Replace("Interpolator","").Replace("StructTween","");
                    var ic = GUI.color; GUI.color = new Color(0.55f,0.55f,0.8f);
                    GUILayout.Label(short_, EditorStyles.miniLabel, GUILayout.Width(55));
                    GUI.color = ic;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // Callbacks
        if (callbacks != null && callbacks.Count > 0)
        {
            var cc = GUI.color; GUI.color = new Color(0.85f,0.75f,0.4f);
            for (int i = 0; i < callbacks.Count; i++)
            {
                float cbTime = R_CallbackTime(callbacks[i]);
                bool fired = seq.Elapsed >= cbTime;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(12);
                GUI.color = fired ? ColCompleted : new Color(0.85f,0.75f,0.4f);
                GUILayout.Label(fired ? "✓" : "◇", EditorStyles.miniLabel, GUILayout.Width(14));
                GUILayout.Label($"Callback @{cbTime:0.000}s", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
            GUI.color = cc;
        }
        GUILayout.Space(3);
    }

    private void DrawSequenceTimeline(Rect rect, Sequence seq, bool expanded)
    {
        EditorGUI.DrawRect(rect, ColCurveBg);
        if (seq.TotalDuration <= 0f) return;

        var steps = R_SeqSteps(seq);
        float dur = seq.TotalDuration;

        if (expanded && steps != null && steps.Count > 0)
        {
            // Full visualization: each step is a colored block on its own row
            float rowH  = Mathf.Max(8f, (rect.height - 14f) / Mathf.Max(steps.Count, 1));
            Color[] rowColors = GroupColors;

            for (int i = 0; i < steps.Count; i++)
            {
                var   step   = steps[i];
                float stepDur = R_StepTween(step)?.Duration ?? 0f;
                float x0  = rect.x + rect.width * (R_StepStartTime(step) / dur);
                float x1  = rect.x + rect.width * Mathf.Min((R_StepStartTime(step) + stepDur) / dur, 1f);
                float y   = rect.y + i * rowH;
                Color col = rowColors[i % rowColors.Length];
                col.a = 0.55f;
                EditorGUI.DrawRect(new Rect(x0, y, Mathf.Max(x1 - x0, 2f), rowH - 1f), col);
                // Active fill
                if (R_StepTween(step) != null && seq.Elapsed > R_StepStartTime(step))
                {
                    float prog = Mathf.Clamp01(R_StepTween(step).Progress);
                    Color fillCol = col; fillCol.a = 0.9f;
                    EditorGUI.DrawRect(new Rect(x0, y, Mathf.Max((x1-x0)*prog, 1f), rowH - 1f), fillCol);
                }
                // Label
                if (x1 - x0 > 24f)
                {
                    string lbl = R_StepTween(step)?.Target != null ? R_StepTween(step).Target.name : $"step {i}";
                    EditorGUI.LabelField(new Rect(x0+2, y, x1-x0-4, rowH), lbl, miniCenteredLabel);
                }
            }

            // Draw callbacks as vertical tick marks
            if (R_SeqCallbacks(seq) != null)
                foreach (var cb in R_SeqCallbacks(seq))
                {
                    float cx = rect.x + rect.width * Mathf.Clamp01(R_CallbackTime(cb) / dur);
                    EditorGUI.DrawRect(new Rect(cx-1, rect.y, 2, rect.height - 14f), new Color(1f,0.9f,0.3f,0.8f));
                }

            // Playhead
            float markerX = rect.x + rect.width * Mathf.Clamp01(seq.Elapsed / dur);
            EditorGUI.DrawRect(new Rect(markerX - 1, rect.y, 2, rect.height), Color.white);

            // Time ruler at the bottom
            float rulerY = rect.y + rect.height - 14f;
            EditorGUI.DrawRect(new Rect(rect.x, rulerY, rect.width, 1), ColDivider);
            EditorGUI.LabelField(new Rect(rect.x+2, rulerY+1, 60, 12), "0s", miniCenteredLabel);
            EditorGUI.LabelField(new Rect(rect.x + rect.width/2f - 15, rulerY+1, 30, 12), $"{dur/2f:0.0}s", miniCenteredLabel);
            EditorGUI.LabelField(new Rect(rect.x + rect.width - 30, rulerY+1, 30, 12), $"{dur:0.0}s", miniCenteredLabel);
        }
        else
        {
            // Compact single-bar
            float markerX = rect.x + rect.width * Mathf.Clamp01(seq.Elapsed / dur);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, markerX - rect.x, rect.height), new Color(ColSeqBar.r, ColSeqBar.g, ColSeqBar.b, 0.25f));
            EditorGUI.DrawRect(new Rect(markerX - 1, rect.y, 2, rect.height), Color.white);
            // Tick marks
            if (dur <= 20f)
                for (float t = 0.5f; t < dur; t += 0.5f)
                {
                    float tx = rect.x + rect.width * (t / dur);
                    EditorGUI.DrawRect(new Rect(tx, rect.y + rect.height - 4, 1, 4), ColDivider);
                }
            EditorGUI.LabelField(rect, $"  {seq.Elapsed:0.0}s / {seq.TotalDuration:0.0}s", miniCenteredLabel);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Group Inspector
    // ═══════════════════════════════════════════════════════════════════════════

    private void DrawGroupInspector()
    {
        tweenSnapshot.Clear();
        FlowTween.ForEachActiveTween(t => tweenSnapshot.Add(t));
        FlowTween.ForEachActiveFixedTween(t => tweenSnapshot.Add(t));

        var groups = new Dictionary<string, List<Tween>>();
        foreach (var t in tweenSnapshot)
        {
            string g = string.IsNullOrEmpty(t.Group) ? "(ungrouped)" : t.Group;
            if (!groups.ContainsKey(g)) groups[g] = new List<Tween>();
            groups[g].Add(t);
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"{groups.Count} groups  •  {tweenSnapshot.Count} tweens", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal();

        groupScroll = EditorGUILayout.BeginScrollView(groupScroll);

        foreach (var kvp in groups)
        {
            string gname   = kvp.Key;
            var    members = kvp.Value;
            bool   isNamed = gname != "(ungrouped)";
            Color  gc      = isNamed ? GetGroupColor(gname) : ColCompleted;

            float avgProg = 0f;
            int playingCount = 0, pausedCount = 0;
            foreach (var t in members) { avgProg += t.Progress; if (t.IsPaused) pausedCount++; else if (t.IsPlaying) playingCount++; }
            avgProg /= Mathf.Max(members.Count, 1);

            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), ColDivider);
            EditorGUILayout.BeginVertical(cardStyle);

            EditorGUILayout.BeginHorizontal();
            var p = GUI.color; GUI.color = gc;
            GUILayout.Label("●", GUILayout.Width(14)); GUI.color = p;
            GUILayout.Label(gname, labelBoldStyle);
            GUILayout.Space(6);
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            GUILayout.Label($"{members.Count} tweens  ▶{playingCount} ⏸{pausedCount}", EditorStyles.miniLabel);
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            if (isNamed)
            {
                if (GUILayout.Button("▶",  EditorStyles.miniButtonLeft,  GUILayout.Width(26))) FlowTween.ResumeGroup(gname);
                if (GUILayout.Button("⏸", EditorStyles.miniButtonMid,   GUILayout.Width(26))) FlowTween.PauseGroup(gname);
                if (GUILayout.Button("✕",  EditorStyles.miniButtonRight, GUILayout.Width(26)))
                {
                    if (!confirmKill || EditorUtility.DisplayDialog("Kill Group", $"Kill '{gname}'?","Kill","Cancel"))
                        FlowTween.KillGroup(gname);
                }
            }
            EditorGUILayout.EndHorizontal();

            DrawProgressBar(avgProg, $"Avg {avgProg:P0}", gc);

            foreach (var t in members)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(16);
                string tn = t.Target != null ? t.Target.name : "NoTarget";
                var tc = t.IsCompleted ? ColCompleted : t.IsPaused ? ColPaused : ColPlaying;
                var tp = GUI.color; GUI.color = tc; GUILayout.Label("●", GUILayout.Width(12)); GUI.color = tp;
                GUILayout.Label(tn, EditorStyles.miniLabel, GUILayout.Width(130));
                DrawInlineProgressBar(t.Progress, 80f, gc);
                GUILayout.Label($"{t.Elapsed:0.0}/{t.Duration:0.0}s", EditorStyles.miniLabel, GUILayout.Width(70));
                if (t.Target != null && GUILayout.Button("⦿", expandButtonStyle, GUILayout.Width(18), GUILayout.Height(14)))
                    EditorGUIUtility.PingObject(t.Target);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(CardSpacing);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawInlineProgressBar(float progress, float width, Color color)
    {
        Rect r = GUILayoutUtility.GetRect(width, 10, GUILayout.Width(width));
        EditorGUI.DrawRect(r, ColBarBg);
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width * Mathf.Clamp01(progress), r.height), color);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Pool Inspector Tab
    // ═══════════════════════════════════════════════════════════════════════════

    private static readonly Color ColPoolTween    = new(0.28f, 0.78f, 0.62f);
    private static readonly Color ColPoolSeq      = new(0.75f, 0.42f, 0.95f);
    private static readonly Color ColPoolHit      = new(0.27f, 0.82f, 0.50f);
    private static readonly Color ColPoolMiss     = new(0.95f, 0.38f, 0.30f);
    private static readonly Color ColPoolHitLine  = new(0.27f, 0.82f, 0.50f, 0.7f);
    private static readonly Color ColPoolMissLine = new(0.95f, 0.55f, 0.30f, 0.7f);

    private void DrawPoolTab()
    {
        // ── Toolbar ──────────────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Pool Inspector", EditorStyles.miniLabel, GUILayout.Width(90));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Reset Stats", EditorStyles.toolbarButton, GUILayout.Width(76)))
        {
            FlowTween.ResetPoolStats();
            Array.Clear(tweenPoolSizeHistory,    0, ProfilerHistory);
            Array.Clear(sequencePoolSizeHistory, 0, ProfilerHistory);
            Array.Clear(tweenPoolHitHistory,     0, ProfilerHistory);
            Array.Clear(seqPoolHitHistory,       0, ProfilerHistory);
            poolProfilerHead = 0;
            peakTweenPoolSize = peakSeqPoolSize = 0f;
        }
        EditorGUILayout.EndHorizontal();

        poolScroll = EditorGUILayout.BeginScrollView(poolScroll);
        GUILayout.Space(6);

        // ── Live Counts ──────────────────────────────────────────────────────
        EditorGUILayout.LabelField("Live Pool Sizes", sectionHeaderStyle);
        EditorGUILayout.BeginHorizontal();
        PoolStatBox("Tween\nPool",    FlowTween.TweenPoolSize.ToString(),    ColPoolTween,  $"peak {peakTweenPoolSize:0}");
        PoolStatBox("Sequence\nPool", FlowTween.SequencePoolSize.ToString(), ColPoolSeq,    $"peak {peakSeqPoolSize:0}");
        PoolStatBox("Active\nTweens", FlowTween.ActiveCount.ToString(),       ColBar,       "idle + fixed");
        PoolStatBox("Active\nSeqs",   FlowTween.ActiveSequenceCount.ToString(), ColSeqBar,  "sequences");
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8);

        // ── Pool Size History graph ───────────────────────────────────────────
        EditorGUILayout.LabelField("Pool Size History", sectionHeaderStyle);
        Rect szRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(80), GUILayout.ExpandWidth(true));
        DrawProfilerGraph(szRect, poolProfilerHead,
            new[] { tweenPoolSizeHistory, sequencePoolSizeHistory },
            new[] { ColPoolTween, ColPoolSeq },
            new[] { "Tween Pool", "Sequence Pool" },
            peakWatermark: showPeakWatermarks ? Mathf.Max(peakTweenPoolSize, peakSeqPoolSize) : -1f);
        GUILayout.Space(2);

        // Min-pool threshold reference line annotation
        var mc = GUI.color; GUI.color = new Color(0.8f, 0.8f, 0.8f);
        GUILayout.Label($"  MinPoolSize = {10}  (pool trims when above this threshold)", EditorStyles.miniLabel);
        GUI.color = mc;

        GUILayout.Space(10);

        // ── Hit/Miss Stats ───────────────────────────────────────────────────
        EditorGUILayout.LabelField("Pool Hit / Miss Statistics", sectionHeaderStyle);

        DrawPoolHitMissSection("Tween Pool",
            FlowTween.TweenPoolHits, FlowTween.TweenPoolMisses,
            FlowTween.TweenPoolTotalReturns, FlowTween.TweenPoolHitRate,
            ColPoolTween);

        GUILayout.Space(4);

        DrawPoolHitMissSection("Sequence Pool",
            FlowTween.SequencePoolHits, FlowTween.SequencePoolMisses,
            FlowTween.SequencePoolTotalReturns, FlowTween.SequencePoolHitRate,
            ColPoolSeq);

        GUILayout.Space(10);

        // ── Hit Rate History graph ───────────────────────────────────────────
        EditorGUILayout.LabelField("Hit Rate History  (100% = always reusing, 0% = always allocating)", sectionHeaderStyle);
        Rect hrRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(70), GUILayout.ExpandWidth(true));
        DrawProfilerGraph(hrRect, poolProfilerHead,
            new[] { tweenPoolHitHistory, seqPoolHitHistory },
            new[] { ColPoolHitLine, ColPoolMissLine },
            new[] { "Tween Hit%", "Seq Hit%" },
            maxOverride: 100f, showBaseline: true, baseline: 80f);
        GUILayout.Space(2);
        var bc2 = GUI.color; GUI.color = new Color(0.9f, 0.9f, 0.6f);
        GUILayout.Label("  — Yellow baseline = 80% hit rate target", EditorStyles.miniLabel);
        GUI.color = bc2;

        GUILayout.Space(10);

        // ── Pool Shrink Settings Info ────────────────────────────────────────
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Pool Shrink Policy", sectionHeaderStyle);
        EditorGUILayout.LabelField("FlowTween auto-trims the pool every 10 seconds when it exceeds MinPoolSize (10).", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.BeginHorizontal();
        InfoPill("MinPoolSize",     "10",    ColPoolTween);
        InfoPill("ShrinkInterval",  "10 s",  ColBar);
        InfoPill("ShrinkPercent",   "25%",   ColPaused);
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(4);
        EditorGUILayout.LabelField(
            "Each shrink pass removes ⌈excess × 25%⌉ items (minimum 1), until the pool is back at MinPoolSize.",
            EditorStyles.wordWrappedMiniLabel);

        // Show how many items would be trimmed right now
        int tweenExcess = Mathf.Max(0, FlowTween.TweenPoolSize - 10);
        int seqExcess   = Mathf.Max(0, FlowTween.SequencePoolSize - 10);
        int tweenWouldTrim = tweenExcess > 0 ? Mathf.Max(1, Mathf.CeilToInt(tweenExcess * 0.25f)) : 0;
        int seqWouldTrim   = seqExcess   > 0 ? Mathf.Max(1, Mathf.CeilToInt(seqExcess   * 0.25f)) : 0;
        GUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        var lc = GUI.color;
        GUI.color = tweenExcess > 0 ? ColWarn : ColPlaying;
        GUILayout.Label($"Tweens: excess={tweenExcess}  would trim={tweenWouldTrim}", EditorStyles.miniLabel);
        GUI.color = seqExcess > 0 ? ColWarn : ColPlaying;
        GUILayout.Label($"  |  Sequences: excess={seqExcess}  would trim={seqWouldTrim}", EditorStyles.miniLabel);
        GUI.color = lc;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // ── Interpolator Pools ────────────────────────────────────────────────
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Interpolator Pools  (per value-type caches)", sectionHeaderStyle);
        var interpPools = InterpolatorPoolStats.All;
        if (interpPools.Count == 0)
        {
            GUILayout.Label("No interpolator pools registered yet. They appear once each type is first used.", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            var hc2 = GUI.color; GUI.color = new Color(0.6f,0.6f,0.6f);
            GUILayout.Label("Pool Name", EditorStyles.miniLabel, GUILayout.Width(220));
            GUILayout.Label("Cached", EditorStyles.miniLabel, GUILayout.Width(50));
            GUI.color = hc2;
            EditorGUILayout.EndHorizontal();
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true)), ColDivider);

            foreach (var kvp in interpPools)
            {
                int sz = kvp.Value();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(kvp.Key, EditorStyles.miniLabel, GUILayout.Width(220));
                var vc = GUI.color; GUI.color = sz > 0 ? ColPoolHit : ColCompleted;
                GUILayout.Label(sz.ToString(), EditorStyles.miniLabel, GUILayout.Width(40));
                GUI.color = vc;
                // Mini fill bar
                Rect br = GUILayoutUtility.GetRect(60f, 8f, GUILayout.Width(60f));
                EditorGUI.DrawRect(br, ColBarBg);
                if (sz > 0) EditorGUI.DrawRect(new Rect(br.x, br.y, Mathf.Min(sz * 8f, br.width), br.height), ColPoolHit);
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // ── Health Advisory ──────────────────────────────────────────────────
        DrawPoolHealthAdvisory();

        GUILayout.Space(8);
        EditorGUILayout.EndScrollView();
    }

    private void DrawPoolHitMissSection(string label, int hits, int misses, int returns, float hitRate, Color accent)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        var ac = GUI.color; GUI.color = accent;
        GUILayout.Label(label, new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 }, GUILayout.Width(110));
        GUI.color = ac;
        GUILayout.FlexibleSpace();

        // Hit rate bar
        float rate = hitRate >= 0f ? hitRate : 0f;
        Color barCol = rate >= 0.8f ? ColPoolHit : rate >= 0.5f ? ColPaused : ColPoolMiss;
        Rect barR = GUILayoutUtility.GetRect(150f, 14f, GUILayout.Width(150f));
        EditorGUI.DrawRect(barR, ColBarBg);
        EditorGUI.DrawRect(new Rect(barR.x, barR.y, barR.width * rate, barR.height), barCol);
        string rateStr = hitRate < 0f ? "N/A" : $"{rate * 100f:0.0}% hit rate";
        EditorGUI.LabelField(barR, rateStr, EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(2);
        EditorGUILayout.BeginHorizontal();
        var hc = GUI.color; GUI.color = ColPoolHit;
        GUILayout.Label($"✔ Hits:   {hits,6}", EditorStyles.miniLabel, GUILayout.Width(100));
        GUI.color = ColPoolMiss;
        GUILayout.Label($"✘ Misses: {misses,6}  (= cold allocs, not per-frame)", EditorStyles.miniLabel, GUILayout.Width(210));
        GUI.color = ColBar;
        GUILayout.Label($"↩ Returns: {returns,6}", EditorStyles.miniLabel);
        GUI.color = hc;
        EditorGUILayout.EndHorizontal();

        if (hitRate >= 0f)
        {
            string advice = rate >= 0.9f ? "✔ Excellent — pool is well-warmed."
                          : rate >= 0.7f ? "◑ Good — minor allocation pressure."
                          : rate >= 0.4f ? "⚠ Fair — consider pre-warming the pool at startup."
                          :                "✘ Poor — high allocation rate. Pre-warm or reduce churn.";
            var color = rate >= 0.9f ? ColPlaying : rate >= 0.7f ? ColPaused : ColWarn;
            GUILayout.Space(2);
            var oc = GUI.color; GUI.color = color;
            GUILayout.Label(advice, EditorStyles.miniLabel);
            GUI.color = oc;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawPoolHealthAdvisory()
    {
        float tweenRate = FlowTween.TweenPoolHitRate;
        float seqRate   = FlowTween.SequencePoolHitRate;
        bool anyIssue   = (tweenRate >= 0f && tweenRate < 0.7f) || (seqRate >= 0f && seqRate < 0.7f)
                       || FlowTween.TweenPoolSize == 0 || FlowTween.SequencePoolSize == 0;

        if (!anyIssue)
        {
            EditorGUILayout.HelpBox("✔  Pool health looks good. No issues detected.", MessageType.Info);
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Pool Health Advisories:");

        if (tweenRate >= 0f && tweenRate < 0.7f)
            sb.AppendLine($"  • Tween pool hit rate is low ({tweenRate*100f:0.0}%). Pre-warm with FlowTween.GetTweenRaw() at scene load and kill/return them.");
        if (seqRate >= 0f && seqRate < 0.7f)
            sb.AppendLine($"  • Sequence pool hit rate is low ({seqRate*100f:0.0}%). Re-use sequences instead of creating new ones each time.");
        if (FlowTween.TweenPoolSize == 0 && FlowTween.TweenPoolHits + FlowTween.TweenPoolMisses > 0)
            sb.AppendLine("  • Tween pool is currently empty — all returned tweens have already been reissued.");
        if (FlowTween.SequencePoolSize == 0 && FlowTween.SequencePoolHits + FlowTween.SequencePoolMisses > 0)
            sb.AppendLine("  • Sequence pool is currently empty.");

        EditorGUILayout.HelpBox(sb.ToString().TrimEnd(), MessageType.Warning);
    }

    private void PoolStatBox(string label, string value, Color accent, string sub = "")
    {
        EditorGUILayout.BeginVertical("box", GUILayout.MinWidth(90));
        var p = GUI.color; GUI.color = accent;
        GUILayout.Label(value, new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter });
        GUI.color = Color.white;
        GUILayout.Label(label, new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true });
        if (!string.IsNullOrEmpty(sub)) { GUI.color = new Color(0.6f,0.6f,0.6f); GUILayout.Label(sub, new GUIStyle(EditorStyles.centeredGreyMiniLabel)); }
        GUI.color = p;
        EditorGUILayout.EndVertical();
    }

    private void InfoPill(string label, string value, Color accent)
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(100));
        var p = GUI.color; GUI.color = accent;
        GUILayout.Label(value, new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter });
        GUI.color = Color.white;
        GUILayout.Label(label, EditorStyles.centeredGreyMiniLabel);
        GUI.color = p;
        EditorGUILayout.EndVertical();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Event Log
    // ═══════════════════════════════════════════════════════════════════════════

    public void LogEvent(EventKind kind, Tween t, string detail)
    {
        if (!enableEventLog) return;
        eventLog.Insert(0, new EventEntry
        {
            time = Time.time, kind = kind,
            targetName = t.Target != null ? t.Target.name : "NoTarget",
            detail = detail, group = t.Group, id = t.Id,
        });
        if (eventLog.Count > EventLogMax) eventLog.RemoveAt(eventLog.Count - 1);
    }

    private void DrawEventLogTab()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"{eventLog.Count} events", EditorStyles.miniLabel, GUILayout.Width(65));
        eventLogShowStart       = GUILayout.Toggle(eventLogShowStart,      "Start",  EditorStyles.toolbarButton, GUILayout.Width(40));
        eventLogShowComplete    = GUILayout.Toggle(eventLogShowComplete,   "End",    EditorStyles.toolbarButton, GUILayout.Width(35));
        eventLogShowKill        = GUILayout.Toggle(eventLogShowKill,       "Kill",   EditorStyles.toolbarButton, GUILayout.Width(35));
        eventLogShowLoop        = GUILayout.Toggle(eventLogShowLoop,       "Loop",   EditorStyles.toolbarButton, GUILayout.Width(36));
        eventLogShowWarning     = GUILayout.Toggle(eventLogShowWarning,    "⚠",     EditorStyles.toolbarButton, GUILayout.Width(26));
        eventLogShowPauseResume = GUILayout.Toggle(eventLogShowPauseResume,"⏸/▶",  EditorStyles.toolbarButton, GUILayout.Width(42));
        GUILayout.Space(4);
        GUILayout.Label("🔍", GUILayout.Width(16));
        eventLogSearch = EditorGUILayout.TextField(eventLogSearch, EditorStyles.toolbarSearchField, GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        eventLogAutoScroll = GUILayout.Toggle(eventLogAutoScroll, "Auto", EditorStyles.toolbarButton, GUILayout.Width(40));
        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(46))) eventLog.Clear();
        EditorGUILayout.EndHorizontal();

        eventLogScroll = EditorGUILayout.BeginScrollView(eventLogScroll);
        if (eventLog.Count == 0) DrawEmptyState("No events recorded yet.");
        else foreach (var e in eventLog) { if (ShowEvent(e)) DrawEventRow(e); }
        EditorGUILayout.EndScrollView();
    }

    private bool ShowEvent(EventEntry e)
    {
        bool show = e.kind switch
        {
            EventKind.Start    => eventLogShowStart,
            EventKind.Complete => eventLogShowComplete,
            EventKind.Kill     => eventLogShowKill,
            EventKind.Loop     => eventLogShowLoop,
            EventKind.Warning  => eventLogShowWarning,
            EventKind.Pause    => eventLogShowPauseResume,
            EventKind.Resume   => eventLogShowPauseResume,
            _                  => true,
        };
        if (!show) return false;
        if (!string.IsNullOrEmpty(eventLogSearch))
        {
            string s = eventLogSearch.ToLower();
            return e.targetName.ToLower().Contains(s) || e.detail.ToLower().Contains(s);
        }
        return true;
    }

    private void DrawEventRow(EventEntry e)
    {
        Color kindColor = e.kind switch
        {
            EventKind.Start    => ColPlaying,
            EventKind.Complete => ColCompleted,
            EventKind.Kill     => ColFpsBad,
            EventKind.Loop     => new Color(0.30f,0.75f,0.90f),
            EventKind.Warning  => ColWarn,
            EventKind.Pause    => ColPaused,
            EventKind.Resume   => ColPlaying,
            _                  => Color.white,
        };
        string kindStr = e.kind switch
        {
            EventKind.Start    => "START   ",  EventKind.Complete => "COMPLETE",
            EventKind.Kill     => "KILL    ",  EventKind.Loop     => "LOOP    ",
            EventKind.Warning  => "WARN    ",  EventKind.Pause    => "PAUSE   ",
            EventKind.Resume   => "RESUME  ",  _                  => "EVENT   ",
        };
        EditorGUILayout.BeginHorizontal();
        var p = GUI.color;
        GUI.color = new Color(0.55f,0.55f,0.55f); GUILayout.Label($"t={e.time,6:0.00}s", EditorStyles.miniLabel, GUILayout.Width(70));
        GUI.color = kindColor;                     GUILayout.Label(kindStr, EditorStyles.miniLabel, GUILayout.Width(72));
        GUI.color = Color.white;                   GUILayout.Label(e.targetName, EditorStyles.miniLabel, GUILayout.Width(120));
        GUI.color = new Color(0.75f,0.75f,0.75f); GUILayout.Label(e.detail, EditorStyles.miniLabel);
        if (!string.IsNullOrEmpty(e.group)) { GUI.color = GetGroupColor(e.group); GUILayout.Label($"[{e.group}]", EditorStyles.miniLabel); }
        GUI.color = p;
        EditorGUILayout.EndHorizontal();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Ease Reference Gallery
    // ═══════════════════════════════════════════════════════════════════════════

    private void DrawEaseReferenceTab()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("All 48 easing curves", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        easeGalleryAnimate = GUILayout.Toggle(easeGalleryAnimate, "Animate", EditorStyles.toolbarButton, GUILayout.Width(60));
        if (!easeGalleryAnimate)
        {
            GUILayout.Label("t=", EditorStyles.miniLabel, GUILayout.Width(16));
            easeGalleryT = EditorGUILayout.Slider(easeGalleryT, 0f, 1f, GUILayout.Width(100));
        }
        EditorGUILayout.EndHorizontal();

        easeRefScroll = EditorGUILayout.BeginScrollView(easeRefScroll);

        var transitions = (Tween.TransitionType[])Enum.GetValues(typeof(Tween.TransitionType));
        var eases       = (Tween.EaseType[])Enum.GetValues(typeof(Tween.EaseType));
        float cellW     = Mathf.Max(60f, (position.width - 72f) / eases.Length);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(64));
        foreach (var ease in eases) GUILayout.Label(ease.ToString(), EditorStyles.boldLabel, GUILayout.Width(cellW));
        EditorGUILayout.EndHorizontal();

        foreach (var trans in transitions)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(trans.ToString(), EditorStyles.miniLabel, GUILayout.Width(64));
            foreach (var ease in eases)
            {
                Rect cr = GUILayoutUtility.GetRect(cellW, 54f, GUILayout.Width(cellW));
                DrawGalleryCell(cr, trans, ease, easeGalleryT);
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawGalleryCell(Rect rect, Tween.TransitionType trans, Tween.EaseType ease, float t)
    {
        string key = $"{trans}_{ease}";
        if (!curveCache.TryGetValue(key, out float[] samples))
        {
            samples = new float[CurveSamples];
            for (int i = 0; i < CurveSamples; i++) samples[i] = EaseMath.Evaluate(i / (float)(CurveSamples - 1), trans, ease);
            curveCache[key] = samples;
        }

        bool hovered = rect.Contains(Event.current.mousePosition);
        EditorGUI.DrawRect(rect, hovered ? new Color(0.18f,0.18f,0.22f) : ColCurveBg);
        if (hovered) EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), ColAccent);

        float pad = 4f;
        Rect inner = new Rect(rect.x+pad, rect.y+pad, rect.width-pad*2, rect.height-pad*2);

        if (Event.current.type == EventType.Repaint)
        {
            Handles.color = hovered ? Color.white : ColCurve;
            for (int i = 1; i < CurveSamples; i++)
                Handles.DrawLine(
                    new Vector3(inner.x + inner.width * ((i-1) / (float)(CurveSamples-1)), inner.y + inner.height * (1f - Mathf.Clamp01(samples[i-1]))),
                    new Vector3(inner.x + inner.width * (i     / (float)(CurveSamples-1)), inner.y + inner.height * (1f - Mathf.Clamp01(samples[i]))));

            float bx = inner.x + inner.width * t;
            float by = inner.y + inner.height * (1f - Mathf.Clamp01(EaseMath.Evaluate(t, trans, ease)));
            Handles.color = ColPlaying;
            Handles.DrawSolidDisc(new Vector3(bx, by, 0), Vector3.forward, 3f);
        }

        if (hovered)
        {
            float v = EaseMath.Evaluate(t, trans, ease);
            EditorGUI.LabelField(new Rect(rect.x+2, rect.y+rect.height-14, rect.width-4, 14), $"t={t:0.00} v={v:0.00}", miniCenteredLabel);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Profiler
    // ═══════════════════════════════════════════════════════════════════════════

    private void DrawProfilerTab()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        profilerPaused = GUILayout.Toggle(profilerPaused, profilerPaused ? "⏸ Paused" : "▶ Recording",
            EditorStyles.toolbarButton, GUILayout.Width(90));
        showPeakWatermarks = GUILayout.Toggle(showPeakWatermarks, "Peak Markers", EditorStyles.toolbarButton, GUILayout.Width(88));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(80))) ExportProfilerCSV();
        if (GUILayout.Button("Clear",      EditorStyles.toolbarButton, GUILayout.Width(46))) ClearProfilerData();
        EditorGUILayout.EndHorizontal();

        profilerScroll = EditorGUILayout.BeginScrollView(profilerScroll);
        GUILayout.Space(4);

        EditorGUILayout.LabelField("Live Stats", sectionHeaderStyle);
        EditorGUILayout.BeginHorizontal();
        StatBox("Idle",      FlowTween.ActiveIdleCount.ToString(),     ColPlaying);
        StatBox("Fixed",     FlowTween.ActiveFixedCount.ToString(),    ColPaused);
        StatBox("Total",     FlowTween.ActiveCount.ToString(),         ColBar);
        StatBox("Sequences", FlowTween.ActiveSequenceCount.ToString(), ColSeqBar);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        StatBox("Tween Pool", FlowTween.TweenPoolSize.ToString(), ColPoolTween);
        StatBox("Seq Pool",   FlowTween.SequencePoolSize.ToString(), ColPoolSeq);
        string hrStr = FlowTween.TweenPoolHitRate >= 0f ? $"{FlowTween.TweenPoolHitRate*100f:0.0}%" : "—";
        float  hrVal = FlowTween.TweenPoolHitRate;
        StatBox("Hit Rate", hrStr, hrVal >= 0.8f ? ColPoolHit : hrVal >= 0.5f ? ColPaused : hrVal >= 0f ? ColPoolMiss : ColCompleted);
        StatBox("Misses\n(lifetime)", FlowTween.TweenPoolMisses.ToString(), FlowTween.TweenPoolMisses == 0 ? ColPoolHit : new Color(0.7f, 0.55f, 0.55f));
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);
        float fps_ = Time.deltaTime > 0f ? 1f/Time.deltaTime : 0f;
        EditorGUILayout.BeginHorizontal();
        StatBox("FPS",       $"{fps_:0.0}",               fps_ >= 55 ? ColFpsGood : fps_ >= 30 ? ColFpsOk : ColFpsBad);
        StatBox("Frame ms",  $"{Time.deltaTime*1000f:0.0}", ColBar);
        StatBox("TimeScale", $"{Time.timeScale:0.00}×",    ColPaused);
        StatBox("Game Time", $"{Time.time:0.0}s",          ColBar);
        EditorGUILayout.EndHorizontal();

        if (showPeakWatermarks)
        {
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            StatBox("Peak Idle",  $"{peakTweenCount:0}",    new Color(0.5f,1f,0.5f));
            StatBox("Peak Fixed", $"{peakFixedCount:0}",    new Color(1f,0.8f,0.3f));
            StatBox("Peak Seq",   $"{peakSequenceCount:0}", new Color(0.8f,0.5f,1f));
            StatBox("Peak FPS",   $"{peakFps:0}",           new Color(0.3f,0.8f,1f));
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Tween Count History", sectionHeaderStyle);
        Rect cRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(80), GUILayout.ExpandWidth(true));
        DrawProfilerGraph(cRect, profilerHead,
            new[]{tweenCountHistory, fixedCountHistory, sequenceCountHistory},
            new[]{ColPlaying, ColPaused, ColSeqBar}, new[]{"Idle","Fixed","Seq"},
            peakWatermark: showPeakWatermarks ? Mathf.Max(peakTweenCount, peakFixedCount) : -1f);

        GUILayout.Space(8);
        EditorGUILayout.LabelField("FPS History", sectionHeaderStyle);
        Rect fRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(70), GUILayout.ExpandWidth(true));
        DrawProfilerGraph(fRect, profilerHead, new[]{fpsHistory}, new[]{ColFpsGood}, new[]{"FPS"},
            maxOverride:120f, showBaseline:true, baseline:60f,
            peakWatermark: showPeakWatermarks ? peakFps : -1f);

        GUILayout.Space(10);

        // ── Per-Group tween count history ─────────────────────────────────────
        showGroupProfiler = EditorGUILayout.Foldout(showGroupProfiler, "Per-Group Tween Count History", true);
        if (showGroupProfiler && groupCountHistory.Count > 0)
        {
            groupProfilerScroll = EditorGUILayout.BeginScrollView(groupProfilerScroll, GUILayout.MaxHeight(200));
            foreach (var kvp in groupCountHistory)
            {
                string g = kvp.Key;
                Color  c = g == "(ungrouped)" ? ColCompleted : GetGroupColor(g);
                EditorGUILayout.LabelField(g, EditorStyles.miniLabel);
                Rect gr = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(40), GUILayout.ExpandWidth(true));
                int  gh = groupHistoryHead.TryGetValue(g, out int hh) ? hh : 0;
                DrawProfilerGraph(gr, gh, new[]{kvp.Value}, new[]{c}, new[]{g});
                GUILayout.Space(2);
            }
            EditorGUILayout.EndScrollView();
        }
        else if (showGroupProfiler) { GUILayout.Label("No group history recorded yet.", EditorStyles.miniLabel); }

        // ── Conflict summary ──────────────────────────────────────────────────
        GUILayout.Space(8);
        int conflictCount = _conflictingTweenHashes.Count;
        if (conflictCount > 0)
        {
            var hb = GUI.backgroundColor; GUI.backgroundColor = new Color(0.6f,0.1f,0.1f);
            EditorGUILayout.HelpBox($"⚡ {conflictCount} tweens are in property conflict (same target + interpolator type). See the Update/Fixed tab with ⚡ filter.", MessageType.Warning);
            GUI.backgroundColor = hb;
        }
        else { EditorGUILayout.HelpBox("✔ No property conflicts detected.", MessageType.Info); }

        GUILayout.Space(10);
        EditorGUILayout.EndScrollView();
    }

    private void ClearProfilerData()
    {
        Array.Clear(tweenCountHistory, 0, ProfilerHistory); Array.Clear(fixedCountHistory, 0, ProfilerHistory);
        Array.Clear(sequenceCountHistory, 0, ProfilerHistory); Array.Clear(fpsHistory, 0, ProfilerHistory);
        profilerHead = 0; peakTweenCount = peakFixedCount = peakSequenceCount = peakFps = 0f;
    }

    private void ExportProfilerCSV()
    {
        string path = EditorUtility.SaveFilePanel("Export Profiler CSV", "", "FlowTweenProfiler", "csv");
        if (string.IsNullOrEmpty(path)) return;
        var sb = new StringBuilder();
        sb.AppendLine("Sample,IdleTweens,FixedTweens,Sequences,FPS,TweenPoolSize,SeqPoolSize,TweenHitPct,SeqHitPct");
        for (int i = 0; i < ProfilerHistory; i++)
        {
            int idx  = (profilerHead    + i) % ProfilerHistory;
            int pidx = (poolProfilerHead + i) % ProfilerHistory;
            sb.AppendLine($"{i},{tweenCountHistory[idx]:0},{fixedCountHistory[idx]:0}," +
                          $"{sequenceCountHistory[idx]:0},{fpsHistory[idx]:0.0}," +
                          $"{tweenPoolSizeHistory[pidx]:0},{sequencePoolSizeHistory[pidx]:0}," +
                          $"{tweenPoolHitHistory[pidx]:0.0},{seqPoolHitHistory[pidx]:0.0}");
        }
        System.IO.File.WriteAllText(path, sb.ToString());
        Debug.Log($"[FlowTween] CSV exported → {path}");
    }

    private void StatBox(string label, string value, Color accent)
    {
        EditorGUILayout.BeginVertical("box", GUILayout.MinWidth(80));
        var p = GUI.color; GUI.color = accent;
        GUILayout.Label(value, new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter });
        GUI.color = p;
        GUILayout.Label(label, new GUIStyle(EditorStyles.centeredGreyMiniLabel));
        EditorGUILayout.EndVertical();
    }

    private void DrawProfilerGraph(Rect rect, int head, float[][] series, Color[] colors, string[] labels,
        float maxOverride = -1f, bool showBaseline = false, float baseline = 60f, float peakWatermark = -1f)
    {
        EditorGUI.DrawRect(rect, ColCurveBg);
        float maxVal = maxOverride > 0f ? maxOverride : 1f;
        if (maxOverride < 0f) foreach (var s in series) foreach (var v in s) if (v > maxVal) maxVal = v;
        if (maxVal <= 0f) maxVal = 1f;

        for (int g = 1; g < 4; g++)
        {
            float gy = rect.y + rect.height * (1f - g / 4f);
            EditorGUI.DrawRect(new Rect(rect.x, gy, rect.width, 1), ColGrid);
            float gval = maxVal * g / 4f;
            EditorGUI.LabelField(new Rect(rect.x + 2, gy - 10, 30, 12), gval >= 1f ? gval.ToString("0") : gval.ToString("0.0"), miniCenteredLabel);
        }
        if (showBaseline) { float by = rect.y + rect.height * (1f - Mathf.Clamp01(baseline / maxVal)); EditorGUI.DrawRect(new Rect(rect.x, by, rect.width, 1), new Color(1f, 1f, 0f, 0.35f)); }
        if (peakWatermark > 0f)
        {
            float py = rect.y + rect.height * (1f - Mathf.Clamp01(peakWatermark / maxVal));
            EditorGUI.DrawRect(new Rect(rect.x, py, rect.width, 1), ColWatermark);
            EditorGUI.LabelField(new Rect(rect.x + rect.width - 50, py - 12, 50, 12), $"pk {peakWatermark:0}", miniCenteredLabel);
        }
        if (Event.current.type == EventType.Repaint)
        {
            for (int si = 0; si < series.Length; si++)
            {
                Handles.color = colors[si];
                for (int i = 1; i < ProfilerHistory; i++)
                {
                    int pi = (head + i - 1) % ProfilerHistory, ci = (head + i) % ProfilerHistory;
                    Handles.DrawLine(
                        new Vector3(rect.x + rect.width * ((i-1) / (float)(ProfilerHistory-1)), rect.y + rect.height * (1f - Mathf.Clamp01(series[si][pi] / maxVal))),
                        new Vector3(rect.x + rect.width * (i     / (float)(ProfilerHistory-1)), rect.y + rect.height * (1f - Mathf.Clamp01(series[si][ci] / maxVal))));
                }
            }
        }
        EditorGUILayout.BeginHorizontal();
        for (int si = 0; si < labels.Length; si++) { var p = GUI.color; GUI.color = colors[si]; GUILayout.Label($"— {labels[si]}", EditorStyles.miniLabel, GUILayout.Width(80)); GUI.color = p; }
        GUILayout.FlexibleSpace(); GUILayout.Label($"Max:{maxVal:0}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Settings
    // ═══════════════════════════════════════════════════════════════════════════

    private void DrawSettingsTab()
    {
        settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll);
        GUILayout.Space(8);

        EditorGUILayout.LabelField("Display", sectionHeaderStyle); EditorGUI.indentLevel++;
        density              = (DensityMode)EditorGUILayout.EnumPopup("Card Density",        density);
        refreshRate          = EditorGUILayout.Slider("Refresh Rate (s)", refreshRate, 0.016f, 0.5f);
        showCurvePreviews    = EditorGUILayout.Toggle("Easing Curve Preview",    showCurvePreviews);
        showSparklines       = EditorGUILayout.Toggle("Progress Sparklines",     showSparklines);
        showTimeScaleSliders = EditorGUILayout.Toggle("TimeScale Sliders",       showTimeScaleSliders);
        showPeakWatermarks   = EditorGUILayout.Toggle("Peak Watermarks",         showPeakWatermarks);
        highlightNearComplete = EditorGUILayout.Toggle("Highlight Near-Complete (≥85%)", highlightNearComplete);
        EditorGUI.indentLevel--;

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Performance", sectionHeaderStyle); EditorGUI.indentLevel++;
        EditorGUILayout.HelpBox(
            "Virtual scrolling is always active — only visible cards are rendered.\n" +
            "Max Visible Cards caps the list. 0 = unlimited (may be slow at 1000+).",
            MessageType.Info);
        maxVisibleCards = EditorGUILayout.IntSlider("Max Visible Cards", maxVisibleCards, 0, 1000);
        GUILayout.Space(4);
        slowModeOnHighCount = EditorGUILayout.Toggle("Auto Slow-Mode", slowModeOnHighCount);
        if (slowModeOnHighCount)
            slowModeThreshold = EditorGUILayout.IntSlider("  Slow-Mode Threshold", slowModeThreshold, 50, 2000);
        EditorGUILayout.HelpBox(
            tooManyTweensNow
                ? $"⚠ Slow-Mode ACTIVE ({FlowTween.ActiveCount} tweens ≥ {slowModeThreshold}). Repaint capped at ~7 fps to protect game FPS."
                : $"Slow-Mode inactive ({FlowTween.ActiveCount} tweens < {slowModeThreshold}).",
            tooManyTweensNow ? MessageType.Warning : MessageType.None);
        EditorGUI.indentLevel--;

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Behaviour", sectionHeaderStyle); EditorGUI.indentLevel++;
        confirmKill     = EditorGUILayout.Toggle("Confirm Kill",  confirmKill);
        enableGraveyard = EditorGUILayout.Toggle("Graveyard",     enableGraveyard);
        enableEventLog  = EditorGUILayout.Toggle("Event Log",     enableEventLog);
        EditorGUI.indentLevel--;

        GUILayout.Space(8);
        EditorGUILayout.LabelField("FlowTween Defaults", sectionHeaderStyle); EditorGUI.indentLevel++;
        var nt = (Tween.TransitionType)EditorGUILayout.EnumPopup("Transition", FlowTween.DefaultTransition);
        if (nt != FlowTween.DefaultTransition) FlowTween.SetDefaultTransition(nt);
        var ne = (Tween.EaseType)EditorGUILayout.EnumPopup("Ease", FlowTween.DefaultEase);
        if (ne != FlowTween.DefaultEase) FlowTween.SetDefaultEase(ne);
        if (Application.isPlaying)
        {
            float nts = EditorGUILayout.Slider("Time.timeScale", Time.timeScale, 0f, 5f);
            if (Math.Abs(nts - Time.timeScale) > 0.001f) Time.timeScale = nts;
        }
        EditorGUI.indentLevel--;

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Global Controls", sectionHeaderStyle);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("▶ Resume All"))   FlowTween.ResumeAll();
        if (GUILayout.Button("⏸ Pause All"))   FlowTween.PauseAll();
        if (GUILayout.Button("✓ Complete All")) FlowTween.CompleteAll();
        if (GUILayout.Button("✕ Kill All"))
            if (EditorUtility.DisplayDialog("Kill All","Kill all tweens?","Yes","Cancel")) FlowTween.KillAll();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("▶ Resume Seq"))  FlowTween.ResumeSequences();
        if (GUILayout.Button("⏸ Pause Seq"))  FlowTween.PauseSequences();
        if (GUILayout.Button("✕ Kill Seq"))
            if (EditorUtility.DisplayDialog("Kill Seq","Kill all sequences?","Yes","Cancel")) FlowTween.KillSequences();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Internals", sectionHeaderStyle); EditorGUI.indentLevel++;
        EditorGUILayout.LabelField($"Curve Cache:     {curveCache.Count} entries",  EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"History:         {tweenHistory.Count} tweens", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Warning Cache:   {warningCache.Count} entries",EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Graveyard:       {graveyard.Count} entries",   EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Event Log:       {eventLog.Count} entries",    EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Pinned:          {pinnedTweens.Count}",        EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Selected:        {selectedTweens.Count}",      EditorStyles.miniLabel);
        // Pool
        GUILayout.Space(4);
        EditorGUILayout.LabelField("Pool:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"  Tween Pool Size:     {FlowTween.TweenPoolSize}  (peak {peakTweenPoolSize:0})",    EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"  Sequence Pool Size:  {FlowTween.SequencePoolSize}  (peak {peakSeqPoolSize:0})",   EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"  Tween  Hits/Misses:  {FlowTween.TweenPoolHits} / {FlowTween.TweenPoolMisses}",    EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"  Seq    Hits/Misses:  {FlowTween.SequencePoolHits} / {FlowTween.SequencePoolMisses}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"  Tween  Returns:      {FlowTween.TweenPoolTotalReturns}",    EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"  Seq    Returns:      {FlowTween.SequencePoolTotalReturns}", EditorStyles.miniLabel);
        EditorGUI.indentLevel--;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Curve Cache"))  curveCache.Clear();
        if (GUILayout.Button("Clear History"))      { tweenHistory.Clear(); tweenHistoryHead.Clear(); }
        if (GUILayout.Button("Clear Graveyard"))    graveyard.Clear();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Event Log"))    eventLog.Clear();
        if (GUILayout.Button("Clear Warned Set"))   warnedTweens.Clear();
        if (GUILayout.Button("Unpin All"))          pinnedTweens.Clear();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8);
        EditorGUILayout.LabelField("About", sectionHeaderStyle);
        EditorGUILayout.LabelField("FlowTween Debugger  •  Virtual-Scroll Edition", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.LabelField("Alt+Shift+T  •  9 tabs  •  48 ease curves  •  conflict detection  •  chain display  •  seq timeline  •  copy-as-code  •  interp values  •  pool inspector", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndScrollView();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Easing Curve Preview (expanded card)
    // ═══════════════════════════════════════════════════════════════════════════

    private void DrawEaseCurvePreview(Rect rect, Tween tween)
    {
        Tween.TransitionType trans  = _transField != null ? (Tween.TransitionType)_transField.GetValue(tween) : Tween.TransitionType.Linear;
        Tween.EaseType       ease   = _easeField  != null ? (Tween.EaseType)_easeField.GetValue(tween)       : Tween.EaseType.In;
        AnimationCurve       custom = _curveField != null ? (AnimationCurve)_curveField.GetValue(tween)      : null;

        string key = custom != null ? $"custom_{tween.GetHashCode()}" : $"{trans}_{ease}";
        if (!curveCache.TryGetValue(key, out float[] samples))
        {
            samples = new float[CurveSamples];
            for (int i = 0; i < CurveSamples; i++) { float t = i / (float)(CurveSamples-1); samples[i] = custom != null ? custom.Evaluate(t) : EaseMath.Evaluate(t, trans, ease); }
            curveCache[key] = samples;
        }

        EditorGUI.DrawRect(rect, ColCurveBg);
        for (int g = 1; g < 4; g++) { float gy = rect.y + rect.height*(1f-g/4f); EditorGUI.DrawRect(new Rect(rect.x,gy,rect.width,1), ColGrid); }

        if (Event.current.type == EventType.Repaint)
        {
            Handles.color = ColCurve;
            for (int i = 1; i < CurveSamples; i++)
                Handles.DrawLine(
                    new Vector3(rect.x + rect.width*((i-1)/(float)(CurveSamples-1)), rect.y + rect.height*(1f-Mathf.Clamp01(samples[i-1]))),
                    new Vector3(rect.x + rect.width*(i    /(float)(CurveSamples-1)), rect.y + rect.height*(1f-Mathf.Clamp01(samples[i]))));

            float px = rect.x + rect.width * tween.Progress;
            Handles.color = Color.white;
            Handles.DrawLine(new Vector3(px, rect.y), new Vector3(px, rect.y + rect.height));
            float bval = custom != null ? custom.Evaluate(tween.Progress) : EaseMath.Evaluate(tween.Progress, trans, ease);
            Handles.color = ColPlaying;
            Handles.DrawSolidDisc(new Vector3(px, rect.y + rect.height*(1f - Mathf.Clamp01(bval)), 0), Vector3.forward, 3.5f);
        }

        string cname = custom != null ? "Custom Curve" : $"{trans} / {ease}";
        EditorGUI.LabelField(new Rect(rect.x+3, rect.y+1, rect.width-6, 14), cname, miniCenteredLabel);
    }

    // ─── Sparkline ────────────────────────────────────────────────────────────

    private void RecordHistory(int hash, float value)
    {
        if (!tweenHistory.ContainsKey(hash)) { tweenHistory[hash] = new float[HistorySize]; tweenHistoryHead[hash] = 0; }
        int h = tweenHistoryHead[hash];
        tweenHistory[hash][h] = value;
        tweenHistoryHead[hash] = (h + 1) % HistorySize;
    }

    private void DrawSparkline(Rect rect, int hash)
    {
        EditorGUI.DrawRect(rect, ColCurveBg);
        if (!tweenHistory.TryGetValue(hash, out float[] hist)) return;
        int head = tweenHistoryHead.TryGetValue(hash, out int h) ? h : 0;
        if (Event.current.type == EventType.Repaint)
        {
            Handles.color = ColSparkline;
            for (int i = 1; i < HistorySize; i++)
            {
                int pi = (head + i - 1) % HistorySize, ci = (head + i) % HistorySize;
                Handles.DrawLine(
                    new Vector3(rect.x + rect.width*((i-1)/(float)(HistorySize-1)), rect.y + rect.height*(1f - hist[pi])),
                    new Vector3(rect.x + rect.width*(i    /(float)(HistorySize-1)), rect.y + rect.height*(1f - hist[ci])));
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Shared Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private void DrawProgressBar(float progress, string label, Color barColor)
    {
        Rect rect = GUILayoutUtility.GetRect(18, ProgressBarH, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, ColBarBg);
        if (progress > 0f) EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(progress), rect.height), barColor);
        if (progress > 0f && progress < 1f)
        {
            float ex = rect.x + rect.width * Mathf.Clamp01(progress) - 3;
            Color gc = barColor; gc.a = 0.55f;
            EditorGUI.DrawRect(new Rect(ex, rect.y, 3, rect.height), gc);
        }
        EditorGUI.LabelField(rect, label, EditorStyles.centeredGreyMiniLabel);
    }

    private void DrawEmptyState(string msg)
    {
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
        GUILayout.Label(msg, EditorStyles.centeredGreyMiniLabel);
        GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
    }

    private List<Tween> FilterList(List<Tween> tweens)
    {
        var result = new List<Tween>();
        foreach (var t in tweens)
        {
            if (t.IsCompleted && !showCompleted) continue;
            if (t.IsPaused    && !showPaused)    continue;
            if (t.IsPlaying   && !showPlaying)   continue;
            if (!string.IsNullOrEmpty(groupFilter) &&
                (string.IsNullOrEmpty(t.Group) || !t.Group.ToLower().Contains(groupFilter.ToLower()))) continue;
            result.Add(t);
        }
        return result;
    }

    private void SortList(List<Tween> tweens)
    {
        tweens.Sort((a, b) =>
        {
            bool ap = pinnedTweens.Contains(a.GetHashCode()), bp = pinnedTweens.Contains(b.GetHashCode());
            if (ap != bp) return ap ? -1 : 1;
            int cmp = sortMode switch
            {
                SortMode.Name      => string.Compare(a.Target?.name, b.Target?.name, StringComparison.OrdinalIgnoreCase),
                SortMode.Progress  => a.Progress.CompareTo(b.Progress),
                SortMode.Duration  => a.Duration.CompareTo(b.Duration),
                SortMode.Group     => string.Compare(a.Group, b.Group, StringComparison.OrdinalIgnoreCase),
                SortMode.TimeScale => a.TimeScale.CompareTo(b.TimeScale),
                SortMode.Remaining => (a.Duration - a.Elapsed).CompareTo(b.Duration - b.Elapsed),
                _                  => 0,
            };
            return sortAsc ? cmp : -cmp;
        });
    }

    private bool MatchesSearch(string name, Tween t)
    {
        if (string.IsNullOrEmpty(searchFilter)) return true;
        if (useRegex)
        {
            if (!regexValid || compiledRegex == null) return true;
            return compiledRegex.IsMatch(name) ||
                   (t.Id != null && compiledRegex.IsMatch(t.Id.ToString())) ||
                   (!string.IsNullOrEmpty(t.Group) && compiledRegex.IsMatch(t.Group));
        }
        string f = searchFilter.ToLower();
        return name.ToLower().Contains(f) ||
               (t.Id != null && t.Id.ToString().ToLower().Contains(f)) ||
               (!string.IsNullOrEmpty(t.Group) && t.Group.ToLower().Contains(f));
    }

    private void CollectGroups(List<Tween> tweens)
    {
        foreach (var t in tweens)
            if (!string.IsNullOrEmpty(t.Group) && !groupColorMap.ContainsKey(t.Group))
                groupColorMap[t.Group] = GroupColors[groupColorMap.Count % GroupColors.Length];
    }

    private Color GetGroupColor(string group)
    {
        if (!groupColorMap.TryGetValue(group, out Color c))
        { c = GroupColors[groupColorMap.Count % GroupColors.Length]; groupColorMap[group] = c; }
        return c;
    }

    // ─── Freeze snapshot JSON export ─────────────────────────────────────────

    private void ExportSnapshotJSON()
    {
        if (_frozenSnapshot == null) return;
        string path = EditorUtility.SaveFilePanel("Export Snapshot JSON", "", $"FlowTweenSnapshot_{_frozenAt:0.00}s", "json");
        if (string.IsNullOrEmpty(path)) return;
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"capturedAt\": {_frozenAt:0.000},");
        sb.AppendLine($"  \"count\": {_frozenSnapshot.Count},");
        sb.AppendLine("  \"tweens\": [");
        for (int i = 0; i < _frozenSnapshot.Count; i++)
        {
            var (n, p, el, dur, grp) = _frozenSnapshot[i];
            string comma = i < _frozenSnapshot.Count - 1 ? "," : "";
            sb.AppendLine($"    {{ \"name\": \"{n}\", \"progress\": {p:0.000}, \"elapsed\": {el:0.000}, \"duration\": {dur:0.000}, \"group\": \"{grp}\" }}{comma}");
        }
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        System.IO.File.WriteAllText(path, sb.ToString());
        Debug.Log($"[FlowTween] Snapshot JSON exported → {path}");
    }

    private void CopyReportToClipboard()
    {
        var sb = new StringBuilder();
        sb.AppendLine("══════════════════════════════════════════");
        sb.AppendLine($"  FlowTween Debug Report  |  {DateTime.Now:yyyy-MM-dd HH:mm:ss}  |  t={Time.time:0.00}s");
        sb.AppendLine("══════════════════════════════════════════");
        float fps_ = Time.deltaTime > 0f ? 1f/Time.deltaTime : 0f;
        sb.AppendLine($"  FPS:{fps_:0.0}  TimeScale:{Time.timeScale:0.00}×");
        sb.AppendLine($"  Idle:{FlowTween.ActiveIdleCount}(peak {peakTweenCount:0})  Fixed:{FlowTween.ActiveFixedCount}(peak {peakFixedCount:0})  Seq:{FlowTween.ActiveSequenceCount}(peak {peakSequenceCount:0})");
        sb.AppendLine($"  TweenPool:{FlowTween.TweenPoolSize}  HitRate:{(FlowTween.TweenPoolHitRate>=0f?FlowTween.TweenPoolHitRate*100f:0f):0.0}%  Allocs:{FlowTween.TweenPoolMisses}");
        sb.AppendLine($"  SeqPool:{FlowTween.SequencePoolSize}  HitRate:{(FlowTween.SequencePoolHitRate>=0f?FlowTween.SequencePoolHitRate*100f:0f):0.0}%  Allocs:{FlowTween.SequencePoolMisses}");
        if (_conflictingTweenHashes.Count > 0)
            sb.AppendLine($"  ⚡ CONFLICTS: {_conflictingTweenHashes.Count} tweens in property conflict!");
        sb.AppendLine();
        sb.AppendLine("  STATE    TARGET                   INTERP          PROGRESS  ELAPSED  DURATION  TS    FLAGS  GROUP");
        sb.AppendLine("  ───────────────────────────────────────────────────────────────────────────────────────────────────");
        var snap = new List<Tween>();
        FlowTween.ForEachActiveTween(t => snap.Add(t));
        FlowTween.ForEachActiveFixedTween(t => snap.Add(t));
        foreach (var t in snap)
        {
            string n     = t.Target != null ? t.Target.name : "NoTarget";
            string state = t.IsCompleted ? "Done   " : t.IsPaused ? "Paused " : "Playing";
            string itype = (R_InterpolatorTypeName(t) ?? "—").Replace("Interpolator","").Replace("StructTween","S");
            string flags = "";
            if (t.IsRelative)          flags += "Rel ";
            if (R_UseUnscaled(t))  flags += "Unsc ";
            if (R_HasPending(t))       flags += "Chain ";
            if (IsTweenConflicting(t)) flags += "⚡CONFLICT";
            sb.AppendLine($"  {state}  {n,-24} {itype,-15} {t.Progress*100f,6:0.0}%  {t.Elapsed,7:0.000}s  {t.Duration,7:0.000}s  {t.TimeScale,4:0.00}×  {flags,-12} {t.Group??"-"}");
        }
        if (graveyard.Count > 0)
        {
            sb.AppendLine(); sb.AppendLine($"  Graveyard ({graveyard.Count}):");
            foreach (var g in graveyard)
                sb.AppendLine($"    [{g.cause,-12}] {g.targetName,-24} {g.elapsedAtDeath:0.00}/{g.duration:0.00}s at t={g.killedAt:0.0}s");
        }
        sb.AppendLine("══════════════════════════════════════════");
        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("[FlowTween] Report copied to clipboard.");
    }
}