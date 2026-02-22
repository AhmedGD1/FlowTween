using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace FlT
{
    public class FlowTween : MonoBehaviour
    {
        // ── Fallback constants (used when no settings asset exists) ───────────
        private const int   FallbackMinPoolSize    = 10;
        private const float FallbackShrinkInterval = 10f;
        private const float FallbackShrinkPercent  = 0.25f;

        // ── Runtime-configurable settings (loaded from FlowTweenSettings) ─────
        private static int   minPoolSize      = FallbackMinPoolSize;
        private static float shrinkInterval   = FallbackShrinkInterval;
        private static float shrinkPercent    = FallbackShrinkPercent;
        private static float globalTimeScale  = 1f;
        private static bool  killOnSceneUnload = true;
        private static bool  autoKillOrphans   = true;

        // ── Public statistics ─────────────────────────────────────────────────
        public static int   ActiveIdleCount          => activeTweens.Count;
        public static int   ActiveFixedCount         => activeFixedTweens.Count;
        public static int   ActiveCount              => activeTweens.Count + activeFixedTweens.Count;
        public static int   PendingTweenCount        => pendingTweens.Count;
        public static int   ActiveSequenceCount      => activeSequences.Count;
        public static int   TweenPoolSize            => tweenPool.Count;
        public static int   SequencePoolSize         => sequencePool.Count;
        public static int   TweenPoolHits            => tweenPoolHits;
        public static int   TweenPoolMisses          => tweenPoolMisses;
        public static int   SequencePoolHits         => sequencePoolHits;
        public static int   SequencePoolMisses       => sequencePoolMisses;
        public static int   TweenPoolTotalReturns    => tweenPoolTotalReturns;
        public static int   SequencePoolTotalReturns => sequencePoolTotalReturns;

        public static float TweenPoolHitRate =>
            (tweenPoolHits + tweenPoolMisses) == 0 ? -1f
            : tweenPoolHits / (float)(tweenPoolHits + tweenPoolMisses);

        public static float SequencePoolHitRate =>
            (sequencePoolHits + sequencePoolMisses) == 0 ? -1f
            : sequencePoolHits / (float)(sequencePoolHits + sequencePoolMisses);

        // ── Public settings getters ───────────────────────────────────────────
        public static Tween.TransitionType DefaultTransition  => defaultTransition;
        public static Tween.EaseType       DefaultEase        => defaultEase;
        public static float                GlobalTimeScale    => globalTimeScale;
        public static bool                 AutoKillOrphans    => autoKillOrphans;
        public static bool                 KillOnSceneUnload  => killOnSceneUnload;
        public static FlowTweenSettings    Settings           => settingsAsset;

        // ── Private collections ───────────────────────────────────────────────
        private static readonly List<Tween>                        activeTweens      = new();
        private static readonly List<Tween>                        activeFixedTweens = new();
        private static readonly HashSet<Tween>                     pendingTweens     = new();
        private static readonly Stack<Tween>                       tweenPool         = new();
        private static readonly Dictionary<string, HashSet<Tween>> groups            = new();
        private static readonly List<Sequence>                     activeSequences   = new();
        private static readonly Stack<Sequence>                    sequencePool      = new();
        private static readonly List<Tween>                        groupSnapshot     = new();

        private static readonly Action<Tween> killAction     = t => t.Kill();
        private static readonly Action<Tween> pauseAction    = t => t.Pause();
        private static readonly Action<Tween> resumeAction   = t => t.Resume();
        private static readonly Action<Tween> completeAction = t => t.Complete();

        private static FlowTween         instance;
        private static FlowTweenSettings settingsAsset;

        private static Tween.TransitionType defaultTransition = Tween.TransitionType.Linear;
        private static Tween.EaseType       defaultEase       = Tween.EaseType.In;

        private static float shrinkTimer;
        private static int   tweenPoolHits,    tweenPoolMisses,    tweenPoolTotalReturns;
        private static int   sequencePoolHits, sequencePoolMisses, sequencePoolTotalReturns;

        // ═════════════════════════════════════════════════════════════════════
        //  Initialisation
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Runs before the first scene loads.
        /// Loads the settings asset and spawns the singleton.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInit()
        {
            if (instance != null) return;
            LoadSettings();
            var go = new GameObject("[FlowTween]");
            go.AddComponent<FlowTween>();
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            // Pre-warm pools now that the MonoBehaviour is alive.
            if (settingsAsset != null)
                Prewarm(settingsAsset.prewarmTweens, settingsAsset.prewarmSequences);
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Settings — Load & Apply
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Loads FlowTweenSettings from Resources and applies every field.
        /// Called automatically at startup; safe to call manually at any time.
        /// </summary>
        public static void LoadSettings()
        {
            settingsAsset = Resources.Load<FlowTweenSettings>("FlowTweenSettings");
            if (settingsAsset != null) ApplySettings(settingsAsset);
        }

        /// <summary>
        /// Pushes every field from <paramref name="s"/> into the live runtime
        /// state immediately. Safe to call during Play Mode.
        /// </summary>
        public static void ApplySettings(FlowTweenSettings s)
        {
            if (s == null) return;
            defaultTransition = s.defaultTransition;
            defaultEase       = s.defaultEase;
            globalTimeScale   = Mathf.Max(0f, s.globalTimeScale);
            killOnSceneUnload = s.killOnSceneUnload;
            autoKillOrphans   = s.autoKillOrphans;
            minPoolSize       = Mathf.Max(0, s.minPoolSize);
            shrinkInterval    = Mathf.Max(0.1f, s.shrinkInterval);
            shrinkPercent     = Mathf.Clamp01(s.shrinkPercent);
            settingsAsset     = s;
        }

        /// <summary>
        /// Fills the pools up to <paramref name="tweenCount"/> and
        /// <paramref name="sequenceCount"/> without over-allocating.
        /// </summary>
        public static void Prewarm(int tweenCount, int sequenceCount)
        {
            for (int i = tweenPool.Count;    i < tweenCount;    i++) tweenPool.Push(new Tween());
            for (int i = sequencePool.Count; i < sequenceCount; i++) sequencePool.Push(new Sequence());
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Update Loops
        // ═════════════════════════════════════════════════════════════════════

        private void Update()
        {
            // globalTimeScale is a FlowTween-layer multiplier applied on top of
            // Time.deltaTime. Tweens that called SetUnscaledTime(true) receive the
            // raw unscaled delta inside Tween.Update, bypassing globalTimeScale.
            float dt         = Time.deltaTime         * globalTimeScale;
            float unscaledDt = Time.unscaledDeltaTime;

            for (int i = activeTweens.Count - 1; i >= 0; i--)
            {
                Tween t = activeTweens[i];

                // Orphan guard — skipped entirely when the setting is off.
                if (autoKillOrphans && t.Target != null && !t.Target)
                    t.Kill();

                t.Update(dt, unscaledDt);

                if (t.IsCompleted)
                {
                    ReturnToPool(t);
                    activeTweens[i] = activeTweens[^1];
                    activeTweens.RemoveAt(activeTweens.Count - 1);
                }
            }

            for (int i = activeSequences.Count - 1; i >= 0; i--)
            {
                Sequence s = activeSequences[i];
                s.Update(dt);

                if (s.IsCompleted)
                {
                    ReturnSequenceToPool(s);
                    activeSequences[i] = activeSequences[^1];
                    activeSequences.RemoveAt(activeSequences.Count - 1);
                }
            }

            shrinkTimer += Time.deltaTime;
            if (shrinkTimer >= shrinkInterval)
            {
                shrinkTimer = 0f;
                ShrinkPools();
            }
        }

        private void FixedUpdate()
        {
            float dt         = Time.fixedDeltaTime         * globalTimeScale;
            float unscaledDt = Time.fixedUnscaledDeltaTime;

            for (int i = activeFixedTweens.Count - 1; i >= 0; i--)
            {
                Tween t = activeFixedTweens[i];

                if (autoKillOrphans && t.Target != null && !t.Target)
                    t.Kill();

                t.Update(dt, unscaledDt);

                if (t.IsCompleted)
                {
                    ReturnToPool(t);
                    activeFixedTweens[i] = activeFixedTweens[^1];
                    activeFixedTweens.RemoveAt(activeFixedTweens.Count - 1);
                }
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Scene Lifecycle
        // ═════════════════════════════════════════════════════════════════════

        private void OnSceneUnloaded(Scene scene)
        {
            if (!killOnSceneUnload) return;
            CleanSceneFromList(activeTweens,      scene);
            CleanSceneFromList(activeFixedTweens, scene);
        }

        private static void CleanSceneFromList(List<Tween> list, Scene scene)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                GameObject go = list[i].Target switch
                {
                    GameObject g => g,
                    Component  c => c.gameObject,
                    _            => null
                };
                if (go != null && go.scene == scene)
                {
                    ReturnToPool(list[i]);
                    list.RemoveAt(i);
                }
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Runtime Setters
        // ═════════════════════════════════════════════════════════════════════

        public static void SetDefaultTransition(Tween.TransitionType t) => defaultTransition = t;
        public static void SetDefaultEase(Tween.EaseType e)             => defaultEase       = e;

        /// <summary>
        /// Adjusts the FlowTween-layer time multiplier without touching
        /// <see cref="Time.timeScale"/>. Tweens using SetUnscaledTime(true)
        /// are not affected.
        /// </summary>
        public static void SetGlobalTimeScale(float scale) => globalTimeScale = Mathf.Max(0f, scale);

        public static void SetAutoKillOrphans(bool value)   => autoKillOrphans   = value;
        public static void SetKillOnSceneUnload(bool value) => killOnSceneUnload = value;

        public static void ResetPoolStats()
        {
            tweenPoolHits = tweenPoolMisses = tweenPoolTotalReturns = 0;
            sequencePoolHits = sequencePoolMisses = sequencePoolTotalReturns = 0;
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Pool Shrink
        // ═════════════════════════════════════════════════════════════════════

        private static void ShrinkPools()
        {
            TrimPool(tweenPool);
            TrimPool(sequencePool);
        }

        private static void TrimPool<T>(Stack<T> pool)
        {
            int excess = pool.Count - minPoolSize;
            if (excess <= 0) return;
            int toRemove = Mathf.Max(1, Mathf.CeilToInt(excess * shrinkPercent));
            for (int i = 0; i < toRemove; i++) pool.Pop();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Tween Factory & Pool
        // ═════════════════════════════════════════════════════════════════════

        public static Tween GetTween<TTarget, TValue, TInterp>(TTarget target, float duration, TValue to)
            where TTarget : UnityEngine.Object
            where TInterp : struct, IPropertyInterpolator<TTarget, TValue>
        {
            Tween tween = GetTweenRaw(duration);
            tween.Target = target;
            var interp = StructTweenInterpolator<TTarget, TValue, TInterp>.Get();
            interp.Setup(target, to, tween);
            tween.SetInterpolator(interp);
            return tween;
        }

        public static Tween GetTweenRaw(float duration)
        {
            Tween tween;
            if (tweenPool.Count == 0) { tweenPoolMisses++; tween = new Tween(); }
            else                       { tweenPoolHits++;   tween = tweenPool.Pop(); }
            tween.SetDuration(duration);
            AddActiveTween(tween);
            return tween;
        }

        public static void ReturnToPool(Tween tween)
        {
            pendingTweens.Remove(tween); // clean up if killed while pending
            tween.ResetData();
            tweenPool.Push(tween);
            tweenPoolTotalReturns++;
        }

        public static void RemoveActiveTween(Tween tween)
        {
            switch (tween.UpdateMode)
            {
                case Tween.TweenUpdateMode.Idle:  activeTweens.Remove(tween);      break;
                case Tween.TweenUpdateMode.Fixed: activeFixedTweens.Remove(tween); break;
            }
        }

        public static void AddActiveTween(Tween tween)
        {
            switch (tween.UpdateMode)
            {
                case Tween.TweenUpdateMode.Idle:  activeTweens.Add(tween);      break;
                case Tween.TweenUpdateMode.Fixed: activeFixedTweens.Add(tween); break;
            }
        }

        /// <summary>Called only by Tween.Pend() — tracks .Then() chains for the debugger.
        /// NOT called by Sequence.AbsorbTween, so sequence tweens are never shown here.</summary>
        internal static void RegisterPending(Tween tween)   => pendingTweens.Add(tween);

        /// <summary>Called only by Tween.UnPend() — removes from pending when the chain fires.</summary>
        internal static void UnregisterPending(Tween tween) => pendingTweens.Remove(tween);

        // ═════════════════════════════════════════════════════════════════════
        //  Sequence Factory & Pool
        // ═════════════════════════════════════════════════════════════════════

        public static Sequence Sequence() => GetSequenceFromPool();

        public static void RegisterSequence(Sequence sequence) => activeSequences.Add(sequence);

        private static Sequence GetSequenceFromPool()
        {
            if (sequencePool.Count == 0) { sequencePoolMisses++; return new Sequence(); }
            sequencePoolHits++;
            return sequencePool.Pop();
        }

        private static void ReturnSequenceToPool(Sequence sequence)
        {
            sequence.ResetData();
            sequencePool.Push(sequence);
            sequencePoolTotalReturns++;
        }

        public static void KillSequences()   { foreach (var s in activeSequences) s.Kill();   }
        public static void PauseSequences()  { foreach (var s in activeSequences) s.Pause();  }
        public static void ResumeSequences() { foreach (var s in activeSequences) s.Resume(); }

        // ═════════════════════════════════════════════════════════════════════
        //  Queries & Global Controls
        // ═════════════════════════════════════════════════════════════════════

        public static void ForEachActiveTween(Action<Tween> action)
        {
            for (int i = 0; i < activeTweens.Count; i++) action(activeTweens[i]);
        }
        public static void ForEachActiveFixedTween(Action<Tween> action)
        {
            for (int i = 0; i < activeFixedTweens.Count; i++) action(activeFixedTweens[i]);
        }
        public static void ForEachPendingTween(Action<Tween> action)
        {
            foreach (var t in pendingTweens) action(t);
        }
        public static void ForEachActiveSequence(Action<Sequence> action)
        {
            for (int i = 0; i < activeSequences.Count; i++) action(activeSequences[i]);
        }

        public static void KillAll()     => ForEachActiveTween(killAction);
        public static void PauseAll()    => ForEachActiveTween(pauseAction);
        public static void ResumeAll()   => ForEachActiveTween(resumeAction);
        public static void CompleteAll() => ForEachActiveTween(completeAction);

        public static void KillTarget(UnityEngine.Object target)
        {
            for (int i = 0; i < activeTweens.Count;      i++) if (activeTweens[i].Target      == target) activeTweens[i].Kill();
            for (int i = 0; i < activeFixedTweens.Count; i++) if (activeFixedTweens[i].Target == target) activeFixedTweens[i].Kill();
        }

        public static void CompleteTarget(UnityEngine.Object target)
        {
            for (int i = 0; i < activeTweens.Count;      i++) if (activeTweens[i].Target      == target) activeTweens[i].Complete();
            for (int i = 0; i < activeFixedTweens.Count; i++) if (activeFixedTweens[i].Target == target) activeFixedTweens[i].Complete();
        }

        public static void KillById(object id)
        {
            if (id == null) return;
            for (int i = 0; i < activeTweens.Count;      i++) if (activeTweens[i].Id      != null && activeTweens[i].Id.Equals(id))      activeTweens[i].Kill();
            for (int i = 0; i < activeFixedTweens.Count; i++) if (activeFixedTweens[i].Id != null && activeFixedTweens[i].Id.Equals(id)) activeFixedTweens[i].Kill();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Groups
        // ═════════════════════════════════════════════════════════════════════

        public static void AddTweenToGroup(Tween tween, string group)
        {
            if (string.IsNullOrEmpty(group)) return;
            if (!groups.ContainsKey(group)) groups[group] = new HashSet<Tween>();
            groups[group].Add(tween);
        }

        public static void RemoveTweenFromGroup(Tween tween, string group)
        {
            if (string.IsNullOrEmpty(group) || !ValidateGroup(group)) return;
            groups[group].Remove(tween);
            if (groups[group].Count == 0) groups.Remove(group);
        }

        // ── String overloads — canonical implementation ───────────────────────
        public static void KillGroup(string   group) => ForEachGroup(group, killAction);
        public static void PauseGroup(string  group) => ForEachGroup(group, pauseAction);
        public static void ResumeGroup(string group) => ForEachGroup(group, resumeAction);

        // ── Enum overloads — delegate to string overloads (no recursion) ─────
        public static void KillGroup<TEnum>(TEnum   g) where TEnum : Enum => KillGroup(g.ToString());
        public static void PauseGroup<TEnum>(TEnum  g) where TEnum : Enum => PauseGroup(g.ToString());
        public static void ResumeGroup<TEnum>(TEnum g) where TEnum : Enum => ResumeGroup(g.ToString());

        public static void ForEachGroup(string group, Action<Tween> config)
        {
            if (!ValidateGroup(group)) return;
            groupSnapshot.Clear();
            groupSnapshot.AddRange(groups[group]);
            for (int i = 0; i < groupSnapshot.Count; i++) config(groupSnapshot[i]);
        }

        private static bool ValidateGroup(string group) =>
            !string.IsNullOrEmpty(group) &&
            groups.TryGetValue(group, out var tweens) &&
            tweens.Count > 0;
    }
}