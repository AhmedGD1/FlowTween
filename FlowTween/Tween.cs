using System;
using UnityEngine;

namespace FlT
{
    public class Tween
    {
        public enum TransitionType { Linear, Sine, Quad, Cubic, Quart, Quint, Expo, Circ, Back, Elastic, Bounce, Spring }
        public enum EaseType { In, Out, InOut, OutIn }

        public enum LoopType { Restart, Yoyo }
        public enum TweenUpdateMode { Idle, Fixed }

        public object Id => id;

        public TweenUpdateMode UpdateMode => updateMode;
        
        public float Progress   => Mathf.Clamp01(Elapsed / Duration);
        public bool IsPlaying   => !paused && !completed;
        public bool IsPaused    => paused;
        public bool IsCompleted => completed;

        internal bool IsRelative      => relative;

        // ── Debug / inspector surface ──────────────────────────────────────────
        public bool   DbgUseUnscaledTime => useUnscaledTime;
        public bool   DbgUseSpeedBase    => useSpeedBase;
        public bool   DbgHasPending      => pendingTween != null;
        public bool   DbgIsPopped        => popped;
        public int    DbgLoops           => loops;
        public int    DbgCurrentLoop     => currentLoop;
        public float  DbgPlaybackDir     => playbackDirection;
        public float  DbgDelayElapsed    => delayElapsed;
        public bool   DbgStarted         => started;
        public string DbgInterpolatorType => interpolator?.GetType().Name;
        public Tween  DbgPendingTween     => pendingTween;

        public string Group
        {
            get => group;
            set
            {
                if (group != null)
                {
                    FlowTween.RemoveTweenFromGroup(this, group);
                }  

                group = value;
                FlowTween.AddTweenToGroup(this, group);
            } 
        }

        public UnityEngine.Object Target { get; set; }

        public float Elapsed { get; set; }
        public float Duration { get; set; }
        public float Delay { get; set; }
        public float TimeScale { get; set; } = 1f;

        private ITweenInterpolator interpolator;
        private Tween pendingTween;

        private Action onStart;
        private Action onComplete;
        private Action<float> onUpdate;
        private Action<int> onLoop;
        private Action onKill;

        private AnimationCurve customCurve;

        private TransitionType transition = FlowTween.DefaultTransition;
        private EaseType ease = FlowTween.DefaultEase;
        private LoopType loopType = LoopType.Restart;
        private TweenUpdateMode updateMode = TweenUpdateMode.Idle;

        private bool completed;
        private bool paused;
        private bool started;
        private bool relative;
        private bool useUnscaledTime;
        private bool useSpeedBase;
        private bool popped;

        private string group;

        private object id;
        private int loops;
        private int currentLoop;

        private float speed;
        private float delayElapsed;
        private float playbackDirection = 1f;

        private void CheckComplete()
        {
            if (currentLoop++ < loops)
            {
                if (loopType == LoopType.Yoyo)
                {
                    playbackDirection *= -1f;
                    Elapsed = playbackDirection > 0f ? 0f : Duration;
                }
                else
                {
                    Elapsed = 0f;
                    started = false;
                }

                onLoop?.Invoke(currentLoop);
                return;
            }

            Complete();
        }

        public void Kill()
        {
            completed = true;
            onKill?.Invoke();
        }

        public Tween Then(Tween tween)
        {
            tween.Pend();
            pendingTween = tween;
            return this;
        }

        public Tween Restart()
        {
            Elapsed = 0f;
            currentLoop = 0;
            completed = false;
            paused = false;
            started = false;
            playbackDirection = 1f;
            delayElapsed = 0f; 

            return this;
        }

        public void Complete()
        {
            Elapsed = playbackDirection > 0f ? Duration : 0f;
            interpolator?.OnComplete();
            onComplete?.Invoke();
            pendingTween?.UnPend();
            completed = true;
        }

        #region Callback Methods
        public Tween OnStart(Action callback)
        {
            onStart = callback;
            return this;
        }

        public Tween OnLoop(Action<int> action)
        {
            onLoop = action;
            return this;
        }

        public Tween OnComplete(Action action)
        {
            onComplete = action;
            return this;
        }

        public Tween OnUpdate(Action<float> callback)
        {
            onUpdate = callback;
            return this;
        }

        public Tween OnKill(Action callback)
        {
            onKill = callback;
            return this;
        }
        #endregion

        #region Pause & Resume
        public Tween Pause()
        {
            paused = true;
            return this;
        }

        public Tween Resume()
        {
            paused = false;
            return this;
        }
        #endregion

        #region Set Methods
        public Tween SetDuration(float duration)
        {
            Duration = duration;
            return this;
        }

        public Tween SetFrom<TValue>(TValue value)
        {
            if (interpolator == null || !interpolator.TrySetFrom(value))
                Debug.LogWarning($"FlowTween: SetFrom<{typeof(TValue).Name}> type mismatch — value ignored.");
            return this;
        }

        public Tween SetLoops(int count, LoopType type = LoopType.Restart)
        {
            loops = count;
            loopType = type;
            return this;
        }

        public Tween SetRelative()
        {
            relative = true;
            return this;
        }

        public Tween SetTransition(TransitionType transition)
        {
            this.transition = transition;
            return this;
        }

        public Tween SetEase(EaseType ease)
        {
            this.ease = ease;
            return this;
        }

        public Tween SetDelay(float delay)
        {
            Delay = delay;
            return this;
        }

