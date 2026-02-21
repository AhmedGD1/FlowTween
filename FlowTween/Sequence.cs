using UnityEngine;
using System.Collections.Generic;
using System;

namespace FlT
{
    public class Sequence
    {
        internal struct SequenceStep
        {
            public Tween tween;
            public float startTime;

            internal SequenceStep(Tween tween, float startTime)
            {
                this.tween = tween;
                this.startTime = startTime;
            }

            public void AddTime(float time) => startTime += time;
        }

        public bool IsCompleted => completed;
        public bool IsPaused => paused;

        private readonly List<SequenceStep> steps = new();
        private readonly List<(float time, Action callback)> callbacks = new();

        private bool completed;
        private bool paused;

        private float currentTime;
        private float elapsed;
        private float totalDuration;

        private int activeStepIndex;
        private int callbackIndex;

        //-1 = infinite loops
        private int loops;
        private int currentLoop;

        private Action onComplete;
        private Action<int> onLoop;

        private static readonly Comparison<SequenceStep> stepComparison = (a, b) => a.startTime.CompareTo(b.startTime);
        private static readonly Comparison<(float time, Action callback)> callbackComparison = (a, b) => a.time.CompareTo(b.time);
        
        internal void Update(float dt)
        {
            if (completed || paused) return;

            elapsed += dt;

            TickSteps();
            AdvanceStepCursor();
            FireCallbacks();

            if (elapsed >= totalDuration)
            {
                if (loops == -1 || currentLoop++ < loops)
                {
                    RestartLoop();
                    return;
                }

                onComplete?.Invoke();
                completed = true;
            }
        }

        private void RestartLoop()
        {
            elapsed = 0f;
            activeStepIndex = 0;
            callbackIndex = 0;

            for (int i = 0; i < steps.Count; i++)
                steps[i].tween.Restart();

            onLoop?.Invoke(currentLoop);
        }

        private void TickSteps()
        {
            for (int i = activeStepIndex; i < steps.Count; i++)
            {
                SequenceStep step = steps[i];

                if (elapsed < step.startTime) break;

                float tweenElapsed = elapsed - step.startTime;
                step.tween.Elapsed = Mathf.Min(tweenElapsed, step.tween.Duration);
                step.tween.Update(0f, 0f);
            }
        }

        private void AdvanceStepCursor()
        {
            while (activeStepIndex < steps.Count &&
                   elapsed >= steps[activeStepIndex].startTime + steps[activeStepIndex].tween.Duration)
            {
                activeStepIndex++;
            }
        }

        private void FireCallbacks()
        {
            while (callbackIndex < callbacks.Count && elapsed >= callbacks[callbackIndex].time)
            {
                callbacks[callbackIndex].callback.Invoke();
                callbackIndex++;
            }
        }

        public Sequence Play()
        {
            FlowTween.RegisterSequence(this);
            return this;
        }

        public void Kill()
        {
            completed = true;
        }

        public Sequence Pause()
        {
            paused = true;
            return this;
        }

        public Sequence Resume()
        {
            paused = false;
            return this;
        }

        /// <summary>Add a tween after all existing steps.</summary>
        public Sequence Append(Tween tween)
        {
            AbsorbTween(tween, startTime: currentTime);
            currentTime  += tween.Duration;
            totalDuration = currentTime;
            SortSteps();
            return this;
        }

        /// <summary>Run a tween in parallel with the most recently appended step.</summary>
        public Sequence Join(Tween tween)
        {
            if (steps.Count == 0)
            {
                Debug.LogWarning("FlowTween.Sequence: Join called on empty sequence — using Append instead.");
                return Append(tween);
            }

            AbsorbTween(tween, startTime: joinAnchor);
            totalDuration = Mathf.Max(totalDuration, joinAnchor + tween.Duration);
            SortSteps();
            return this;
        }

        /// <summary>Insert a blank gap at the current write-head.</summary>
        public Sequence AppendInterval(float duration)
        {
            currentTime  += Mathf.Max(0f, duration);
            totalDuration = currentTime;
            return this;
        }

        /// <summary>Place a tween at an explicit time, independent of the write-head.</summary>
        public Sequence Insert(float time, Tween tween)
        {
            AbsorbTween(tween, startTime: time);
            totalDuration = Mathf.Max(totalDuration, time + tween.Duration);
            SortSteps();
            return this;
        }

        /// <summary>Insert a tween before all existing steps, shifting everything forward.</summary>
        public Sequence Prepend(Tween tween)
        {
            float shift = tween.Duration;

            // Time Addition using for loop since sequence step is a struct
            for (int i = 0; i < steps.Count; i++)
            {
                SequenceStep step = steps[i];
                step.AddTime(shift);
                steps[i] = step;
            }

            ShiftCallbacks(shift);

            AbsorbTween(tween, startTime: 0f);
            currentTime   += shift;
            totalDuration += shift;
            SortSteps();
            return this;
        }

        public Sequence AppendCallback(Action callback)
        {
            callbacks.Add((currentTime, callback));
            callbacks.Sort(callbackComparison);
            return this;
        }

        public Sequence SetLoops(int count)
        {
            loops = count;
            return this;
        }

        public Sequence SetRelative()
        {
            for (int i = 0; i < steps.Count; i++)
                steps[i].tween.SetRelative();
            return this;
        }

        public Sequence OnComplete(Action callback)
        {
            onComplete = callback;
            return this;
        }

        public Sequence OnLoop(Action<int> callback)
        {
            onLoop = callback;
            return this;
        }

        internal void ResetData()
        {
            for (int i = 0; i < steps.Count; i++)
                FlowTween.ReturnToPool(steps[i].tween);

            steps.Clear();
            callbacks.Clear();

            completed       = false;
            paused          = false;
            onComplete      = null;
            onLoop          = null;
            totalDuration   = 0f;
            currentTime     = 0f;
            elapsed         = 0f;
            joinAnchor      = 0f;
            loops           = 0;
            currentLoop     = 0;
            activeStepIndex = 0;
            callbackIndex   = 0;
        }

        private float joinAnchor;

        private void AbsorbTween(Tween tween, float startTime)
        {
            tween.Pop();
            steps.Add(new SequenceStep(tween, startTime));
            joinAnchor = startTime;
        }

        private void SortSteps()
        {
            steps.Sort(stepComparison);
        }

        private void ShiftCallbacks(float shift)
        {
            for (int i = 0; i < callbacks.Count; i++)
                callbacks[i] = (callbacks[i].time + shift, callbacks[i].callback);
        }
    }
}