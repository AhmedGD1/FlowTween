using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlT
{
    public enum SquishDirection { Up, Down }

    internal struct ShakePoint
    {
        public Vector3 offset;

        public ShakePoint(float strength)
        {
            offset = new Vector3(
                UnityEngine.Random.Range(-strength, strength),
                UnityEngine.Random.Range(-strength, strength),
                UnityEngine.Random.Range(-strength, strength)
            );
        }
    }

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
        public static Tween FlowAnchorMove(this RectTransform rectTransform, Vector2 to, float duration) =>
            FlowTween.GetTween<RectTransform, Vector2, AnchoredPositionInterpolator>(rectTransform, duration, to);

        public static Tween FlowAnchorMin(this RectTransform t, Vector2 to, float duration)
            => FlowTween.GetTween<RectTransform, Vector2, AnchorMinInterpolator>(t, duration, to);

        public static Tween FlowAnchorMax(this RectTransform t, Vector2 to, float duration)
            => FlowTween.GetTween<RectTransform, Vector2, AnchorMaxInterpolator>(t, duration, to);

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
        public static Tween FlowColor(this Renderer renderer, Color to, float duration) =>
            FlowTween.GetTween<Renderer, Color, RendererColorInterpolator>(renderer, duration, to);
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

        public static Tween FlowFillAmount(this Image i, float to, float duration) =>
            FlowTween.GetTween<Image, float, ImageFillInterpolator>(i, duration, to);

        public static Tween FlowPosition(this ScrollRect s, Vector2 to, float duration) =>
            FlowTween.GetTween<ScrollRect, Vector2, ScrollRectPositionInterpolator>(s, duration, to);

        public static Tween FlowValue(this Slider s, float to, float duration) =>
            FlowTween.GetTween<Slider, float, SliderValueInterpolator>(s, duration, to);
        #endregion

        #region RigidBody Methods
        public static Tween FlowPosition(this Rigidbody rb, Vector3 to, float duration) =>
            FlowTween.GetTween<Rigidbody, Vector3, RigidbodyPositionInterpolator>(rb, duration, to).SetUpdateMode(Tween.TweenUpdateMode.Fixed);

        public static Tween TweenRotation(this Rigidbody rb, Quaternion to, float duration) =>
            FlowTween.GetTween<Rigidbody, Quaternion, RigidbodyRotationInterpolator>(rb, duration, to).SetUpdateMode(Tween.TweenUpdateMode.Fixed);

        public static Tween TweenPosition(this Rigidbody2D rb, Vector2 to, float duration) =>
            FlowTween.GetTween<Rigidbody2D, Vector2, Rigidbody2DPositionInterpolator>(rb, duration, to).SetUpdateMode(Tween.TweenUpdateMode.Fixed);

        public static Tween FlowRotation(this Rigidbody2D rb, float to, float duration) =>
            FlowTween.GetTween<Rigidbody2D, float, Rigidbody2DRotationInterpolator>(rb, duration, to).SetUpdateMode(Tween.TweenUpdateMode.Fixed);
        #endregion

        #region Shake 2D
        public static Tween FlowShake2D(this Transform transform, float duration, float strength = 1f, float frequency = 20f)
        {
            Vector3 startPos = transform.position;

            float seedX = UnityEngine.Random.value * 10000f;
            float seedY = seedX + 131.73f;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float damper = Mathf.Lerp(strength, 0f, t);
                float noiseTime = t * frequency;

                float x = (Mathf.PerlinNoise(noiseTime, seedX) * 2f - 1f) * damper;
                float y = (Mathf.PerlinNoise(noiseTime, seedY) * 2f - 1f) * damper;

                transform.position = new Vector3(
                    startPos.x + x,
                    startPos.y + y,
                    startPos.z
                );

            }).OnComplete(() => transform.position = startPos);
        }

        public static Tween FlowShakeLocal2D(this Transform transform, float duration, float strength = 1f, float frequency = 20f)
        {
            Vector3 startPos = transform.localPosition;

            float seedX = UnityEngine.Random.value * 10000f;
            float seedY = seedX + 131.73f;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float damper = Mathf.Lerp(strength, 0f, t);
                float noiseTime = t * frequency;

                float x = (Mathf.PerlinNoise(noiseTime, seedX) * 2f - 1f) * damper;
                float y = (Mathf.PerlinNoise(noiseTime, seedY) * 2f - 1f) * damper;

                transform.localPosition = new Vector3(
                    startPos.x + x,
                    startPos.y + y,
                    startPos.z
                );

            }).OnComplete(() => transform.position = startPos);
        }
        #endregion

        #region Shake 3D
        public static Tween FlowShake3D(this Transform transform, float duration, float strength = 1f, float frequency = 20f)
        {
            Vector3 startPos = transform.position;

            float seedX = UnityEngine.Random.value * 10000f;
            float seedY = seedX + 131.73f;
            float seedZ = seedX + 263.46f;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float damper = Mathf.Lerp(strength, 0f, t);
                float noiseTime = t * frequency;

                float x = (Mathf.PerlinNoise(noiseTime, seedX) * 2f - 1f) * damper;
                float y = (Mathf.PerlinNoise(noiseTime, seedY) * 2f - 1f) * damper;
                float z = (Mathf.PerlinNoise(noiseTime, seedZ) * 2f - 1f) * damper;

                transform.position = startPos + new Vector3(x, y, z);

            }).OnComplete(() => transform.position = startPos);
        }

        public static Tween FlowShakeLocal3D(this Transform transform, float duration, float strength = 1f, float frequency = 20f)
        {
            Vector3 startPos = transform.localPosition;

            float seedX = UnityEngine.Random.value * 10000f;
            float seedY = seedX + 131.73f;
            float seedZ = seedX + 263.46f;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float damper = Mathf.Lerp(strength, 0f, t);
                float noiseTime = t * frequency;

                float x = (Mathf.PerlinNoise(noiseTime, seedX) * 2f - 1f) * damper;
                float y = (Mathf.PerlinNoise(noiseTime, seedY) * 2f - 1f) * damper;
                float z = (Mathf.PerlinNoise(noiseTime, seedZ) * 2f - 1f) * damper;

                transform.localPosition = startPos + new Vector3(x, y, z);

            }).OnComplete(() => transform.localPosition = startPos);
        }
        #endregion

        #region Shake Rotation
        public static Tween FlowShakeRotation3D(this Transform transform, float duration, float strength = 15f, float frequency = 20f)
        {
            Quaternion startRot = transform.localRotation;

            float seedX = UnityEngine.Random.value * 10000f;
            float seedY = seedX + 131.73f;
            float seedZ = seedX + 263.46f;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float damper = Mathf.Lerp(strength, 0f, t);
                float noiseTime = t * frequency;

                float x = (Mathf.PerlinNoise(noiseTime, seedX) * 2f - 1f) * damper;
                float y = (Mathf.PerlinNoise(noiseTime, seedY) * 2f - 1f) * damper;
                float z = (Mathf.PerlinNoise(noiseTime, seedZ) * 2f - 1f) * damper;

                transform.localRotation = startRot * Quaternion.Euler(x, y, z);

            }).OnComplete(() => transform.localRotation = startRot);
        }

        public static Tween FlowShakeRotation2D(this Transform transform, float duration, float strength = 15f, float frequency = 20f)
        {
            Quaternion startRot = transform.localRotation;

            float seed = UnityEngine.Random.value * 10000f;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float damper = Mathf.Lerp(strength, 0f, t);
                float z = (Mathf.PerlinNoise(t * frequency, seed) * 2f - 1f) * damper;

                transform.localRotation = startRot * Quaternion.Euler(0f, 0f, z);

            }).OnComplete(() => transform.localRotation = startRot);
        }

        public static Tween FlowShakeRotationAxis(this Transform transform, Vector3 axis, float duration, float strength = 15f, float frequency = 20f)
        {
            Quaternion startRot = transform.localRotation;

            float seed = UnityEngine.Random.value * 10000f;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float damper = Mathf.Lerp(strength, 0f, t);
                float angle = (Mathf.PerlinNoise(t * frequency, seed) * 2f - 1f) * damper;

                transform.localRotation = startRot * Quaternion.AngleAxis(angle, axis);

            }).OnComplete(() => transform.localRotation = startRot);
        }
        #endregion

        #region Punch 2D

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

        public static Tween FlowPunchPosition3D(this Transform transform, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            Vector3 startPosition = transform.position;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float wave = Mathf.Sin(vibrato * Mathf.PI * t) * (1f - t);
                transform.position = startPosition + punch * (wave * elasticity);
            }).OnComplete(() => transform.position = startPosition);
        }

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

