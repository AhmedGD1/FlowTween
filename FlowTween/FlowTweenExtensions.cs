using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlT
{
    public enum SquishDirection { Up, Down }

    public static class FlowTweenExtensions
    {
        #region Main Methods
        public static void FlowKill(this UnityEngine.Object target) => FlowTween.KillTarget(target);
        public static void FlowComplete(this UnityEngine.Object target) => FlowTween.CompleteTarget(target);
        #endregion

        #region Move Methods
        public static Tween FlowMove(this Transform transform, Vector3 to, float duration) =>
            FlowTween.GetTween<Transform, Vector3, PositionInterpolator>(transform, duration, to);

        public static Tween FlowMoveX(this Transform transform, float to, float duration) =>
            FlowTween.GetTween<Transform, float, PositionXInterpolator>(transform, duration, to);

        public static Tween FlowMoveY(this Transform transform, float to, float duration) =>
            FlowTween.GetTween<Transform, float, PositionYInterpolator>(transform, duration, to);

        public static Tween FlowMoveZ(this Transform transform, float to, float duration) =>
            FlowTween.GetTween<Transform, float, PositionZInterpolator>(transform, duration, to);

        public static Tween FlowMoveLocal(this Transform transform, Vector3 to, float duration) =>
            FlowTween.GetTween<Transform, Vector3, LocalPositionInterpolator>(transform, duration, to);
        #endregion

        #region Scale Methods
        public static Tween FlowScale(this Transform transform, Vector3 to, float duration) =>
            FlowTween.GetTween<Transform, Vector3, LocalScaleInterpolator>(transform, duration, to);

        public static Tween FlowScaleUniform(this Transform transform, float scale, float duration) =>
            transform.FlowScale(Vector3.one * scale, duration);

        public static Sequence FlowSquish(this Transform transform, float duration, float ratio = 0.2f, SquishDirection direction = SquishDirection.Up)
        {
            Vector3 val1 = direction == SquishDirection.Up ? new Vector3(1f - ratio, 1f + ratio, 1f) : new Vector3(1f + ratio, 1f - ratio, 1f);
            Vector3 val2 = direction == SquishDirection.Up ? new Vector3(1f + ratio, 1f - ratio, 1f) : new Vector3(1f - ratio, 1f + ratio, 1f);
            Vector3 val3 = Vector3.one;

            float stepDuration = duration / 3f;

            return FlowTween.Sequence()
                .Append(transform.FlowScale(val1, stepDuration).Sine())
                .Append(transform.FlowScale(val2, stepDuration).Sine())
                .Append(transform.FlowScale(val3, stepDuration).Sine())
                .Play();
        }
        #endregion

        #region Rotation Methods
        public static Tween FlowRotate(this Transform transform, Quaternion to, float duration) =>
            FlowTween.GetTween<Transform, Quaternion, RotationInterpolator>(transform, duration, to);

        public static Tween FlowRotate(this Transform transform, Vector3 toEuler, float duration) =>
            FlowTween.GetTween<Transform, Vector3, RotationEulerInterpolator>(transform, duration, toEuler);

        public static Tween FlowRotateLocal(this Transform transform, Quaternion to, float duration) =>
            FlowTween.GetTween<Transform, Quaternion, LocalRotationInterpolator>(transform, duration, to);

        public static Tween FlowRotateLocal(this Transform transform, Vector3 toEuler, float duration) =>
            FlowTween.GetTween<Transform, Vector3, LocalRotationEulerInterpolator>(transform, duration, toEuler);
        #endregion

        #region Fade Methods
        public static Tween FlowFade(this CanvasGroup canvasGroup, float to, float duration) =>
            FlowTween.GetTween<CanvasGroup, float, CanvasAlphaInterpolator>(canvasGroup, duration, to);

        public static Tween FlowFadeIn(this CanvasGroup canvasGroup, float duration) =>
            canvasGroup.FlowFade(1f, duration);

        public static Tween FlowFadeOut(this CanvasGroup canvasGroup, float duration) =>
            canvasGroup.FlowFade(0f, duration);
        #endregion

        #region UI Methods
        public static Tween AnchorMove(this RectTransform rectTransform, Vector2 to, float duration) =>
            FlowTween.GetTween<RectTransform, Vector2, AnchoredPositionInterpolator>(rectTransform, duration, to);

        public static Tween FlowSizeDelta(this RectTransform rectTransform, Vector2 to, float duration) =>
            FlowTween.GetTween<RectTransform, Vector2, SizeDeltaInterpolator>(rectTransform, duration, to);
        #endregion

        #region Renderer Methods
        public static Tween FlowFade(this SpriteRenderer spriteRenderer, float to, float duration) =>
            FlowTween.GetTween<SpriteRenderer, float, SpriteAlphaInterpolator>(spriteRenderer, duration, to);

        public static Tween FlowFadeIn(this SpriteRenderer spriteRenderer, float duration) =>
            spriteRenderer.FlowFade(1f, duration);

        public static Tween FlowFadeOut(this SpriteRenderer spriteRenderer, float duration) =>
            spriteRenderer.FlowFade(0f, duration);

        public static Tween FlowColor(this SpriteRenderer spriteRenderer, Color to, float duration) =>
            FlowTween.GetTween<SpriteRenderer, Color, SpriteColorInterpolator>(spriteRenderer, duration, to);
        #endregion

        #region Material Methods
        public static Tween FlowColor(this Material material, Color to, float duration) =>
            FlowTween.GetTween<Material, Color, MaterialColorInterpolator>(material, duration, to);
        #endregion

        #region Audio Methods
        public static Tween FlowVolume(this AudioSource audioSource, float to, float duration) =>
            FlowTween.GetTween<AudioSource, float, AudioVolumeInterpolator>(audioSource, duration, to);

        public static Tween FlowPitch(this AudioSource audioSource, float to, float duration) =>
            FlowTween.GetTween<AudioSource, float, AudioPitchInterpolator>(audioSource, duration, to);
        #endregion

        #region Light Methods
        public static Tween FlowIntensity(this Light light, float to, float duration) =>
            FlowTween.GetTween<Light, float, LightIntensityInterpolator>(light, duration, to);

        public static Tween FlowColor(this Light light, Color to, float duration) => 
            FlowTween.GetTween<Light, Color, LightColorInterpolator>(light, duration, to);

        public static Tween FlowRange(this Light light, float to, float duration) =>
            FlowTween.GetTween<Light, float, LightRangeInterpolator>(light, duration, to);
        #endregion

        #region Camera Methods
        public static Tween FlowFov(this Camera camera, float to, float duration) =>
            FlowTween.GetTween<Camera, float, CameraFovInterpolator>(camera, duration, to);

        public static Tween FlowOrthoSize(this Camera camera, float to, float duration) =>
            FlowTween.GetTween<Camera, float, CameraOrthoSizeInterpolator>(camera, duration, to);
        #endregion

        #region UI Graphics
        public static Tween FlowFade(this Graphic graphic, float to, float duration) =>
            FlowTween.GetTween<Graphic, float, GraphicAlphaInterpolator>(graphic, duration, to);

        public static Tween FlowFadeIn(this Graphic graphic, float duration) => graphic.FlowFade(1f, duration);
        public static Tween FlowFadeOut(this Graphic graphic, float duration) => graphic.FlowFade(0f, duration);

        public static Tween FlowColor(this Graphic graphic, Color to, float duration) =>
            FlowTween.GetTween<Graphic, Color, GraphicColorInterpolator>(graphic, duration, to);

        public static Tween FlowReveal(this TMP_Text text, float duration) =>
            FlowTween.GetTween<TMP_Text, int, TMPProRevealInterpolator>(text, duration, text.textInfo.characterCount);
        #endregion

        #region Shake 2D

        /// <summary>Shakes the transform's position on the XY plane (Z unchanged). Suitable for 2D games.</summary>
        public static Tween FlowShake2D(this Transform transform, float duration, float strength = 1f, float randomness = 90f)
        {
            Vector3 startPosition = transform.position;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float dampedStrength = Mathf.Lerp(strength, 0f, t);

                float angle  = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float spread = randomness * Mathf.Deg2Rad;
                angle += UnityEngine.Random.Range(-spread, spread);

                transform.position = startPosition + new Vector3(
                    Mathf.Cos(angle) * dampedStrength,
                    Mathf.Sin(angle) * dampedStrength,
                    0f
                );
            }).OnComplete(() => transform.position = startPosition);
        }

        /// <summary>Shakes the transform's rotation on the Z axis only (degrees). Suitable for 2D games.</summary>
        public static Tween FlowShakeRotation2D(this Transform transform, float duration, float strength = 15f)
        {
            Vector3 startEuler = transform.localEulerAngles;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float dampedStrength = Mathf.Lerp(strength, 0f, t);
                float zShake = UnityEngine.Random.Range(-dampedStrength, dampedStrength);

                transform.localEulerAngles = new Vector3(startEuler.x, startEuler.y, startEuler.z + zShake);
            }).OnComplete(() => transform.localEulerAngles = startEuler);
        }

        #endregion

        #region Shake 3D

        /// <summary>Shakes the transform's position on all three axes. Suitable for 3D games.</summary>
        public static Tween FlowShake3D(this Transform transform, float duration, float strength = 1f, float randomness = 90f)
        {
            Vector3 startPosition = transform.position;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float dampedStrength = Mathf.Lerp(strength, 0f, t);

                Vector3 offset = UnityEngine.Random.insideUnitSphere;
                float spread   = Mathf.Clamp01(randomness / 180f);
                offset         = Vector3.Lerp(offset.normalized, UnityEngine.Random.onUnitSphere, spread);

                transform.position = startPosition + offset * dampedStrength;
            }).OnComplete(() => transform.position = startPosition);
        }

        /// <summary>Shakes the transform's local euler rotation on all three axes (degrees). Suitable for 3D games.</summary>
        public static Tween FlowShakeRotation3D(this Transform transform, float duration, float strength = 15f)
        {
            Vector3 startEuler = transform.localEulerAngles;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float dampedStrength = Mathf.Lerp(strength, 0f, t);

                transform.localEulerAngles = startEuler + new Vector3(
                    UnityEngine.Random.Range(-dampedStrength, dampedStrength),
                    UnityEngine.Random.Range(-dampedStrength, dampedStrength),
                    UnityEngine.Random.Range(-dampedStrength, dampedStrength)
                );
            }).OnComplete(() => transform.localEulerAngles = startEuler);
        }

        #endregion

        #region Punch 2D

        /// <summary>Punches the transform's position on the XY plane, then springs back. Suitable for 2D games.</summary>
        public static Tween FlowPunchPosition2D(this Transform transform, Vector2 punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            Vector3 startPosition = transform.position;
            Vector3 punch3D       = new Vector3(punch.x, punch.y, 0f);

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float wave = Mathf.Sin(vibrato * Mathf.PI * t) * (1f - t);
                transform.position = startPosition + punch3D * (wave * elasticity);
            }).OnComplete(() => transform.position = startPosition);
        }

        /// <summary>Punches the transform's local scale uniformly on XY (Z unchanged), then springs back. Suitable for 2D games.</summary>
        public static Tween FlowPunchScale2D(this Transform transform, float punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            Vector3 startScale = transform.localScale;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float wave   = Mathf.Sin(vibrato * Mathf.PI * t) * (1f - t);
                float offset = wave * punch * elasticity;
                transform.localScale = new Vector3(
                    startScale.x + offset,
                    startScale.y + offset,
                    startScale.z
                );
            }).OnComplete(() => transform.localScale = startScale);
        }

        #endregion

        #region Punch 3D

        /// <summary>Punches the transform's world position in the given direction, then springs back. Suitable for 3D games.</summary>
        public static Tween FlowPunchPosition3D(this Transform transform, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            Vector3 startPosition = transform.position;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float wave = Mathf.Sin(vibrato * Mathf.PI * t) * (1f - t);
                transform.position = startPosition + punch * (wave * elasticity);
            }).OnComplete(() => transform.position = startPosition);
        }

        /// <summary>Punches the transform's local scale on all three axes, then springs back. Suitable for 3D games.</summary>
        public static Tween FlowPunchScale3D(this Transform transform, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            Vector3 startScale = transform.localScale;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float wave = Mathf.Sin(vibrato * Mathf.PI * t) * (1f - t);
                transform.localScale = startScale + punch * (wave * elasticity);
            }).OnComplete(() => transform.localScale = startScale);
        }

        #endregion
    }
}

