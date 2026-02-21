using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace FlT
{
    // ─── Interface ────────────────────────────────────────────────────────────
    public interface IPropertyInterpolator<TTarget, TValue>
    {
        TValue  GetValue(TTarget target);
        void    SetValue(TTarget target, TValue from, TValue to, float t);
    }

    // ─── Transform ────────────────────────────────────────────────────────────
    internal readonly struct PositionInterpolator : IPropertyInterpolator<Transform, Vector3>
    {
        public Vector3 GetValue(Transform t) => t.position;
        public void SetValue(Transform t, Vector3 from, Vector3 to, float pct)
            => t.position = Vector3.LerpUnclamped(from, to, pct);
    }

    internal readonly struct LocalPositionInterpolator : IPropertyInterpolator<Transform, Vector3>
    {
        public Vector3 GetValue(Transform t) => t.localPosition;
        public void SetValue(Transform t, Vector3 from, Vector3 to, float pct)
            => t.localPosition = Vector3.LerpUnclamped(from, to, pct);
    }

    internal readonly struct RotationInterpolator : IPropertyInterpolator<Transform, Quaternion>
    {
        public Quaternion GetValue(Transform t) => t.rotation;
        public void SetValue(Transform t, Quaternion from, Quaternion to, float pct)
            => t.rotation = Quaternion.SlerpUnclamped(from, to, pct);
    }

    internal readonly struct LocalRotationInterpolator : IPropertyInterpolator<Transform, Quaternion>
    {
        public Quaternion GetValue(Transform t) => t.localRotation;
        public void SetValue(Transform t, Quaternion from, Quaternion to, float pct)
            => t.localRotation = Quaternion.SlerpUnclamped(from, to, pct);
    }

    internal readonly struct RotationEulerInterpolator : IPropertyInterpolator<Transform, Vector3>
    {
        public Vector3 GetValue(Transform t) => t.eulerAngles;
        public void SetValue(Transform t, Vector3 from, Vector3 to, float pct)
            => t.eulerAngles = Vector3.LerpUnclamped(from, to, pct);
    }

    internal readonly struct LocalRotationEulerInterpolator : IPropertyInterpolator<Transform, Vector3>
    {
        public Vector3 GetValue(Transform t) => t.localEulerAngles;
        public void SetValue(Transform t, Vector3 from, Vector3 to, float pct)
            => t.localEulerAngles = Vector3.LerpUnclamped(from, to, pct);
    }

    internal readonly struct LocalScaleInterpolator : IPropertyInterpolator<Transform, Vector3>
    {
        public Vector3 GetValue(Transform t) => t.localScale;
        public void SetValue(Transform t, Vector3 from, Vector3 to, float pct)
            => t.localScale = Vector3.LerpUnclamped(from, to, pct);
    }

    internal readonly struct PositionXInterpolator : IPropertyInterpolator<Transform, float>
    {
        public float GetValue(Transform t) => t.position.x;
        public void SetValue(Transform t, float from, float to, float pct)
        {
            Vector3 p = t.position;
            p.x = Mathf.LerpUnclamped(from, to, pct);
            t.position = p;
        }
    }

    internal readonly struct PositionYInterpolator : IPropertyInterpolator<Transform, float>
    {
        public float GetValue(Transform t) => t.position.y;
        public void SetValue(Transform t, float from, float to, float pct)
        {
            Vector3 p = t.position;
            p.y = Mathf.LerpUnclamped(from, to, pct);
            t.position = p;
        }
    }

    internal readonly struct PositionZInterpolator : IPropertyInterpolator<Transform, float>
    {
        public float GetValue(Transform t) => t.position.z;
        public void SetValue(Transform t, float from, float to, float pct)
        {
            Vector3 p = t.position;
            p.z = Mathf.LerpUnclamped(from, to, pct);
            t.position = p;
        }
    }

    // ─── RectTransform ────────────────────────────────────────────────────────
    internal readonly struct AnchoredPositionInterpolator : IPropertyInterpolator<RectTransform, Vector2>
    {
        public Vector2 GetValue(RectTransform t) => t.anchoredPosition;
        public void SetValue(RectTransform t, Vector2 from, Vector2 to, float pct)
            => t.anchoredPosition = Vector2.LerpUnclamped(from, to, pct);
    }

    internal readonly struct SizeDeltaInterpolator : IPropertyInterpolator<RectTransform, Vector2>
    {
        public Vector2 GetValue(RectTransform t) => t.sizeDelta;
        public void SetValue(RectTransform t, Vector2 from, Vector2 to, float pct)
            => t.sizeDelta = Vector2.LerpUnclamped(from, to, pct);
    }

    // ─── CanvasGroup ──────────────────────────────────────────────────────────
    internal readonly struct CanvasAlphaInterpolator : IPropertyInterpolator<CanvasGroup, float>
    {
        public float GetValue(CanvasGroup cg) => cg.alpha;
        public void SetValue(CanvasGroup cg, float from, float to, float t)
            => cg.alpha = Mathf.LerpUnclamped(from, to, t);
    }

    // ─── SpriteRenderer ───────────────────────────────────────────────────────
    internal readonly struct SpriteColorInterpolator : IPropertyInterpolator<SpriteRenderer, Color>
    {
        public Color GetValue(SpriteRenderer sr) => sr.color;
        public void SetValue(SpriteRenderer sr, Color from, Color to, float t)
            => sr.color = Color.LerpUnclamped(from, to, t);
    }

    internal readonly struct SpriteAlphaInterpolator : IPropertyInterpolator<SpriteRenderer, float>
    {
        public float GetValue(SpriteRenderer sr) => sr.color.a;
        public void SetValue(SpriteRenderer sr, float from, float to, float t)
        {
            Color c = sr.color;
            c.a = Mathf.LerpUnclamped(from, to, t);
            sr.color = c;
        }
    }

    // ─── Graphic (UI) ─────────────────────────────────────────────────────────
    internal readonly struct GraphicColorInterpolator : IPropertyInterpolator<Graphic, Color>
    {
        public Color GetValue(Graphic g) => g.color;
        public void SetValue(Graphic g, Color from, Color to, float t)
            => g.color = Color.LerpUnclamped(from, to, t);
    }

    internal readonly struct GraphicAlphaInterpolator : IPropertyInterpolator<Graphic, float>
    {
        public float GetValue(Graphic g) => g.color.a;
        public void SetValue(Graphic g, float from, float to, float t)
        {
            Color c = g.color;
            c.a = Mathf.LerpUnclamped(from, to, t);
            g.color = c;
        }
    }

    // ─── Material ─────────────────────────────────────────────────────────────
    internal readonly struct MaterialColorInterpolator : IPropertyInterpolator<Material, Color>
    {
        public Color GetValue(Material m) => m.color;
        public void SetValue(Material m, Color from, Color to, float t)
            => m.color = Color.LerpUnclamped(from, to, t);
    }

    // ─── AudioSource ──────────────────────────────────────────────────────────
    internal readonly struct AudioVolumeInterpolator : IPropertyInterpolator<AudioSource, float>
    {
        public float GetValue(AudioSource a) => a.volume;
        public void SetValue(AudioSource a, float from, float to, float t)
            => a.volume = Mathf.LerpUnclamped(from, to, t);
    }

    internal readonly struct AudioPitchInterpolator : IPropertyInterpolator<AudioSource, float>
    {
        public float GetValue(AudioSource a) => a.pitch;
        public void SetValue(AudioSource a, float from, float to, float t)
            => a.pitch = Mathf.LerpUnclamped(from, to, t);
    }

    // ─── Light ────────────────────────────────────────────────────────────────
    internal readonly struct LightIntensityInterpolator : IPropertyInterpolator<Light, float>
    {
        public float GetValue(Light l) => l.intensity;
        public void SetValue(Light l, float from, float to, float t)
            => l.intensity = Mathf.LerpUnclamped(from, to, t);
    }

    internal readonly struct LightColorInterpolator : IPropertyInterpolator<Light, Color>
    {
        public Color GetValue(Light l) => l.color;
        public void SetValue(Light l, Color from, Color to, float t)
            => l.color = Color.LerpUnclamped(from, to, t);
    }

    internal readonly struct LightRangeInterpolator : IPropertyInterpolator<Light, float>
    {
        public float GetValue(Light l) => l.range;
        public void SetValue(Light l, float from, float to, float t)
            => l.range = Mathf.LerpUnclamped(from, to, t);
    }

    // ─── Camera ───────────────────────────────────────────────────────────────
    internal readonly struct CameraFovInterpolator : IPropertyInterpolator<Camera, float>
    {
        public float GetValue(Camera c) => c.fieldOfView;
        public void SetValue(Camera c, float from, float to, float t)
            => c.fieldOfView = Mathf.LerpUnclamped(from, to, t);
    }

    internal readonly struct CameraOrthoSizeInterpolator : IPropertyInterpolator<Camera, float>
    {
        public float GetValue(Camera c) => c.orthographicSize;
        public void SetValue(Camera c, float from, float to, float t)
            => c.orthographicSize = Mathf.LerpUnclamped(from, to, t);
    }

    // TMP Pro
    internal readonly struct TMPProRevealInterpolator : IPropertyInterpolator<TMP_Text, int>
    {
        public int GetValue(TMP_Text target) => 0;
        public void SetValue(TMP_Text target, int from, int to, float t) =>
            target.maxVisibleCharacters = Mathf.RoundToInt(Mathf.LerpUnclamped(from, to, t)); 
    }
}