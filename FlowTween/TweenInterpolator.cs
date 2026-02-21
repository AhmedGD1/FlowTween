using System.Collections.Generic;
using UnityEngine;
using System;

namespace FlT
{
    public interface ITweenInterpolator
    {
        void OnStart();
        void OnTick(float t);
        void Reset();
        void ReturnToPool();
        bool TrySetFrom<T>(T value);
    }

    internal sealed class StructTweenInterpolator<TTarget, TValue, TInterp> : ITweenInterpolator
        where TTarget : UnityEngine.Object
        where TInterp : struct, IPropertyInterpolator<TTarget, TValue>
    {
        private TTarget target;
        private TValue  from;
        private TValue  fromOverride;
        private TValue  to;
        private TInterp interp; 
        private Tween owner;

        private bool hasFromOverride;
                                
        public void Setup(TTarget target, TValue to, Tween owner)
        {
            this.target = target;
            this.to     = to;
            this.interp = default; 
            this.owner = owner;
        }

        public void OnStart()
        {
            from = hasFromOverride ? fromOverride : interp.GetValue(target);

            if (owner.IsRelative)
                to = Add(from, to);
        }

        public void OnTick(float t) => interp.SetValue(target, from, to, t);

        public bool TrySetFrom<TVal>(TVal value)
        {
            if (value is TValue typed)
            {
                hasFromOverride = true;
                fromOverride    = typed;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            owner = null;
            target = null;
            from   = default;
            to     = default;
            interp = default; 
            fromOverride = default;
            hasFromOverride = false;
        }

        public void ReturnToPool() 
        { 
            Reset(); 
            TypedPool.Push(this); 
        }

        private static readonly Stack<StructTweenInterpolator<TTarget, TValue, TInterp>> TypedPool = new();
        public static StructTweenInterpolator<TTarget, TValue, TInterp> Get()
            => TypedPool.Count > 0 ? TypedPool.Pop() : new();

        private static TValue Add(TValue a, TValue b)
        {
            if (a is Vector3 av && b is Vector3 bv) return (TValue)(object)(av + bv);
            if (a is Vector2 av2 && b is Vector2 bv2) return (TValue)(object)(av2 + bv2);
            if (a is float af && b is float bf) return (TValue)(object)(af + bf);
            if (a is Color ac && b is Color bc) return (TValue)(object)(ac + bc);
            if (a is Quaternion aq && b is Quaternion bq) return (TValue)(object)(aq * bq);

            Debug.LogWarning($"FlowTween: SetRelative not supported for type {typeof(TValue)}");
            return b;
        }
    }

    internal sealed class FloatInterpolator : ITweenInterpolator
    {
        private float from;
        private float to;
        private Action<float> onUpdate;

        public void Setup(float from, float to, Action<float> onUpdate)
        {
            this.from     = from;
            this.to       = to;
            this.onUpdate = onUpdate;
        }

        public void OnStart() { }

        public void OnTick(float t)
        {
            onUpdate?.Invoke(Mathf.LerpUnclamped(from, to, t));
        }

        public void Reset()
        {
            from     = default;
            to       = default;
            onUpdate = null;
        }

        public bool TrySetFrom<T>(T value) => false;

        private static readonly Stack<FloatInterpolator> pool = new();

        public static FloatInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public void ReturnToPool() { Reset(); pool.Push(this); }
    }

    internal sealed class IntInterpolator : ITweenInterpolator
    {
        private int from;
        private int to;
        private Action<int> onUpdate;

        public void Setup(int from, int to, Action<int> onUpdate)
        {
            this.from     = from;
            this.to       = to;
            this.onUpdate = onUpdate;
        }

        public void OnStart() { }

        public void OnTick(float t)
        {
            onUpdate?.Invoke(Mathf.RoundToInt(Mathf.LerpUnclamped(from, to, t)));
        }

        public void Reset()
        {
            from     = default;
            to       = default;
            onUpdate = null;
        }

        public bool TrySetFrom<T>(T value) => false;

        private static readonly Stack<IntInterpolator> pool = new();

        public static IntInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public void ReturnToPool() { Reset(); pool.Push(this); }
    }

    internal sealed class Vector2Interpolator : ITweenInterpolator
    {
        private Vector2 from;
        private Vector2 to;
        private Action<Vector2> onUpdate;

        public void Setup(Vector2 from, Vector2 to, Action<Vector2> onUpdate)
        {
            this.from     = from;
            this.to       = to;
            this.onUpdate = onUpdate;
        }

        public void OnStart() { }

        public void OnTick(float t)
        {
            onUpdate?.Invoke(Vector2.LerpUnclamped(from, to, t));
        }

        public void Reset()
        {
            from     = default;
            to       = default;
            onUpdate = null;
        }

        public bool TrySetFrom<T>(T value) => false;

        private static readonly Stack<Vector2Interpolator> pool = new();

        public static Vector2Interpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public void ReturnToPool() { Reset(); pool.Push(this); }
    }

    internal sealed class Vector3Interpolator : ITweenInterpolator
    {
        private Vector3 from;
        private Vector3 to;
        private Action<Vector3> onUpdate;

        public void Setup(Vector3 from, Vector3 to, Action<Vector3> onUpdate)
        {
            this.from     = from;
            this.to       = to;
            this.onUpdate = onUpdate;
        }

        public void OnStart() { }

        public void OnTick(float t)
        {
            onUpdate?.Invoke(Vector3.LerpUnclamped(from, to, t));
        }

        public void Reset()
        {
            from     = default;
            to       = default;
            onUpdate = null;
        }

        public bool TrySetFrom<T>(T value) => false;

        private static readonly Stack<Vector3Interpolator> pool = new();

        public static Vector3Interpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public void ReturnToPool() { Reset(); pool.Push(this); }
    }

    internal sealed class ColorInterpolator : ITweenInterpolator
    {
        private Color from;
        private Color to;
        private Action<Color> onUpdate;

        public void Setup(Color from, Color to, Action<Color> onUpdate)
        {
            this.from     = from;
            this.to       = to;
            this.onUpdate = onUpdate;
        }

        public void OnStart() { }

        public void OnTick(float t)
        {
            onUpdate?.Invoke(Color.LerpUnclamped(from, to, t));
        }

        public void Reset()
        {
            from     = default;
            to       = default;
            onUpdate = null;
        }

        public bool TrySetFrom<T>(T value) => false;

        private static readonly Stack<ColorInterpolator> pool = new();

        public static ColorInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public void ReturnToPool() { Reset(); pool.Push(this); }
    }
}