        public Tween SetGroup(string groupName)
        {
            Group = groupName;
            return this;
        }

        public Tween SetGroup<TEnum>(TEnum group) where TEnum : Enum => 
            SetGroup(group.ToString());

        public Tween SetTimeScale(float scale)
        {
            TimeScale = scale;
            return this;
        }

        public Tween SetId(object id)
        {
            this.id = id;
            return this;
        }

        public Tween SetCurve(AnimationCurve curve)
        {
            customCurve = curve;
            return this;   
        }

        public Tween SetUnscaledTime(bool value = true)
        {
            useUnscaledTime = value;
            return this;
        }

        public Tween SetSpeedBase(float unitsPerSecond)
        {
            useSpeedBase = true;
            speed = unitsPerSecond;
            return this;
        }

        public Tween SetUpdateMode(TweenUpdateMode mode)
        {
            if (popped) return this;

            FlowTween.RemoveActiveTween(this);
            updateMode = mode;
            FlowTween.AddActiveTween(this);
            return this;
        }
        #endregion
        
        #region Transitions
        public Tween Linear() => SetTransition(TransitionType.Linear);
        public Tween Back() => SetTransition(TransitionType.Back);
        public Tween Sine() => SetTransition(TransitionType.Sine);
        public Tween Quad() => SetTransition(TransitionType.Quad);
        public Tween Expo() => SetTransition(TransitionType.Expo);
        public Tween Circ() => SetTransition(TransitionType.Circ);
        public Tween Quint() => SetTransition(TransitionType.Quint);
        public Tween Quart() => SetTransition(TransitionType.Quart);
        public Tween Cubic() => SetTransition(TransitionType.Cubic);
        public Tween Bounce() => SetTransition(TransitionType.Bounce);
        public Tween Spring() => SetTransition(TransitionType.Spring);
        public Tween Elastic() => SetTransition(TransitionType.Elastic);
        #endregion

        #region Easing
        public Tween EaseIn() => SetEase(EaseType.In);
        public Tween EaseOut() => SetEase(EaseType.Out);
        public Tween EaseInOut() => SetEase(EaseType.InOut);
        public Tween EaseOutIn() => SetEase(EaseType.OutIn);
        #endregion

        #region Internal Methods
        internal void SetInterpolator(ITweenInterpolator i)
        {
            interpolator = i;
        }

        internal void Update(float delta, float unScaledDelta)
        {
            if (Target != null && !Target)
            {
                completed = true;
                return;
            }

            if (paused || completed)
                return;

            float dt = useUnscaledTime ? unScaledDelta : delta;

            if (delayElapsed < Delay)
            {
                delayElapsed += dt;
                return;
            }

            if (!started)
            {
                started = true;
                interpolator?.OnStart();
                onStart?.Invoke();

                if (useSpeedBase && interpolator.TryGetDistance(out float dist))
                {
                    Duration = dist / speed;
                    if (currentLoop == 0) Elapsed = 0f;
                }
            }

            Elapsed += dt * TimeScale * playbackDirection;
            Elapsed = Mathf.Clamp(Elapsed, 0f, Duration);

            float normalized = Mathf.Clamp01(Elapsed / Duration);
            float eased = customCurve != null ? customCurve.Evaluate(normalized) : EaseMath.Evaluate(normalized, transition, ease);

            onUpdate?.Invoke(eased);
            interpolator?.OnTick(eased);

            bool reachedEnd   = playbackDirection > 0f && Elapsed >= Duration;
            bool reachedStart = playbackDirection < 0f && Elapsed <= 0f;

            if (reachedEnd || reachedStart)
            {
                if (loops == -1)
                {
                    if (loopType == LoopType.Yoyo)
                    {
                        playbackDirection *= -1f;
                        Elapsed = playbackDirection > 0f ? 0f : Duration;
                    }
                    else
                    {
                        Elapsed = 0f;
                        started = false;
                    }
                    onLoop?.Invoke(++currentLoop);
                }
                else
                {
                    CheckComplete();
                }
            }
        }

        internal void ResetData()
        {
            interpolator?.ReturnToPool();
            interpolator = null;

            Target = null;

            if (group != null)
                FlowTween.RemoveTweenFromGroup(this, group);
            group = null;

            transition = FlowTween.DefaultTransition;
            ease = FlowTween.DefaultEase;
            updateMode = TweenUpdateMode.Idle;

            completed = false;
            paused = false;
            started = false;
            relative = false;
            useSpeedBase = false;
            useUnscaledTime = false;
            popped = false;

            Elapsed = 0f;
            delayElapsed = 0f;
            Delay = 0f;
            speed = 0f;

            onLoop = null;
            onComplete = null;
            onUpdate = null;
            onStart = null;
            onKill = null;

            loops = 0;
            currentLoop = 0;

            customCurve = null;
            pendingTween = null;

            TimeScale = 1f;
            playbackDirection = 1f;
        }
        #endregion

        internal void Pend()
        {
            Pop();
            FlowTween.RegisterPending(this);   // tell debugger this is a .Then() chain tween
        }

        internal void UnPend()
        {
            FlowTween.UnregisterPending(this); // remove from pending before re-activating
            Restart();
            FlowTween.AddActiveTween(this);
            popped = false;
        }

        internal void Pop()
        {
            popped = true;
            FlowTween.RemoveActiveTween(this);
        }
    }
}