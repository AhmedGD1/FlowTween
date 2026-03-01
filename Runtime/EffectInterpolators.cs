using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FlT
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    internal abstract class EffectInterpolatorBase : ITweenInterpolator
    {
        public virtual string DbgValueDescription => GetType().Name;
        public virtual void OnStart() { }
        public abstract void OnTick(float t);
        public abstract void OnComplete();
        public abstract void Reset();
        public abstract void ReturnToPool();
        public virtual bool TrySetFrom<T>(T value) => false;
        public virtual bool TryGetDistance(out float distance) { distance = 0f; return false; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SpriteRenderer – Gradient
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class SpriteGradientInterpolator : EffectInterpolatorBase
    {
        private SpriteRenderer renderer;
        private Gradient       gradient;

        public void Setup(SpriteRenderer renderer, Gradient gradient)
        {
            this.renderer = renderer;
            this.gradient = gradient;
        }

        public override void OnTick(float t)    => renderer.color = gradient.Evaluate(t);
        public override void OnComplete()       { }
        public override void Reset()            { renderer = null; gradient = null; }

        private static readonly Stack<SpriteGradientInterpolator> pool = new();
        static SpriteGradientInterpolator() => InterpolatorPoolStats.Register(nameof(SpriteGradientInterpolator), () => pool.Count);
        public static SpriteGradientInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class GradientInterpolator : EffectInterpolatorBase
    {
        private Graphic  graphic;
        private Gradient gradient;

        public void Setup(Graphic graphic, Gradient gradient)
        {
            this.graphic  = graphic;
            this.gradient = gradient;
        }

        public override void OnTick(float t)    => graphic.color = gradient.Evaluate(t);
        public override void OnComplete()       { }
        public override void Reset()            { graphic = null; gradient = null; }

        private static readonly Stack<GradientInterpolator> pool = new();
        static GradientInterpolator() => InterpolatorPoolStats.Register(nameof(GradientInterpolator), () => pool.Count);
        public static GradientInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TMP – Counter (float + format string)
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class TmpCounterFloatInterpolator : EffectInterpolatorBase
    {
        private TMPro.TMP_Text text;
        private float          from;
        private float          to;
        private string         format;

        public void Setup(TMPro.TMP_Text text, float from, float to, string format)
        {
            this.text   = text;
            this.from   = from;
            this.to     = to;
            this.format = format;
        }

        public override void OnTick(float t)    => text.text = Mathf.LerpUnclamped(from, to, t).ToString(format);
        public override void OnComplete()       { }
        public override void Reset()            { text = null; format = null; from = to = 0f; }

        public override bool TryGetDistance(out float distance) { distance = Mathf.Abs(to - from); return true; }

        private static readonly Stack<TmpCounterFloatInterpolator> pool = new();
        static TmpCounterFloatInterpolator() => InterpolatorPoolStats.Register(nameof(TmpCounterFloatInterpolator), () => pool.Count);
        public static TmpCounterFloatInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TMP – Counter (float + Func<float,string> formatter)
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class TmpCounterFormatterInterpolator : EffectInterpolatorBase
    {
        private TMPro.TMP_Text   text;
        private float            from;
        private float            to;
        private Func<float, string> formatter;

        public void Setup(TMPro.TMP_Text text, float from, float to, Func<float, string> formatter)
        {
            this.text      = text;
            this.from      = from;
            this.to        = to;
            this.formatter = formatter;
        }

        public override void OnTick(float t)    => text.text = formatter(Mathf.LerpUnclamped(from, to, t));
        public override void OnComplete()       { }
        public override void Reset()            { text = null; formatter = null; from = to = 0f; }

        public override bool TryGetDistance(out float distance) { distance = Mathf.Abs(to - from); return true; }

        private static readonly Stack<TmpCounterFormatterInterpolator> pool = new();
        static TmpCounterFormatterInterpolator() => InterpolatorPoolStats.Register(nameof(TmpCounterFormatterInterpolator), () => pool.Count);
        public static TmpCounterFormatterInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TMP – Counter (int)
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class TmpCounterIntInterpolator : EffectInterpolatorBase
    {
        private TMPro.TMP_Text text;
        private int            from;
        private int            to;

        public void Setup(TMPro.TMP_Text text, int from, int to)
        {
            this.text = text;
            this.from = from;
            this.to   = to;
        }

        public override void OnTick(float t)    => text.text = Mathf.RoundToInt(Mathf.LerpUnclamped(from, to, t)).ToString();
        public override void OnComplete()       { }
        public override void Reset()            { text = null; from = to = 0; }

        private static readonly Stack<TmpCounterIntInterpolator> pool = new();
        static TmpCounterIntInterpolator() => InterpolatorPoolStats.Register(nameof(TmpCounterIntInterpolator), () => pool.Count);
        public static TmpCounterIntInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TMP – Character Color
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class TmpCharacterColorInterpolator : EffectInterpolatorBase
    {
        private TMPro.TMP_Text text;
        private Color          from;
        private Color          to;

        public void Setup(TMPro.TMP_Text text, Color to)
        {
            this.text = text;
            this.to   = to;
        }

        public override void OnStart()       => from = text.color;
        public override void OnTick(float t)
        {
            text.color = Color.LerpUnclamped(from, to, t);
            text.ForceMeshUpdate();
        }
        public override void OnComplete()    { }
        public override void Reset()         { text = null; from = to = default; }

        private static readonly Stack<TmpCharacterColorInterpolator> pool = new();
        static TmpCharacterColorInterpolator() => InterpolatorPoolStats.Register(nameof(TmpCharacterColorInterpolator), () => pool.Count);
        public static TmpCharacterColorInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TMP – Typewriter
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class TmpTypewriterInterpolator : EffectInterpolatorBase
    {
        private TMPro.TMP_Text text;
        private int            totalChars;

        public void Setup(TMPro.TMP_Text text, int totalChars)
        {
            this.text       = text;
            this.totalChars = totalChars;
        }

        public override void OnTick(float t)    => text.maxVisibleCharacters = Mathf.RoundToInt(Mathf.LerpUnclamped(0, totalChars, t));
        public override void OnComplete()       => text.maxVisibleCharacters = totalChars;
        public override void Reset()            { text = null; totalChars = 0; }

        private static readonly Stack<TmpTypewriterInterpolator> pool = new();
        static TmpTypewriterInterpolator() => InterpolatorPoolStats.Register(nameof(TmpTypewriterInterpolator), () => pool.Count);
        public static TmpTypewriterInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Blink – SpriteRenderer
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class SpriteBlinkInterpolator : EffectInterpolatorBase
    {
        private SpriteRenderer renderer;
        private int            blinks;
        private bool           endVisible;

        public void Setup(SpriteRenderer renderer, int blinks, bool endVisible)
        {
            this.renderer   = renderer;
            this.blinks     = blinks;
            this.endVisible = endVisible;
        }

        public override void OnTick(float t)
        {
            if (renderer == null || !renderer) return;
            renderer.enabled = Mathf.RoundToInt(Mathf.LerpUnclamped(0, blinks, t)) % 2 == 0;
        }
        public override void OnComplete()
        {
            if (renderer == null || !renderer) return;
            renderer.enabled = endVisible;
        }
        public override void Reset() { renderer = null; blinks = 0; endVisible = true; }

        private static readonly Stack<SpriteBlinkInterpolator> pool = new();
        static SpriteBlinkInterpolator() => InterpolatorPoolStats.Register(nameof(SpriteBlinkInterpolator), () => pool.Count);
        public static SpriteBlinkInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Blink – CanvasGroup
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class CanvasGroupBlinkInterpolator : EffectInterpolatorBase
    {
        private CanvasGroup canvasGroup;
        private int         blinks;
        private bool        endVisible;

        public void Setup(CanvasGroup canvasGroup, int blinks, bool endVisible)
        {
            this.canvasGroup = canvasGroup;
            this.blinks      = blinks;
            this.endVisible  = endVisible;
        }

        public override void OnTick(float t)
        {
            if (canvasGroup == null || !canvasGroup) return;
            canvasGroup.enabled = Mathf.RoundToInt(Mathf.LerpUnclamped(0, blinks, t)) % 2 == 0;
        }
        public override void OnComplete()
        {
            if (canvasGroup == null || !canvasGroup) return;
            canvasGroup.enabled = endVisible;
        }
        public override void Reset() { canvasGroup = null; blinks = 0; endVisible = true; }

        private static readonly Stack<CanvasGroupBlinkInterpolator> pool = new();
        static CanvasGroupBlinkInterpolator() => InterpolatorPoolStats.Register(nameof(CanvasGroupBlinkInterpolator), () => pool.Count);
        public static CanvasGroupBlinkInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Jello
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class JelloInterpolator : EffectInterpolatorBase
    {
        private Transform transform;
        private Vector3   startScale;
        private float     intensity;
        private float     frequency;

        public void Setup(Transform transform, Vector3 startScale, float intensity, float frequency)
        {
            this.transform  = transform;
            this.startScale = startScale;
            this.intensity  = intensity;
            this.frequency  = frequency;
        }

        public override void OnTick(float t)
        {
            float decay = 1f - t;
            float wave  = Mathf.Sin(t * frequency * Mathf.PI * 2f) * intensity * decay;
            transform.localScale = new Vector3(startScale.x + wave, startScale.y - wave, startScale.z);
        }
        public override void OnComplete()    => transform.localScale = startScale;
        public override void Reset()         { transform = null; startScale = default; intensity = frequency = 0f; }

        private static readonly Stack<JelloInterpolator> pool = new();
        static JelloInterpolator() => InterpolatorPoolStats.Register(nameof(JelloInterpolator), () => pool.Count);
        public static JelloInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  WobbleRotate
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class WobbleRotateInterpolator : EffectInterpolatorBase
    {
        private Transform  transform;
        private Quaternion startRot;
        private float      strength;
        private float      frequency;

        public void Setup(Transform transform, Quaternion startRot, float strength, float frequency)
        {
            this.transform = transform;
            this.startRot  = startRot;
            this.strength  = strength;
            this.frequency = frequency;
        }

        public override void OnTick(float t)
        {
            float decay = 1f - t;
            float angle = Mathf.Sin(t * frequency * Mathf.PI * 2f) * strength * decay;
            transform.localRotation = startRot * Quaternion.Euler(0f, 0f, angle);
        }
        public override void OnComplete()    => transform.localRotation = startRot;
        public override void Reset()         { transform = null; startRot = default; strength = frequency = 0f; }

        private static readonly Stack<WobbleRotateInterpolator> pool = new();
        static WobbleRotateInterpolator() => InterpolatorPoolStats.Register(nameof(WobbleRotateInterpolator), () => pool.Count);
        public static WobbleRotateInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Float (bobbing)
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class FloatBobInterpolator : EffectInterpolatorBase
    {
        private Transform transform;
        private Vector3   origin;
        private float     amplitude;

        public void Setup(Transform transform, Vector3 origin, float amplitude)
        {
            this.transform = transform;
            this.origin    = origin;
            this.amplitude = amplitude;
        }

        public override void OnTick(float t)
        {
            float y = Mathf.Sin(t * Mathf.PI * 2f) * amplitude;
            transform.localPosition = new Vector3(origin.x, origin.y + y, origin.z);
        }
        public override void OnComplete()    => transform.localPosition = origin;
        public override void Reset()         { transform = null; origin = default; amplitude = 0f; }

        private static readonly Stack<FloatBobInterpolator> pool = new();
        static FloatBobInterpolator() => InterpolatorPoolStats.Register(nameof(FloatBobInterpolator), () => pool.Count);
        public static FloatBobInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Shake 2D (world position, XY)
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class Shake2DInterpolator : EffectInterpolatorBase
    {
        private Transform transform;
        private Vector3   startPos;
        private float     strength;
        private float     frequency;
        private float     seedX;
        private float     seedY;
        private bool      local;

        public void Setup(Transform transform, Vector3 startPos, float strength, float frequency, float seedX, float seedY, bool local)
        {
            this.transform = transform;
            this.startPos  = startPos;
            this.strength  = strength;
            this.frequency = frequency;
            this.seedX     = seedX;
            this.seedY     = seedY;
            this.local     = local;
        }

        public override void OnTick(float t)
        {
            float damper    = Mathf.Lerp(strength, 0f, t);
            float noiseTime = t * frequency;
            float x = (Mathf.PerlinNoise(noiseTime, seedX) * 2f - 1f) * damper;
            float y = (Mathf.PerlinNoise(noiseTime, seedY) * 2f - 1f) * damper;
            Vector3 pos = new Vector3(startPos.x + x, startPos.y + y, startPos.z);
            if (local) transform.localPosition = pos;
            else       transform.position      = pos;
        }
        public override void OnComplete()
        {
            if (local) transform.localPosition = startPos;
            else       transform.position      = startPos;
        }
        public override void Reset() { transform = null; startPos = default; strength = frequency = seedX = seedY = 0f; local = false; }

        private static readonly Stack<Shake2DInterpolator> pool = new();
        static Shake2DInterpolator() => InterpolatorPoolStats.Register(nameof(Shake2DInterpolator), () => pool.Count);
        public static Shake2DInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Shake 3D (position, XYZ)
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class Shake3DInterpolator : EffectInterpolatorBase
    {
        private Transform transform;
        private Vector3   startPos;
        private float     strength;
        private float     frequency;
        private float     seedX;
        private float     seedY;
        private float     seedZ;
        private bool      local;

        public void Setup(Transform transform, Vector3 startPos, float strength, float frequency,
                          float seedX, float seedY, float seedZ, bool local)
        {
            this.transform = transform;
            this.startPos  = startPos;
            this.strength  = strength;
            this.frequency = frequency;
            this.seedX     = seedX;
            this.seedY     = seedY;
            this.seedZ     = seedZ;
            this.local     = local;
        }

        public override void OnTick(float t)
        {
            float damper    = Mathf.Lerp(strength, 0f, t);
            float noiseTime = t * frequency;
            float x = (Mathf.PerlinNoise(noiseTime, seedX) * 2f - 1f) * damper;
            float y = (Mathf.PerlinNoise(noiseTime, seedY) * 2f - 1f) * damper;
            float z = (Mathf.PerlinNoise(noiseTime, seedZ) * 2f - 1f) * damper;
            Vector3 pos = startPos + new Vector3(x, y, z);
            if (local) transform.localPosition = pos;
            else       transform.position      = pos;
        }
        public override void OnComplete()
        {
            if (local) transform.localPosition = startPos;
            else       transform.position      = startPos;
        }
        public override void Reset() { transform = null; startPos = default; strength = frequency = seedX = seedY = seedZ = 0f; local = false; }

        private static readonly Stack<Shake3DInterpolator> pool = new();
        static Shake3DInterpolator() => InterpolatorPoolStats.Register(nameof(Shake3DInterpolator), () => pool.Count);
        public static Shake3DInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Shake Rotation 3D (all axes)
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class ShakeRotation3DInterpolator : EffectInterpolatorBase
    {
        private Transform  transform;
        private Quaternion startRot;
        private float      strength;
        private float      frequency;
        private float      seedX;
        private float      seedY;
        private float      seedZ;

        public void Setup(Transform transform, Quaternion startRot, float strength, float frequency,
                          float seedX, float seedY, float seedZ)
        {
            this.transform = transform;
            this.startRot  = startRot;
            this.strength  = strength;
            this.frequency = frequency;
            this.seedX     = seedX;
            this.seedY     = seedY;
            this.seedZ     = seedZ;
        }

        public override void OnTick(float t)
        {
            float damper    = Mathf.Lerp(strength, 0f, t);
            float noiseTime = t * frequency;
            float x = (Mathf.PerlinNoise(noiseTime, seedX) * 2f - 1f) * damper;
            float y = (Mathf.PerlinNoise(noiseTime, seedY) * 2f - 1f) * damper;
            float z = (Mathf.PerlinNoise(noiseTime, seedZ) * 2f - 1f) * damper;
            transform.localRotation = startRot * Quaternion.Euler(x, y, z);
        }
        public override void OnComplete()    => transform.localRotation = startRot;
        public override void Reset()         { transform = null; startRot = default; strength = frequency = seedX = seedY = seedZ = 0f; }

        private static readonly Stack<ShakeRotation3DInterpolator> pool = new();
        static ShakeRotation3DInterpolator() => InterpolatorPoolStats.Register(nameof(ShakeRotation3DInterpolator), () => pool.Count);
        public static ShakeRotation3DInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Shake Rotation 2D (Z axis only)
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class ShakeRotation2DInterpolator : EffectInterpolatorBase
    {
        private Transform  transform;
        private Quaternion startRot;
        private float      strength;
        private float      frequency;
        private float      seed;

        public void Setup(Transform transform, Quaternion startRot, float strength, float frequency, float seed)
        {
            this.transform = transform;
            this.startRot  = startRot;
            this.strength  = strength;
            this.frequency = frequency;
            this.seed      = seed;
        }

        public override void OnTick(float t)
        {
            float damper = Mathf.Lerp(strength, 0f, t);
            float z = (Mathf.PerlinNoise(t * frequency, seed) * 2f - 1f) * damper;
            transform.localRotation = startRot * Quaternion.Euler(0f, 0f, z);
        }
        public override void OnComplete()    => transform.localRotation = startRot;
        public override void Reset()         { transform = null; startRot = default; strength = frequency = seed = 0f; }

        private static readonly Stack<ShakeRotation2DInterpolator> pool = new();
        static ShakeRotation2DInterpolator() => InterpolatorPoolStats.Register(nameof(ShakeRotation2DInterpolator), () => pool.Count);
        public static ShakeRotation2DInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Shake Rotation Axis
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class ShakeRotationAxisInterpolator : EffectInterpolatorBase
    {
        private Transform  transform;
        private Quaternion startRot;
        private Vector3    axis;
        private float      strength;
        private float      frequency;
        private float      seed;

        public void Setup(Transform transform, Quaternion startRot, Vector3 axis, float strength, float frequency, float seed)
        {
            this.transform = transform;
            this.startRot  = startRot;
            this.axis      = axis;
            this.strength  = strength;
            this.frequency = frequency;
            this.seed      = seed;
        }

        public override void OnTick(float t)
        {
            float damper = Mathf.Lerp(strength, 0f, t);
            float angle  = (Mathf.PerlinNoise(t * frequency, seed) * 2f - 1f) * damper;
            transform.localRotation = startRot * Quaternion.AngleAxis(angle, axis);
        }
        public override void OnComplete()    => transform.localRotation = startRot;
        public override void Reset()         { transform = null; startRot = default; axis = default; strength = frequency = seed = 0f; }

        private static readonly Stack<ShakeRotationAxisInterpolator> pool = new();
        static ShakeRotationAxisInterpolator() => InterpolatorPoolStats.Register(nameof(ShakeRotationAxisInterpolator), () => pool.Count);
        public static ShakeRotationAxisInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Shake Scale
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class ShakeScaleInterpolator : EffectInterpolatorBase
    {
        private Transform transform;
        private Vector3   startScale;
        private float     strength;
        private float     frequency;
        private float     seedX;
        private float     seedY;
        private float     seedZ;

        public void Setup(Transform transform, Vector3 startScale, float strength, float frequency,
                        float seedX, float seedY, float seedZ)
        {
            this.transform  = transform;
            this.startScale = startScale;
            this.strength   = strength;
            this.frequency  = frequency;
            this.seedX      = seedX;
            this.seedY      = seedY;
            this.seedZ      = seedZ;
        }

        public override void OnTick(float t)
        {
            float damper    = Mathf.Lerp(strength, 0f, t);
            float noiseTime = t * frequency;
            float x = (Mathf.PerlinNoise(noiseTime, seedX) * 2f - 1f) * damper;
            float y = (Mathf.PerlinNoise(noiseTime, seedY) * 2f - 1f) * damper;
            float z = (Mathf.PerlinNoise(noiseTime, seedZ) * 2f - 1f) * damper;
            transform.localScale = startScale + new Vector3(x, y, z);
        }

        public override void OnComplete()    => transform.localScale = startScale;
        public override void Reset()         { transform = null; startScale = default; strength = frequency = seedX = seedY = seedZ = 0f; }

        private static readonly Stack<ShakeScaleInterpolator> pool = new();
        static ShakeScaleInterpolator() => InterpolatorPoolStats.Register(nameof(ShakeScaleInterpolator), () => pool.Count);
        public static ShakeScaleInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Punch Position 2D
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class PunchPosition2DInterpolator : EffectInterpolatorBase
    {
        private Transform transform;
        private Vector3   startPosition;
        private Vector3   punch3D;
        private int       vibrato;
        private float     elasticity;

        public void Setup(Transform transform, Vector3 startPosition, Vector3 punch3D, int vibrato, float elasticity)
        {
            this.transform     = transform;
            this.startPosition = startPosition;
            this.punch3D       = punch3D;
            this.vibrato       = vibrato;
            this.elasticity    = elasticity;
        }

        public override void OnTick(float t)
        {
            float wave = Mathf.Sin(vibrato * Mathf.PI * t) * (1f - t);
            transform.position = startPosition + punch3D * (wave * elasticity);
        }
        public override void OnComplete()    => transform.position = startPosition;
        public override void Reset()         { transform = null; startPosition = punch3D = default; vibrato = 0; elasticity = 0f; }

        private static readonly Stack<PunchPosition2DInterpolator> pool = new();
        static PunchPosition2DInterpolator() => InterpolatorPoolStats.Register(nameof(PunchPosition2DInterpolator), () => pool.Count);
        public static PunchPosition2DInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Punch Scale 2D (uniform XY)
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class PunchScale2DInterpolator : EffectInterpolatorBase
    {
        private Transform transform;
        private Vector3   startScale;
        private float     punch;
        private int       vibrato;
        private float     elasticity;

        public void Setup(Transform transform, Vector3 startScale, float punch, int vibrato, float elasticity)
        {
            this.transform  = transform;
            this.startScale = startScale;
            this.punch      = punch;
            this.vibrato    = vibrato;
            this.elasticity = elasticity;
        }

        public override void OnTick(float t)
        {
            float wave   = Mathf.Sin(vibrato * Mathf.PI * t) * (1f - t);
            float offset = wave * punch * elasticity;
            transform.localScale = new Vector3(startScale.x + offset, startScale.y + offset, startScale.z);
        }
        public override void OnComplete()    => transform.localScale = startScale;
        public override void Reset()         { transform = null; startScale = default; punch = elasticity = 0f; vibrato = 0; }

        private static readonly Stack<PunchScale2DInterpolator> pool = new();
        static PunchScale2DInterpolator() => InterpolatorPoolStats.Register(nameof(PunchScale2DInterpolator), () => pool.Count);
        public static PunchScale2DInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Punch Position 3D
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class PunchPosition3DInterpolator : EffectInterpolatorBase
    {
        private Transform transform;
        private Vector3   startPosition;
        private Vector3   punch;
        private int       vibrato;
        private float     elasticity;

        public void Setup(Transform transform, Vector3 startPosition, Vector3 punch, int vibrato, float elasticity)
        {
            this.transform     = transform;
            this.startPosition = startPosition;
            this.punch         = punch;
            this.vibrato       = vibrato;
            this.elasticity    = elasticity;
        }

        public override void OnTick(float t)
        {
            float wave = Mathf.Sin(vibrato * Mathf.PI * t) * (1f - t);
            transform.position = startPosition + punch * (wave * elasticity);
        }
        public override void OnComplete()    => transform.position = startPosition;
        public override void Reset()         { transform = null; startPosition = punch = default; vibrato = 0; elasticity = 0f; }

        private static readonly Stack<PunchPosition3DInterpolator> pool = new();
        static PunchPosition3DInterpolator() => InterpolatorPoolStats.Register(nameof(PunchPosition3DInterpolator), () => pool.Count);
        public static PunchPosition3DInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Punch Scale 3D (per-axis)
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class PunchScale3DInterpolator : EffectInterpolatorBase
    {
        private Transform transform;
        private Vector3   startScale;
        private Vector3   punch;
        private int       vibrato;
        private float     elasticity;

        public void Setup(Transform transform, Vector3 startScale, Vector3 punch, int vibrato, float elasticity)
        {
            this.transform  = transform;
            this.startScale = startScale;
            this.punch      = punch;
            this.vibrato    = vibrato;
            this.elasticity = elasticity;
        }

        public override void OnTick(float t)
        {
            float wave = Mathf.Sin(vibrato * Mathf.PI * t) * (1f - t);
            transform.localScale = startScale + punch * (wave * elasticity);
        }
        public override void OnComplete()    => transform.localScale = startScale;
        public override void Reset()         { transform = null; startScale = punch = default; vibrato = 0; elasticity = 0f; }

        private static readonly Stack<PunchScale3DInterpolator> pool = new();
        static PunchScale3DInterpolator() => InterpolatorPoolStats.Register(nameof(PunchScale3DInterpolator), () => pool.Count);
        public static PunchScale3DInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Flow Path (world & local)
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class FlowPathInterpolator : EffectInterpolatorBase
    {
        private Transform transform;
        private Vector3[] points;
        private Vector3   startPos;
        private bool      closedLoop;
        private bool      orientToPath;
        private bool      local;

        public void Setup(Transform transform, Vector3[] points, Vector3 startPos,
                          bool closedLoop, bool orientToPath, bool local)
        {
            this.transform    = transform;
            this.points       = points;
            this.startPos     = startPos;
            this.closedLoop   = closedLoop;
            this.orientToPath = orientToPath;
            this.local        = local;
        }

        public override void OnTick(float t)
        {
            Vector3 pos = SampleCatmullRom(points, t, closedLoop, startPos);

            if (orientToPath)
            {
                Vector3 nextPos = SampleCatmullRom(points, Mathf.Min(t + 0.01f, 1f), closedLoop, startPos);
                Vector3 dir     = nextPos - pos;
                if (dir != Vector3.zero)
                {
                    if (local) transform.localRotation = Quaternion.LookRotation(dir);
                    else       transform.rotation      = Quaternion.LookRotation(dir);
                }
            }

            if (local) transform.localPosition = pos;
            else       transform.position      = pos;
        }

        public override void OnComplete()
        {
            Vector3 end = closedLoop ? startPos : points[points.Length - 1];
            if (local) transform.localPosition = end;
            else       transform.position      = end;
        }

        public override void Reset()
        {
            transform    = null;
            points       = null;
            startPos     = default;
            closedLoop   = false;
            orientToPath = false;
            local        = false;
        }

        private static Vector3 SampleCatmullRom(Vector3[] points, float t, bool closed, Vector3 startPos)
        {
            int count    = points.Length + 1;
            int segments = closed ? count : count - 1;
            float scaled = t * segments;
            int   index  = Mathf.Min(Mathf.FloorToInt(scaled), segments - 1);
            float localT = scaled - index;

            Vector3 p0 = GetPoint(index - 1, points, startPos, closed, count);
            Vector3 p1 = GetPoint(index,     points, startPos, closed, count);
            Vector3 p2 = GetPoint(index + 1, points, startPos, closed, count);
            Vector3 p3 = GetPoint(index + 2, points, startPos, closed, count);

            float t2 = localT * localT;
            float t3 = t2 * localT;

            return 0.5f * (
                2f * p1 +
                (-p0 + p2) * localT +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private static Vector3 GetPoint(int index, Vector3[] points, Vector3 startPos, bool closed, int count)
        {
            if (closed)
            {
                index = ((index % count) + count) % count;
                return index == 0 ? startPos : points[index - 1];
            }
            index = Mathf.Clamp(index, 0, count - 1);
            return index == 0 ? startPos : points[index - 1];
        }

        private static readonly Stack<FlowPathInterpolator> pool = new();
        static FlowPathInterpolator() => InterpolatorPoolStats.Register(nameof(FlowPathInterpolator), () => pool.Count);
        public static FlowPathInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  LookAt (world target provider)
    // ─────────────────────────────────────────────────────────────────────────

    internal sealed class LookAtInterpolator : EffectInterpolatorBase
    {
        private Transform        transform;
        private Func<Vector3>    targetProvider;
        private Vector3          up;
        private bool             is2D;

        public void Setup(Transform transform, Func<Vector3> targetProvider, Vector3 up, bool is2D)
        {
            this.transform      = transform;
            this.targetProvider = targetProvider;
            this.up             = up;
            this.is2D           = is2D;
        }

        public override void OnTick(float t)
        {
            Vector3 dir = targetProvider() - transform.position;
            if (dir == Vector3.zero) return;
            if (is2D)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(dir, up);
            }
        }
        public override void OnComplete()    { }
        public override void Reset()         { transform = null; targetProvider = null; up = default; is2D = false; }

        private static readonly Stack<LookAtInterpolator> pool = new();
        static LookAtInterpolator() => InterpolatorPoolStats.Register(nameof(LookAtInterpolator), () => pool.Count);
        public static LookAtInterpolator Get() => pool.Count > 0 ? pool.Pop() : new();
        public override void ReturnToPool() { Reset(); pool.Push(this); }
    }
}
