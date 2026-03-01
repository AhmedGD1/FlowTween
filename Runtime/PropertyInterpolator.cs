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

    internal readonly struct LocalPositionXInterpolator : IPropertyInterpolator<Transform, float>
    {
        public float GetValue(Transform target) => target.localPosition.x;

        public void SetValue(Transform target, float from, float to, float pct)
        {
            Vector3 localPos = target.localPosition;
            localPos.x = Mathf.LerpUnclamped(from, to, pct);
            target.localPosition = localPos;
        }
    }

    internal readonly struct LocalPositionYInterpolator : IPropertyInterpolator<Transform, float>
    {
        public float GetValue(Transform target) => target.localPosition.y;

        public void SetValue(Transform target, float from, float to, float pct)
        {
            Vector3 localPos = target.localPosition;
            localPos.y = Mathf.LerpUnclamped(from, to, pct);
            target.localPosition = localPos;
        }
    }

    internal readonly struct LocalPositionZInterpolator : IPropertyInterpolator<Transform, float>
    {
        public float GetValue(Transform target) => target.localPosition.z;

        public void SetValue(Transform target, float from, float to, float pct)
        {
            Vector3 localPos = target.localPosition;
            localPos.z = Mathf.LerpUnclamped(from, to, pct);
            target.localPosition = localPos;
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

    /// <summary>
    /// Shared helpers for pipeline-agnostic color property resolution.
    /// Prefers _BaseColor (URP/HDRP) and falls back to _Color (built-in pipeline).
    /// </summary>
    internal static class RendererColorHelper
    {
        private static readonly MaterialPropertyBlock mpb      = new();
        private static readonly int                   baseId   = Shader.PropertyToID("_BaseColor");
        private static readonly int                   legacyId = Shader.PropertyToID("_Color");

        public static int ResolveId(Renderer r) =>
            r.sharedMaterial.HasProperty(baseId) ? baseId : legacyId;

        public static Color GetColor(Renderer r)
        {
            int id = ResolveId(r);
            r.GetPropertyBlock(mpb);
            return mpb.HasColor(id) ? mpb.GetColor(id) : r.sharedMaterial.color;
        }

        public static void SetColor(Renderer r, Color color)
        {
            int id = ResolveId(r);
            r.GetPropertyBlock(mpb);
            mpb.SetColor(id, color);
            r.SetPropertyBlock(mpb);
        }
    }

    internal readonly struct RendererColorInterpolator : IPropertyInterpolator<Renderer, Color>
    {
        public Color GetValue(Renderer r) => RendererColorHelper.GetColor(r);

        public void SetValue(Renderer r, Color from, Color to, float t) =>
            RendererColorHelper.SetColor(r, Color.LerpUnclamped(from, to, t));
    }

    internal readonly struct RendererAlphaInterpolator : IPropertyInterpolator<Renderer, float>
    {
        public float GetValue(Renderer r) => RendererColorHelper.GetColor(r).a;

        public void SetValue(Renderer r, float from, float to, float t)
        {
            Color c = RendererColorHelper.GetColor(r);
            c.a = Mathf.LerpUnclamped(from, to, t);
            RendererColorHelper.SetColor(r, c);
        }
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


    internal readonly struct AudioPanStereoInterpolator : IPropertyInterpolator<AudioSource, float>
    {
        public float GetValue(AudioSource a) => a.panStereo;
        public void SetValue(AudioSource a, float from, float to, float progress)
            => a.panStereo = Mathf.LerpUnclamped(from, to, progress);
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

    internal readonly struct CameraBackgroundColorInterpolator : IPropertyInterpolator<Camera, Color>
    {
        public Color GetValue(Camera c) => c.backgroundColor;
        public void SetValue(Camera c, Color from, Color to, float progress)
            => c.backgroundColor = Color.LerpUnclamped(from, to, progress);
    }

    // TMP Pro
    internal readonly struct TMPProRevealInterpolator : IPropertyInterpolator<TMP_Text, int>
    {
        public int GetValue(TMP_Text target) => target.maxVisibleCharacters;
        public void SetValue(TMP_Text target, int from, int to, float t) =>
            target.maxVisibleCharacters = Mathf.RoundToInt(Mathf.LerpUnclamped(from, to, t));
    }

    // ─── RectTransform Anchors ────────────────────────────────────────────────
    internal readonly struct AnchorMinInterpolator : IPropertyInterpolator<RectTransform, Vector2>
    {
        public Vector2 GetValue(RectTransform t) => t.anchorMin;
        public void SetValue(RectTransform t, Vector2 from, Vector2 to, float pct)
            => t.anchorMin = Vector2.LerpUnclamped(from, to, pct);
    }

    internal readonly struct AnchorMaxInterpolator : IPropertyInterpolator<RectTransform, Vector2>
    {
        public Vector2 GetValue(RectTransform t) => t.anchorMax;
        public void SetValue(RectTransform t, Vector2 from, Vector2 to, float pct)
            => t.anchorMax = Vector2.LerpUnclamped(from, to, pct);
    }

    // ─── ScrollRect ───────────────────────────────────────────────────────────
    internal readonly struct ScrollRectPositionInterpolator : IPropertyInterpolator<ScrollRect, Vector2>
    {
        public Vector2 GetValue(ScrollRect s) => s.normalizedPosition;
        public void SetValue(ScrollRect s, Vector2 from, Vector2 to, float pct)
            => s.normalizedPosition = Vector2.LerpUnclamped(from, to, pct);
    }

    // ─── Slider ───────────────────────────────────────────────────────────────
    internal readonly struct SliderValueInterpolator : IPropertyInterpolator<Slider, float>
    {
        public float GetValue(Slider s) => s.value;
        public void SetValue(Slider s, float from, float to, float t)
            => s.value = Mathf.LerpUnclamped(from, to, t);
    }

    // ─── Image FillAmount ─────────────────────────────────────────────────────
    internal readonly struct ImageFillInterpolator : IPropertyInterpolator<Image, float>
    {
        public float GetValue(Image i) => i.fillAmount;
        public void SetValue(Image i, float from, float to, float t)
            => i.fillAmount = Mathf.LerpUnclamped(from, to, t);
    }

    // ─── Rigidbody ────────────────────────────────────────────────────────────
    internal readonly struct RigidbodyPositionInterpolator : IPropertyInterpolator<Rigidbody, Vector3>
    {
        public Vector3 GetValue(Rigidbody rb) => rb.position;
        public void SetValue(Rigidbody rb, Vector3 from, Vector3 to, float t)
            => rb.MovePosition(Vector3.LerpUnclamped(from, to, t));
    }

    internal readonly struct RigidbodyRotationInterpolator : IPropertyInterpolator<Rigidbody, Quaternion>
    {
        public Quaternion GetValue(Rigidbody rb) => rb.rotation;
        public void SetValue(Rigidbody rb, Quaternion from, Quaternion to, float t)
            => rb.MoveRotation(Quaternion.SlerpUnclamped(from, to, t));
    }

    // ─── Rigidbody2D ──────────────────────────────────────────────────────────
    internal readonly struct Rigidbody2DPositionInterpolator : IPropertyInterpolator<Rigidbody2D, Vector2>
    {
        public Vector2 GetValue(Rigidbody2D rb) => rb.position;
        public void SetValue(Rigidbody2D rb, Vector2 from, Vector2 to, float t)
            => rb.MovePosition(Vector2.LerpUnclamped(from, to, t));
    }

    internal readonly struct Rigidbody2DRotationInterpolator : IPropertyInterpolator<Rigidbody2D, float>
    {
        public float GetValue(Rigidbody2D rb) => rb.rotation;
        public void SetValue(Rigidbody2D rb, float from, float to, float t)
            => rb.MoveRotation(Mathf.LerpUnclamped(from, to, t));
    }

    // Pivot & Offset
    internal readonly struct PivotInterpolator : IPropertyInterpolator<RectTransform, Vector2>
    {
        public Vector2 GetValue(RectTransform t) => t.pivot;
        public void SetValue(RectTransform t, Vector2 from, Vector2 to, float progress)
            => t.pivot = Vector2.LerpUnclamped(from, to, progress);
    }

    internal readonly struct OffsetMinInterpolator : IPropertyInterpolator<RectTransform, Vector2>
    {
        public Vector2 GetValue(RectTransform t) => t.offsetMin;
        public void SetValue(RectTransform t, Vector2 from, Vector2 to, float progress)
            => t.offsetMin = Vector2.LerpUnclamped(from, to, progress);
    }

    internal readonly struct OffsetMaxInterpolator : IPropertyInterpolator<RectTransform, Vector2>
    {
        public Vector2 GetValue(RectTransform t) => t.offsetMax;
        public void SetValue(RectTransform t, Vector2 from, Vector2 to, float progress)
            => t.offsetMax = Vector2.LerpUnclamped(from, to, progress);
    }
}