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

        public static Tween FlowRotation(this Rigidbody rb, Quaternion to, float duration) =>
            FlowTween.GetTween<Rigidbody, Quaternion, RigidbodyRotationInterpolator>(rb, duration, to).SetUpdateMode(Tween.TweenUpdateMode.Fixed);

        public static Tween FlowPosition(this Rigidbody2D rb, Vector2 to, float duration) =>
            FlowTween.GetTween<Rigidbody2D, Vector2, Rigidbody2DPositionInterpolator>(rb, duration, to).SetUpdateMode(Tween.TweenUpdateMode.Fixed);

        public static Tween FlowRotation(this Rigidbody2D rb, float to, float duration) =>
            FlowTween.GetTween<Rigidbody2D, float, Rigidbody2DRotationInterpolator>(rb, duration, to).SetUpdateMode(Tween.TweenUpdateMode.Fixed);
        #endregion

        #region Spin Effect
        public static Tween FlowSpin(this Transform transform, float duration) =>
            transform.FlowRotate(Vector3.forward * 360f, duration);
        
        public static Tween FlowSpin(this Transform transform, Vector3 axis, float duration) =>
            transform.FlowRotate(axis * 360f, duration);
        #endregion

        #region Jello
        /// <summary>
        /// Oscillating squash-and-stretch that decays like gelatine.
        /// </summary>
        public static Tween FlowJello(this Transform transform, float duration, float intensity = 0.25f, float frequency = 4f)
        {
            Vector3 startScale = transform.localScale;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float decay  = 1f - t;
                float wave   = Mathf.Sin(t * frequency * Mathf.PI * 2f) * intensity * decay;
                transform.localScale = new Vector3(
                    startScale.x + wave,
                    startScale.y - wave,
                    startScale.z
                );

            }).OnComplete(() => transform.localScale = startScale);
        }
        #endregion

        #region Heartbeat
        /// <summary>
        /// Double-pulse scale effect mimicking a heartbeat (lub-dub).
        /// </summary>
        public static Sequence FlowHeartbeat(this Transform transform, float duration, float intensity = 0.3f, int beats = 1)
        {
            Vector3 origin   = transform.localScale;
            Vector3 lub      = origin * (1f + intensity);
            Vector3 mid      = origin * (1f + intensity * 0.4f);
            Vector3 dub      = origin * (1f + intensity * 0.65f);

            float beatDur  = duration / beats;
            float step     = beatDur / 5f;

            Sequence seq = FlowTween.Sequence();

            for (int i = 0; i < beats; i++)
            {
                seq.Append(transform.FlowScale(lub,    step).Sine().EaseOut())   // lub up
                   .Append(transform.FlowScale(mid,    step).Sine().EaseInOut()) // brief dip
                   .Append(transform.FlowScale(dub,    step).Sine().EaseOut())   // dub up
                   .Append(transform.FlowScale(origin, step * 2f).Sine().EaseIn()); // settle
            }

            return seq.Play();
        }
        #endregion

        #region WobbleRotate
        /// <summary>
        /// Rotates back and forth with decaying oscillation before settling at the target rotation.
        /// </summary>
        public static Tween FlowWobbleRotate(this Transform transform, float duration, float strength = 20f, float frequency = 4f)
        {
            Quaternion startRot = transform.localRotation;

            return FlowVirtual.Float(0f, 1f, duration, t =>
            {
                float decay = 1f - t;
                float angle = Mathf.Sin(t * frequency * Mathf.PI * 2f) * strength * decay;
                transform.localRotation = startRot * Quaternion.Euler(0f, 0f, angle);

            }).OnComplete(() => transform.localRotation = startRot);
        }
        #endregion

        #region Flip
        /// <summary>
        /// Flips the transform 180° or 360° on the Y axis by scaling through zero (card-flip illusion).
        /// </summary>
        public static Sequence FlowFlipY(this Transform transform, float duration, bool full = false)
        {
            Vector3 startScale = transform.localScale;
            Vector3 flat       = new Vector3(0f, startScale.y, startScale.z);
            Vector3 flipped    = new Vector3(-startScale.x, startScale.y, startScale.z);

            float half = duration * 0.5f;

            Sequence seq = FlowTween.Sequence()
                .Append(transform.FlowScale(flat,    half).Sine().EaseIn())
                .Append(transform.FlowScale(full ? flat : flipped, 0f))   // swap content here if needed
                .Append(transform.FlowScale(full ? startScale : flipped, half).Sine().EaseOut());

            return seq.Play();
        }

        /// <summary>
        /// Flips the transform on the X axis (top-to-bottom flip).
        /// </summary>
        public static Sequence FlowFlipX(this Transform transform, float duration, bool full = false)
        {
            Vector3 startScale = transform.localScale;
            Vector3 flat       = new Vector3(startScale.x, 0f, startScale.z);
            Vector3 flipped    = new Vector3(startScale.x, -startScale.y, startScale.z);

            float half = duration * 0.5f;

            Sequence seq = FlowTween.Sequence()
                .Append(transform.FlowScale(flat,    half).Sine().EaseIn())
                .Append(transform.FlowScale(full ? startScale : flipped, half).Sine().EaseOut());

            return seq.Play();
        }
        #endregion

        #region Blink — SpriteRenderer
        /// <summary>
        /// Rapidly blinks a SpriteRenderer a set number of times.
        /// </summary>
        public static Sequence FlowBlink(this SpriteRenderer spriteRenderer, int blinks = 4, float blinkSpeed = 0.1f, bool endVisible = true)
        {
            Sequence seq = FlowTween.Sequence();

            for (int i = 0; i < blinks; i++)
            {
                seq.Append(spriteRenderer.FlowFade(0f, blinkSpeed))
                   .Append(spriteRenderer.FlowFade(1f, blinkSpeed));
            }

            if (!endVisible)
                seq.Append(spriteRenderer.FlowFade(0f, blinkSpeed));

            return seq.Play();
        }
        #endregion

        #region SlideIn / SlideOut
        public enum SlideDirection { Left, Right, Up, Down }

        /// <summary>
        /// Slides a RectTransform into its current position from an edge offset.
        /// </summary>
        public static Tween FlowSlideIn(this RectTransform rectTransform, SlideDirection direction, float offset, float duration)
        {
            Vector2 target    = rectTransform.anchoredPosition;
            Vector2 startFrom = target + DirectionToOffset(direction, offset);
            rectTransform.anchoredPosition = startFrom;

            return rectTransform.FlowAnchorMove(target, duration).Sine().EaseOut();
        }

        /// <summary>
        /// Slides a RectTransform out of its current position toward an edge.
        /// </summary>
        public static Tween FlowSlideOut(this RectTransform rectTransform, SlideDirection direction, float offset, float duration)
        {
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 target   = startPos + DirectionToOffset(direction, offset);

            return rectTransform.FlowAnchorMove(target, duration).Sine().EaseIn();
        }

        private static Vector2 DirectionToOffset(SlideDirection dir, float offset) => dir switch
        {
            SlideDirection.Left  => new Vector2(-offset, 0f),
            SlideDirection.Right => new Vector2( offset, 0f),
            SlideDirection.Up    => new Vector2(0f,  offset),
            SlideDirection.Down  => new Vector2(0f, -offset),
            _                    => Vector2.zero
        };
        #endregion

        #region Float
        /// <summary>
        /// Continuous up-and-down bobbing loop driven by a sine wave.
        /// Call Kill() on the returned tween to stop it.
        /// </summary>
        public static Tween FlowFloat(this Transform transform, float amplitude = 0.2f, float frequency = 1f)
        {
            Vector3 origin = transform.localPosition;

            return FlowVirtual.Float(0f, 1f, 1f / frequency, t =>
            {
                float y = Mathf.Sin(t * Mathf.PI * 2f) * amplitude;
                transform.localPosition = new Vector3(origin.x, origin.y + y, origin.z);

            }).SetLoops(-1, Tween.LoopType.Restart)
              .OnComplete(() => transform.localPosition = origin);
        }
        #endregion

        #region Pulse
        /// <summary>
        /// Looping scale + alpha throb to draw attention.
        /// Call Kill() on the returned sequence to stop it.
        /// </summary>
        public static Sequence FlowPulse(this Transform transform, CanvasGroup canvasGroup,
            float scaleMagnitude = 0.05f, float alphaMin = 0.7f, float frequency = 1f)
        {
            Vector3 origin  = transform.localScale;
            float   period  = 1f / frequency;

            Tween scaleTween = transform.FlowScale(origin * (1f + scaleMagnitude), period)
                .SetLoops(-1, Tween.LoopType.Yoyo).Sine().EaseInOut();

            Tween alphaTween = canvasGroup.FlowFade(alphaMin, period)
                .SetLoops(-1, Tween.LoopType.Yoyo).Sine().EaseInOut();

            return FlowTween.Sequence()
                .Append(scaleTween)
                .Join(alphaTween)
                .Play();
        }

        /// <summary>
        /// Looping scale-only throb (no CanvasGroup required).
        /// </summary>
        public static Tween FlowPulse(this Transform transform, float scaleMagnitude = 0.05f, float frequency = 1f)
        {
            Vector3 origin = transform.localScale;
            float   period = 1f / frequency;

            return transform.FlowScale(origin * (1f + scaleMagnitude), period)
                .SetLoops(-1, Tween.LoopType.Yoyo)
                .Sine()
                .EaseInOut();
        }
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

