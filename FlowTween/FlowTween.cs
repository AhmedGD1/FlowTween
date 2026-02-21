using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace FlT
{
    public class FlowTween : MonoBehaviour
    {
        private const int MinPoolSize = 10;
        private const float PoolShrinkInterval = 10f;
        private const float PoolShrinkPercent = 0.25f;

        public static int ActiveIdleCount     => activeTweens.Count;
        public static int ActiveFixedCount    => activeFixedTweens.Count;
        public static int ActiveCount         => activeTweens.Count + activeFixedTweens.Count;
        public static int ActiveSequenceCount => activeSequences.Count;

        public static Tween.TransitionType DefaultTransition => defaultTransition;
        public static Tween.EaseType DefaultEase => defaultEase;

        private static readonly List<Tween> activeTweens = new();
        private static readonly List<Tween> activeFixedTweens = new();

        private static readonly Stack<Tween> tweenPool = new();
        private static readonly Dictionary<string, HashSet<Tween>> groups = new();

        private static readonly List<Sequence> activeSequences = new();
        private static readonly Stack<Sequence> sequencePool = new();
        private static readonly List<Tween> groupSnapshot = new();

        private static readonly Action<Tween> killAction     = t => t.Kill();
        private static readonly Action<Tween> pauseAction    = t => t.Pause();
        private static readonly Action<Tween> resumeAction   = t => t.Resume();
        private static readonly Action<Tween> completeAction = t => t.Complete();

        private static FlowTween instance;

        private static Tween.TransitionType defaultTransition = Tween.TransitionType.Linear;
        private static Tween.EaseType defaultEase = Tween.EaseType.In;

        private static float shrinkTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInit()
        {
            if (instance != null) return;

            GameObject go = new GameObject("FlowTween");
            go.AddComponent<FlowTween>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void Update()
        {
            for (int i = activeTweens.Count - 1; i >= 0; i--)
            {
                Tween tween = activeTweens[i];

                tween.Update(Time.deltaTime, Time.unscaledDeltaTime);

                if (tween.IsCompleted)
                {
                    ReturnToPool(tween);

                    activeTweens[i] = activeTweens[^1];
                    activeTweens.RemoveAt(activeTweens.Count - 1);
                }
            }

            for (int i = activeSequences.Count - 1; i >= 0; i--)
            {
                Sequence sequence = activeSequences[i];
                sequence.Update(Time.deltaTime);

                if (sequence.IsCompleted)
                {
                    ReturnSequenceToPool(sequence);

                    activeSequences[i] = activeSequences[^1];
                    activeSequences.RemoveAt(activeSequences.Count - 1);
                }
            }

            shrinkTimer += Time.deltaTime;

            if (shrinkTimer >= PoolShrinkInterval)
            {
                shrinkTimer = 0f;
                ShrinkPools();
            }
        }

        private void FixedUpdate()
        {
            for (int i = activeFixedTweens.Count - 1; i >= 0; i--)
            {
                Tween tween = activeFixedTweens[i];

                tween.Update(Time.fixedDeltaTime, Time.fixedUnscaledDeltaTime);

                if (tween.IsCompleted)
                {
                    ReturnToPool(tween);

                    activeFixedTweens[i] = activeFixedTweens[^1];
                    activeFixedTweens.RemoveAt(activeFixedTweens.Count - 1);
                }
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            for (int i = activeTweens.Count - 1; i >= 0; i--)
            {
                Tween tween = activeTweens[i];

                GameObject go = tween.Target switch
                {
                    GameObject g => g,
                    Component c  => c.gameObject,
                    _            => null
                };

                if (go != null && go.scene == scene)
                {
                    ReturnToPool(tween);
                    activeTweens.RemoveAt(i);
                }
            }

            for (int i = activeFixedTweens.Count - 1; i >= 0; i--)
            {
                Tween tween = activeFixedTweens[i];

                GameObject go = tween.Target switch
                {
                    GameObject g => g,
                    Component c  => c.gameObject,
                    _            => null
                };

                if (go != null && go.scene == scene)
                {
                    ReturnToPool(tween);
                    activeFixedTweens.RemoveAt(i);
                }
            }
        }

        public static void SetDefaultTransition(Tween.TransitionType transition) => defaultTransition = transition;
        public static void SetDefaultEase(Tween.EaseType ease) => defaultEase = ease;

        #region Pool Shrink
        private static void ShrinkPools()
        {
            TrimPool(tweenPool);
            TrimPool(sequencePool);
        }

        private static void TrimPool<T>(Stack<T> pool)
        {
            int excess = pool.Count - MinPoolSize;

            if (excess <= 0)
                return;

            int toRemove = Mathf.Max(1, Mathf.CeilToInt(excess * PoolShrinkPercent));

            for (int i = 0; i < toRemove; i++)
                pool.Pop();
        }
        #endregion

        #region Tween Manipulation
        private static Tween CreateTween() => new();

        public static Tween GetTween<TTarget, TValue, TInterp>(
            TTarget target, float duration, TValue to)
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
            Tween tween = tweenPool.Count == 0 ? CreateTween() : tweenPool.Pop();
            tween.SetDuration(duration);
            AddActiveTween(tween);
            return tween;
        }

        public static void ReturnToPool(Tween tween)
        {
            tween.ResetData();
            tweenPool.Push(tween);
        }

        public static void RemoveActiveTween(Tween tween)
        {
            switch (tween.UpdateMode)
            {
                case Tween.TweenUpdateMode.Idle:
                    activeTweens.Remove(tween);
                    break;
                
                case Tween.TweenUpdateMode.Fixed:
                    activeFixedTweens.Remove(tween);
                    break;
            }
        }

        public static void AddActiveTween(Tween tween)
        {
            switch (tween.UpdateMode)
            {
                case Tween.TweenUpdateMode.Idle:
                    activeTweens.Add(tween);
                    break;
                
                case Tween.TweenUpdateMode.Fixed:
                    activeFixedTweens.Add(tween);
                    break;
            }
        }
        #endregion

        #region Sequence
        public static Sequence Sequence()
        {
            return GetSequenceFromPool();
        }

        public static void RegisterSequence(Sequence sequence)
        {
            activeSequences.Add(sequence);
        }

        private static Sequence GetSequenceFromPool()
        {
            return sequencePool.Count == 0 ? new() : sequencePool.Pop();
        }

        private static void ReturnSequenceToPool(Sequence sequence)
        {
            sequence.ResetData();
            sequencePool.Push(sequence);
        }

        public static void KillSequences()
        {
            for (int i = 0; i < activeSequences.Count; i++)
                activeSequences[i].Kill();
        }

        public static void PauseSequences()
        {
            for (int i = 0; i < activeSequences.Count; i++)
                activeSequences[i].Pause();
        }

        public static void ResumeSequences()
        {
            for (int i = 0; i < activeSequences.Count; i++)
                activeSequences[i].Resume();
        }
        #endregion

        #region Queries
        public static void ForEachActiveTween(Action<Tween> action)
        {
            for (int i = 0; i < activeTweens.Count; i++)
                action(activeTweens[i]);
        }

        public static void KillAll() => ForEachActiveTween(killAction);
        public static void PauseAll() => ForEachActiveTween(pauseAction);
        public static void ResumeAll() => ForEachActiveTween(resumeAction);
        public static void CompleteAll() => ForEachActiveTween(completeAction);

        public static void KillTarget(UnityEngine.Object target)
        {
            for (int i = 0; i < activeTweens.Count; i++)
                if (activeTweens[i].Target == target)
                    activeTweens[i].Kill();
            
            for (int i = 0; i < activeFixedTweens.Count; i++)
                if (activeFixedTweens[i].Target == target)
                    activeFixedTweens[i].Kill();
        }

        public static void CompleteTarget(UnityEngine.Object target)
        {
            for (int i = 0; i < activeTweens.Count; i++)
                if (activeTweens[i].Target == target)
                    activeTweens[i].Complete();

            for (int i = 0; i < activeFixedTweens.Count; i++)
                if (activeFixedTweens[i].Target == target)
                    activeFixedTweens[i].Complete();
        }

        public static void KillById(object id)
        {
            if (id == null) return;

            for (int i = 0; i < activeTweens.Count; i++)
                if (activeTweens[i].Id != null && activeTweens[i].Id.Equals(id))
                    activeTweens[i].Kill();

            for (int i = 0; i < activeFixedTweens.Count; i++)
                if (activeFixedTweens[i].Id != null && activeFixedTweens[i].Id.Equals(id))
                    activeFixedTweens[i].Kill();
        }
        #endregion

        #region Groups
        public static void AddTweenToGroup(Tween tween, string group)
        {
            if (string.IsNullOrEmpty(group))
                return;

            if (!groups.ContainsKey(group))
                groups[group] = new();
            groups[group].Add(tween);
        }

        public static void RemoveTweenFromGroup(Tween tween, string group)
        {
            if (string.IsNullOrEmpty(group))
                return;

            if (!ValidateGroup(group))
                return;

            groups[group].Remove(tween);

            if (groups[group].Count == 0)
                groups.Remove(group);
        }

        public static void KillGroup(string group) => ForEachGroup(group, killAction);
        public static void PauseGroup(string group) => ForEachGroup(group, pauseAction);
        public static void ResumeGroup(string group) => ForEachGroup(group, resumeAction);

        private static void ForEachGroup(string group, Action<Tween> config)
        {
            if (!ValidateGroup(group))
                return;

            groupSnapshot.Clear();
            groupSnapshot.AddRange(groups[group]);

            for (int i = 0; i < groupSnapshot.Count; i++)
                config(groupSnapshot[i]);
        }

        public static void ForEachActiveFixedTween(Action<Tween> action)
        {
            for (int i = 0; i < activeFixedTweens.Count; i++)
                action(activeFixedTweens[i]);
        }

        public static void ForEachActiveSequence(Action<Sequence> action)
        {
            for (int i = 0; i < activeSequences.Count; i++)
                action(activeSequences[i]);
        }

        private static bool ValidateGroup(string group)
        {
            return !string.IsNullOrEmpty(group) && groups.TryGetValue(group, out var tweens) && tweens.Count > 0;
        }
        #endregion
    }
}
