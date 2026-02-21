using System;
using UnityEngine;

namespace FlT
{
    public class Tween
    {
        public enum TransitionType { Linear, Sine, Quad, Cubic, Quart, Quint, Expo, Circ, Back, Elastic, Bounce, Spring }
        public enum EaseType { In, Out, InOut, OutIn }

        public enum LoopType { Restart, Yoyo }

        public object Id => id;

        public float Progress => Mathf.Clamp01(Elapsed / Duration);
        public bool IsPaused => paused;

        internal bool IsRelative => relative;

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

        public bool IsCompleted { get; private set; }

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

        private AnimationCurve customCurve;

        private TransitionType transition = TransitionType.Linear;
        private EaseType ease = EaseType.In;
        private LoopType loopType = LoopType.Restart;

        private bool paused;
        private bool started;
        private bool relative;
        private bool useUnscaledTime;

        private string group;

        private object id;
        private int loops;
        private int currentLoop;

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
            IsCompleted = true;
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
            IsCompleted = false;
            paused = false;
            started = false;
            playbackDirection = 1f;
            delayElapsed = 0f; 

            return this;
        }

        public Tween Complete()
        {
            onComplete?.Invoke();
            pendingTween?.UnPend();
            IsCompleted = true;

            return this;
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
                IsCompleted = true;
                return;
            }

            if (paused || IsCompleted)
                return;

            float dt = useUnscaledTime ? unScaledDelta : delta;

            if (delayElapsed < Delay)
            {
                delayElapsed += dt;
                return;
            }

            Elapsed += dt * TimeScale * playbackDirection;
            Elapsed = Mathf.Clamp(Elapsed, 0f, Duration);

            if (!started)
            {
                started = true;
                interpolator?.OnStart();
                onStart?.Invoke();
            }

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

            transition = TransitionType.Linear;
            ease = EaseType.In;

            IsCompleted = false;
            paused = false;
            started = false;
            relative = false;

            Elapsed = 0f;
            delayElapsed = 0f;
            Delay = 0f;

            onLoop = null;
            onComplete = null;
            onUpdate = null;
            onStart = null;

            loops = 0;
            currentLoop = 0;

            useUnscaledTime = false;
            customCurve = null;
            pendingTween = null;

            TimeScale = 1f;
            playbackDirection = 1f;
        }
        #endregion

        internal void Pend()
        {
            Pop();
        }

        internal void UnPend()
        {
            Restart();
            FlowTween.AddActiveTween(this);
        }

        internal void Pop()
        {
            FlowTween.RemoveActiveTween(this);
        }
    }
}
