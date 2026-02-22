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
        bool TryGetDistance(out float distance);
        /// <summary>Human-readable description of the interpolated value type and current state.</summary>
        string DbgValueDescription { get; }
    }

    /// <summary>Editor-only: per-type pool size registry so the debugger can display them.</summary>
    public static class InterpolatorPoolStats
    {
        private static readonly Dictionary<string, Func<int>> _sizeGetters = new();

        public static void Register(string name, Func<int> sizeGetter) =>
            _sizeGetters[name] = sizeGetter;

        public static IReadOnlyDictionary<string, Func<int>> All => _sizeGetters;
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

        public string DbgValueDescription =>
            $"{typeof(TValue).Name}  {FormatVal(from)} → {FormatVal(to)}";

        private static string FormatVal(TValue v) => v switch
        {
            Vector3    vv => $"({vv.x:0.00},{vv.y:0.00},{vv.z:0.00})",
            Vector2    vv => $"({vv.x:0.00},{vv.y:0.00})",
            Color      c  => $"rgba({c.r:0.00},{c.g:0.00},{c.b:0.00},{c.a:0.00})",
            Quaternion q  => $"euler({q.eulerAngles:0.0})",
            float      f  => f.ToString("0.0000"),
            _             => v?.ToString() ?? "null"
        };
                                
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

        static StructTweenInterpolator()
        {
            // Register with the central stats so the debugger can enumerate all typed pools
            string key = $"Struct<{typeof(TTarget).Name},{typeof(TValue).Name}>";
            InterpolatorPoolStats.Register(key, () => TypedPool.Count);
        }

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

        public bool TryGetDistance(out float distance)
        {
            if (from is Vector3 fv && to is Vector3 tv) { distance = Vector3.Distance(fv, tv); return true; }
            if (from is Vector2 fv2 && to is Vector2 tv2) { distance = Vector2.Distance(fv2, tv2); return true; }
            if (from is float ff && to is float tf) { distance = Mathf.Abs(ff - tf); return true; }
            
            distance = 0f;
            return false;
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

        public string DbgValueDescription => $"float  {from:0.0000} → {to:0.0000}";
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

        static FloatInterpolator() => InterpolatorPoolStats.Register("FloatInterpolator", () => pool.Count);

        public static FloatInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public void ReturnToPool() { Reset(); pool.Push(this); }

        public bool TryGetDistance(out float distance)
        {
            distance = Mathf.Abs(to - from);
            return true;
        }
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

        public string DbgValueDescription => $"int  {from} → {to}";
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

        static IntInterpolator() => InterpolatorPoolStats.Register("IntInterpolator", () => pool.Count);

        public static IntInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public void ReturnToPool() { Reset(); pool.Push(this); }

        public bool TryGetDistance(out float distance)
        {
            distance = Mathf.Abs(to - from);
            return true;
        }
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

        public string DbgValueDescription => $"Vector2  ({from.x:0.00},{from.y:0.00}) → ({to.x:0.00},{to.y:0.00})";
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

        static Vector2Interpolator() => InterpolatorPoolStats.Register("Vector2Interpolator", () => pool.Count);

        public static Vector2Interpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public void ReturnToPool() { Reset(); pool.Push(this); }

        public bool TryGetDistance(out float distance)
        {
            distance = Vector2.Distance(from, to);
            return true;
        }
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

        public string DbgValueDescription => $"Vector3  ({from.x:0.00},{from.y:0.00},{from.z:0.00}) → ({to.x:0.00},{to.y:0.00},{to.z:0.00})";
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

        static Vector3Interpolator() => InterpolatorPoolStats.Register("Vector3Interpolator", () => pool.Count);

        public static Vector3Interpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public void ReturnToPool() { Reset(); pool.Push(this); }

        public bool TryGetDistance(out float distance)
        {
            distance = Vector3.Distance(from, to);
            return true;
        }
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

        public string DbgValueDescription =>
            $"Color  rgba({from.r:0.00},{from.g:0.00},{from.b:0.00},{from.a:0.00}) → rgba({to.r:0.00},{to.g:0.00},{to.b:0.00},{to.a:0.00})";

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

        static ColorInterpolator() => InterpolatorPoolStats.Register("ColorInterpolator", () => pool.Count);

        public static ColorInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public void ReturnToPool() { Reset(); pool.Push(this); }

        public bool TryGetDistance(out float distance)
        {
            distance  = 0f;
            return true;
        }
    }
}