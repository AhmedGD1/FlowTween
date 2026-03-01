using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FlT
{
    /// <summary>Direction used by <see cref="FlowTweenExtensions.FlowSquish"/>.</summary>
    public enum SquishDirection { Up, Down }
    public enum SpinDirection { Forward, Backward }

    /// <summary>
    /// Extension methods for animating Unity components with FlowTween.
    /// Covers Transform, UI, Audio, Light, Camera, Rigidbody, TMP, Material, and effect helpers.
    /// </summary>
    public static class FlowTweenExtensions
    {
        #region Main Methods

        /// <summary> Deletes all active tweens immediately & returns them to pool (no callback, no anything just return to pool) </summary>
        /// <param name="target"></param>
        public static void FlowFree(this UnityEngine.Object target) => FlowTween.FreeTarget(target);

        /// <summary>Kills all active tweens targeting this object.</summary>
        /// <param name="target">The Unity object whose tweens should be killed.</param>
        public static void FlowKill(this UnityEngine.Object target) => FlowTween.KillTarget(target);

        /// <summary>Immediately completes all active tweens targeting this object.</summary>
        /// <param name="target">The Unity object whose tweens should be completed.</param>
        public static void FlowComplete(this UnityEngine.Object target) => FlowTween.CompleteTarget(target);

        #endregion

        #region Move Methods

        /// <summary>Moves a Transform to a world-space position over <paramref name="duration"/> seconds.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="to">Target world position.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowMove(this Transform transform, Vector3 to, float duration) =>
            FlowTween.GetTween<Transform, Vector3, PositionInterpolator>(transform, duration, to);

        /// <summary>Moves a Transform to a world-space X position, leaving Y and Z unchanged.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="to">Target X value.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowMoveX(this Transform transform, float to, float duration) =>
            FlowTween.GetTween<Transform, float, PositionXInterpolator>(transform, duration, to);

        /// <summary>Moves a Transform to a world-space Y position, leaving X and Z unchanged.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="to">Target Y value.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowMoveY(this Transform transform, float to, float duration) =>
            FlowTween.GetTween<Transform, float, PositionYInterpolator>(transform, duration, to);

        /// <summary>Moves a Transform to a world-space Z position, leaving X and Y unchanged.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="to">Target Z value.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowMoveZ(this Transform transform, float to, float duration) =>
            FlowTween.GetTween<Transform, float, PositionZInterpolator>(transform, duration, to);

        /// <summary>Moves a Transform to a local-space position over <paramref name="duration"/> seconds.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="to">Target local position.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowMoveLocal(this Transform transform, Vector3 to, float duration) =>
            FlowTween.GetTween<Transform, Vector3, LocalPositionInterpolator>(transform, duration, to);

        /// <summary>Moves a Transform to a local-space X position, leaving Y and Z unchanged.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="to">Target local X value.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowMoveLocalX(this Transform transform, float to, float duration) =>
            FlowTween.GetTween<Transform, float, LocalPositionXInterpolator>(transform, duration, to);

        /// <summary>Moves a Transform to a local-space Y position, leaving X and Z unchanged.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="to">Target local Y value.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowMoveLocalY(this Transform transform, float to, float duration) =>
            FlowTween.GetTween<Transform, float, LocalPositionYInterpolator>(transform, duration, to);

        /// <summary>Moves a Transform to a local-space Z position, leaving X and Y unchanged.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="to">Target local Z value.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowMoveLocalZ(this Transform transform, float to, float duration) =>
            FlowTween.GetTween<Transform, float, LocalPositionZInterpolator>(transform, duration, to);

        #endregion

        #region Scale Methods

        /// <summary>Scales a Transform to the target local scale over <paramref name="duration"/> seconds.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="to">Target local scale.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowScale(this Transform transform, Vector3 to, float duration) =>
            FlowTween.GetTween<Transform, Vector3, LocalScaleInterpolator>(transform, duration, to);

        /// <summary>Uniformly scales a Transform to <paramref name="scale"/> on all axes.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="scale">Uniform scale value applied to all axes.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowScaleUniform(this Transform transform, float scale, float duration) =>
            transform.FlowScale(Vector3.one * scale, duration);

        /// <summary>
        /// Plays a three-step squash-and-stretch animation.
        /// The transform squishes, overshoots, then returns to <c>Vector3.one</c>.
        /// Note: returned tween is a delay tween not the tween updated since it's not one but 3 tweens for squish
        /// </summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="duration">Total duration of the squish effect in seconds.</param>
        /// <param name="ratio">Squish intensity (0–1). Default is 0.2.</param>
        /// <param name="direction">Whether to squish upward or downward first.</param>
        public static Tween FlowSquish(this Transform transform, float duration, float ratio = 0.2f, SquishDirection direction = SquishDirection.Up)
        {
            Vector3 original = transform.localScale;

            float minX = original.x - ratio;
            float maxX = original.x + ratio;
            
            float minY = original.x - ratio;
            float maxY = original.x + ratio;

            Vector3 val1 = direction == SquishDirection.Up ? new Vector3(minX, maxY, 1f) : new Vector3(maxX, minY, 1f);
            Vector3 val2 = direction == SquishDirection.Up ? new Vector3(maxX, minY, 1f) : new Vector3(minX, maxY, 1f);
            Vector3 val3 = Vector3.one;

            float stepDuration = duration / 3f;

            transform.FlowScale(val1, stepDuration).Sine()
                .Then(transform.FlowScale(val2, stepDuration).Sine())
                .Then(transform.FlowScale(val3, stepDuration).Sine());

            return FlowVirtual.DelayedCall(duration);
        }

        #endregion

        #region Rotation Methods

        /// <summary>Rotates a Transform to a world-space quaternion over <paramref name="duration"/> seconds.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="to">Target world rotation.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowRotate(this Transform transform, Quaternion to, float duration) =>
            FlowTween.GetTween<Transform, Quaternion, RotationInterpolator>(transform, duration, to);

        /// <summary>Rotates a Transform to a world-space euler angle over <paramref name="duration"/> seconds.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="toEuler">Target euler angles in degrees.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowRotate(this Transform transform, Vector3 toEuler, float duration) =>
            FlowTween.GetTween<Transform, Vector3, RotationEulerInterpolator>(transform, duration, toEuler);

        /// <summary>Rotates a Transform to a local-space quaternion over <paramref name="duration"/> seconds.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="to">Target local rotation.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowRotateLocal(this Transform transform, Quaternion to, float duration) =>
            FlowTween.GetTween<Transform, Quaternion, LocalRotationInterpolator>(transform, duration, to);

        /// <summary>Rotates a Transform to a local-space euler angle over <paramref name="duration"/> seconds.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="toEuler">Target local euler angles in degrees.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowRotateLocal(this Transform transform, Vector3 toEuler, float duration) =>
            FlowTween.GetTween<Transform, Vector3, LocalRotationEulerInterpolator>(transform, duration, toEuler);

        #endregion

        #region Fade Methods

        /// <summary>Animates a <see cref="CanvasGroup"/> alpha to <paramref name="to"/>.</summary>
        /// <param name="canvasGroup">Target CanvasGroup.</param>
        /// <param name="to">Target alpha (0–1).</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowFade(this CanvasGroup canvasGroup, float to, float duration) =>
            FlowTween.GetTween<CanvasGroup, float, CanvasAlphaInterpolator>(canvasGroup, duration, to);

        /// <summary>Fades a <see cref="CanvasGroup"/> to fully opaque (alpha = 1).</summary>
        /// <param name="canvasGroup">Target CanvasGroup.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowFadeIn(this CanvasGroup canvasGroup, float duration) =>
            canvasGroup.FlowFade(1f, duration);

        /// <summary>Fades a <see cref="CanvasGroup"/> to fully transparent (alpha = 0).</summary>
        /// <param name="canvasGroup">Target CanvasGroup.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowFadeOut(this CanvasGroup canvasGroup, float duration) =>
            canvasGroup.FlowFade(0f, duration);

        /// <summary>
        /// Fades a <see cref="CanvasGroup"/> to transparent, then disables
        /// <c>interactable</c> and <c>blocksRaycasts</c> on completion.
        /// </summary>
        /// <param name="canvasGroup">Target CanvasGroup.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowFadeDisable(this CanvasGroup canvasGroup, float duration)
        {
            return canvasGroup.FlowFade(0f, duration)
                              .OnComplete(() =>
                              {
                                  canvasGroup.interactable   = false;
                                  canvasGroup.blocksRaycasts = false;
                              });
        }

        /// <summary>
        /// Immediately enables <c>interactable</c> and <c>blocksRaycasts</c>,
        /// then fades a <see cref="CanvasGroup"/> to fully opaque.
        /// </summary>
        /// <param name="canvasGroup">Target CanvasGroup.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowFadeEnable(this CanvasGroup canvasGroup, float duration)
        {
            canvasGroup.interactable   = true;
            canvasGroup.blocksRaycasts = true;
            return canvasGroup.FlowFade(1f, duration);
        }

        #endregion

        #region UI Methods

        /// <summary>Animates a <see cref="RectTransform"/> anchored position to <paramref name="to"/>.</summary>
        /// <param name="rectTransform">Target RectTransform.</param>
        /// <param name="to">Target anchored position.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowAnchorMove(this RectTransform rectTransform, Vector2 to, float duration) =>
            FlowTween.GetTween<RectTransform, Vector2, AnchoredPositionInterpolator>(rectTransform, duration, to);

        /// <summary>Animates the <c>anchorMin</c> of a <see cref="RectTransform"/>.</summary>
        /// <param name="t">Target RectTransform.</param>
        /// <param name="to">Target anchorMin value.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowAnchorMin(this RectTransform t, Vector2 to, float duration)
            => FlowTween.GetTween<RectTransform, Vector2, AnchorMinInterpolator>(t, duration, to);

        /// <summary>Animates the <c>anchorMax</c> of a <see cref="RectTransform"/>.</summary>
        /// <param name="t">Target RectTransform.</param>
        /// <param name="to">Target anchorMax value.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowAnchorMax(this RectTransform t, Vector2 to, float duration)
            => FlowTween.GetTween<RectTransform, Vector2, AnchorMaxInterpolator>(t, duration, to);

        /// <summary>Animates the <c>sizeDelta</c> of a <see cref="RectTransform"/>.</summary>
        /// <param name="rectTransform">Target RectTransform.</param>
        /// <param name="to">Target sizeDelta.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowSizeDelta(this RectTransform rectTransform, Vector2 to, float duration) =>
            FlowTween.GetTween<RectTransform, Vector2, SizeDeltaInterpolator>(rectTransform, duration, to);

        /// <summary>Animates the <c>pivot</c> of a <see cref="RectTransform"/>.</summary>
        /// <param name="rectTransform">Target RectTransform.</param>
        /// <param name="to">Target pivot (0–1 range on each axis).</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowPivot(this RectTransform rectTransform, Vector2 to, float duration) =>
            FlowTween.GetTween<RectTransform, Vector2, PivotInterpolator>(rectTransform, duration, to);

        /// <summary>Animates the <c>offsetMin</c> of a <see cref="RectTransform"/>.</summary>
        /// <param name="rectTransform">Target RectTransform.</param>
        /// <param name="to">Target offsetMin.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowOffsetMin(this RectTransform rectTransform, Vector2 to, float duration) =>
            FlowTween.GetTween<RectTransform, Vector2, OffsetMinInterpolator>(rectTransform, duration, to);

        /// <summary>Animates the <c>offsetMax</c> of a <see cref="RectTransform"/>.</summary>
        /// <param name="rectTransform">Target RectTransform.</param>
        /// <param name="to">Target offsetMax.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowOffsetMax(this RectTransform rectTransform, Vector2 to, float duration) =>
            FlowTween.GetTween<RectTransform, Vector2, OffsetMaxInterpolator>(rectTransform, duration, to);

        #endregion

        #region Renderer Methods

        /// <summary>Animates the alpha of a <see cref="SpriteRenderer"/>.</summary>
        /// <param name="spriteRenderer">Target SpriteRenderer.</param>
        /// <param name="to">Target alpha (0–1).</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowFade(this SpriteRenderer spriteRenderer, float to, float duration) =>
            FlowTween.GetTween<SpriteRenderer, float, SpriteAlphaInterpolator>(spriteRenderer, duration, to);

        /// <summary>Fades a <see cref="SpriteRenderer"/> to fully opaque (alpha = 1).</summary>
        /// <param name="spriteRenderer">Target SpriteRenderer.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowFadeIn(this SpriteRenderer spriteRenderer, float duration) =>
            spriteRenderer.FlowFade(1f, duration);

        /// <summary>Fades a <see cref="SpriteRenderer"/> to fully transparent (alpha = 0).</summary>
        /// <param name="spriteRenderer">Target SpriteRenderer.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowFadeOut(this SpriteRenderer spriteRenderer, float duration) =>
            spriteRenderer.FlowFade(0f, duration);

        /// <summary>Animates the color of a <see cref="SpriteRenderer"/>.</summary>
        /// <param name="spriteRenderer">Target SpriteRenderer.</param>
        /// <param name="to">Target color.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowColor(this SpriteRenderer spriteRenderer, Color to, float duration) =>
            FlowTween.GetTween<SpriteRenderer, Color, SpriteColorInterpolator>(spriteRenderer, duration, to);

        /// <summary>
        /// Animates the color of a <see cref="SpriteRenderer"/> by sampling a <see cref="Gradient"/> over time.
        /// </summary>
        /// <param name="spriteRenderer">Target SpriteRenderer.</param>
        /// <param name="gradient">Gradient to sample across the tween duration.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowGradient(this SpriteRenderer spriteRenderer, Gradient gradient, float duration)
        {
            var interp = SpriteGradientInterpolator.Get();
            interp.Setup(spriteRenderer, gradient);
            return FlowTween.MakeTween(duration, interp, spriteRenderer);
        }

        #endregion

        #region Material Methods

        /// <summary>Animates the default color property of a <see cref="Renderer"/> material.</summary>
        /// <param name="renderer">Target Renderer.</param>
        /// <param name="to">Target color.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowColor(this Renderer renderer, Color to, float duration) =>
            FlowTween.GetTween<Renderer, Color, RendererColorInterpolator>(renderer, duration, to);

        /// <summary>Animates a named float property on a <see cref="Renderer"/> material.</summary>
        /// <param name="renderer">Target Renderer.</param>
        /// <param name="property">Shader property name (e.g. <c>"_Glossiness"</c>).</param>
        /// <param name="to">Target float value.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowMaterialFloat(this Renderer renderer, string property, float to, float duration)
        {
            var interp = MaterialFloatInterpolator.Get();
            interp.Setup(renderer, property, to);
            return FlowTween.MakeTween(duration, interp, renderer);
        }

        /// <summary>Animates a named color property on a <see cref="Renderer"/> material.</summary>
        /// <param name="renderer">Target Renderer.</param>
        /// <param name="property">Shader property name (e.g. <c>"_EmissionColor"</c>).</param>
        /// <param name="to">Target color.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowMaterialColor(this Renderer renderer, string property, Color to, float duration)
        {
            var interp = MaterialColorInterpolator.Get();
            interp.Setup(renderer, property, to);
            return FlowTween.MakeTween(duration, interp, renderer);
        }

        /// <summary>Animates a named Vector4 property on a <see cref="Renderer"/> material.</summary>
        /// <param name="renderer">Target Renderer.</param>
        /// <param name="property">Shader property name.</param>
        /// <param name="to">Target Vector4 value.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowMaterialVector(this Renderer renderer, string property, Vector4 to, float duration)
        {
            var interp = MaterialVectorInterpolator.Get();
            interp.Setup(renderer, property, to);
            return FlowTween.MakeTween(duration, interp, renderer);
        }

        /// <summary>
        /// Animates the texture tiling (<c>SetTextureScale</c>) of a named property on a <see cref="Renderer"/> material.
        /// Useful for scrolling or zooming texture effects.
        /// </summary>
        /// <param name="renderer">Target Renderer.</param>
        /// <param name="property">Shader property name (e.g. <c>"_MainTex"</c>).</param>
        /// <param name="to">Target tiling value.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowMaterialTiling(this Renderer renderer, string property, Vector2 to, float duration)
        {
            var interp = MaterialTilingInterpolator.Get();
            interp.Setup(renderer, property, to);
            return FlowTween.MakeTween(duration, interp, renderer);
        }

        /// <summary>
        /// Animates the texture offset (<c>SetTextureOffset</c>) of a named property on a <see cref="Renderer"/> material.
        /// Useful for scrolling backgrounds or water surfaces.
        /// </summary>
        /// <param name="renderer">Target Renderer.</param>
        /// <param name="property">Shader property name (e.g. <c>"_MainTex"</c>).</param>
        /// <param name="to">Target offset value.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowMaterialOffset(this Renderer renderer, string property, Vector2 to, float duration)
        {
            var interp = MaterialOffsetInterpolator.Get();
            interp.Setup(renderer, property, to);
            return FlowTween.MakeTween(duration, interp, renderer);
        }

        /// <summary>Animates the alpha of a <see cref="Renderer"/> material.</summary>
        /// <param name="renderer">Target Renderer.</param>
        /// <param name="to">Target alpha (0–1).</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowAlpha(this Renderer renderer, float to, float duration) =>
            FlowTween.GetTween<Renderer, float, RendererAlphaInterpolator>(renderer, duration, to);

        #endregion

        #region Audio Methods

        /// <summary>Animates the volume of an <see cref="AudioSource"/>.</summary>
        /// <param name="audioSource">Target AudioSource.</param>
        /// <param name="to">Target volume (0–1).</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowVolume(this AudioSource audioSource, float to, float duration) =>
            FlowTween.GetTween<AudioSource, float, AudioVolumeInterpolator>(audioSource, duration, to);

        /// <summary>Animates the pitch of an <see cref="AudioSource"/>.</summary>
        /// <param name="audioSource">Target AudioSource.</param>
        /// <param name="to">Target pitch.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowPitch(this AudioSource audioSource, float to, float duration) =>
            FlowTween.GetTween<AudioSource, float, AudioPitchInterpolator>(audioSource, duration, to);

        /// <summary>Animates the stereo pan of an <see cref="AudioSource"/>.</summary>
        /// <param name="audioSource">Target AudioSource.</param>
        /// <param name="to">Target stereo pan (-1 = left, 0 = center, 1 = right).</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowPanStereo(this AudioSource audioSource, float to, float duration) =>
            FlowTween.GetTween<AudioSource, float, AudioPanStereoInterpolator>(audioSource, duration, to);

        /// <summary>
        /// Fades an <see cref="AudioSource"/> volume to zero, then calls <c>Stop()</c>.
        /// </summary>
        /// <param name="audioSource">Target AudioSource.</param>
        /// <param name="duration">Fade duration in seconds.</param>
        public static Tween FlowFadeOutAndStop(this AudioSource audioSource, float duration)
        {
            return audioSource.FlowVolume(0f, duration)
                              .OnComplete(() => audioSource.Stop());
        }

        #endregion

        #region Light Methods

        /// <summary>Animates the intensity of a <see cref="Light"/>.</summary>
        /// <param name="light">Target Light.</param>
        /// <param name="to">Target intensity.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowIntensity(this Light light, float to, float duration) =>
            FlowTween.GetTween<Light, float, LightIntensityInterpolator>(light, duration, to);

        /// <summary>Animates the color of a <see cref="Light"/>.</summary>
        /// <param name="light">Target Light.</param>
        /// <param name="to">Target color.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowColor(this Light light, Color to, float duration) =>
            FlowTween.GetTween<Light, Color, LightColorInterpolator>(light, duration, to);

        /// <summary>Animates the range of a <see cref="Light"/>.</summary>
        /// <param name="light">Target Light.</param>
        /// <param name="to">Target range.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowRange(this Light light, float to, float duration) =>
            FlowTween.GetTween<Light, float, LightRangeInterpolator>(light, duration, to);

        #endregion

        #region Camera Methods

        /// <summary>Animates the field of view of a perspective <see cref="Camera"/>.</summary>
        /// <param name="camera">Target Camera.</param>
        /// <param name="to">Target FOV in degrees.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowFov(this Camera camera, float to, float duration) =>
            FlowTween.GetTween<Camera, float, CameraFovInterpolator>(camera, duration, to);

        /// <summary>Animates the orthographic size of an orthographic <see cref="Camera"/>.</summary>
        /// <param name="camera">Target Camera.</param>
        /// <param name="to">Target orthographic size.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowOrthoSize(this Camera camera, float to, float duration) =>
            FlowTween.GetTween<Camera, float, CameraOrthoSizeInterpolator>(camera, duration, to);

        /// <summary>Animates the background color of a <see cref="Camera"/>.</summary>
        /// <param name="camera">Target Camera.</param>
        /// <param name="to">Target background color.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowBackgroundColor(this Camera camera, Color to, float duration) =>
            FlowTween.GetTween<Camera, Color, CameraBackgroundColorInterpolator>(camera, duration, to);

        /// <summary>
        /// Animates the viewport rect of a <see cref="Camera"/>.
        /// Useful for split-screen transitions or cinematic letterboxing.
        /// </summary>
        /// <param name="camera">Target Camera.</param>
        /// <param name="to">Target viewport Rect (values 0–1).</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowRect(this Camera camera, Rect to, float duration)
        {
            var interp = CameraRectInterpolator.Get();
            interp.Setup(camera, to);
            return FlowTween.MakeTween(duration, interp, camera);
        }

        #endregion

        #region UI Graphics

        /// <summary>Animates the alpha of a UI <see cref="Graphic"/>.</summary>
        /// <param name="graphic">Target Graphic (Image, Text, etc.).</param>
        /// <param name="to">Target alpha (0–1).</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowFade(this Graphic graphic, float to, float duration) =>
            FlowTween.GetTween<Graphic, float, GraphicAlphaInterpolator>(graphic, duration, to);

        /// <summary>Fades a UI <see cref="Graphic"/> to fully opaque (alpha = 1).</summary>
        /// <param name="graphic">Target Graphic.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowFadeIn(this Graphic graphic, float duration) => graphic.FlowFade(1f, duration);

        /// <summary>Fades a UI <see cref="Graphic"/> to fully transparent (alpha = 0).</summary>
        /// <param name="graphic">Target Graphic.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowFadeOut(this Graphic graphic, float duration) => graphic.FlowFade(0f, duration);

        /// <summary>Animates the color of a UI <see cref="Graphic"/>.</summary>
        /// <param name="graphic">Target Graphic.</param>
        /// <param name="to">Target color.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowColor(this Graphic graphic, Color to, float duration) =>
            FlowTween.GetTween<Graphic, Color, GraphicColorInterpolator>(graphic, duration, to);

        /// <summary>
        /// Animates the color of a UI <see cref="Graphic"/> by sampling a <see cref="Gradient"/> over time.
        /// </summary>
        /// <param name="graphic">Target Graphic.</param>
        /// <param name="gradient">Gradient to sample across the tween duration.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowGradient(this Graphic graphic, Gradient gradient, float duration)
        {
            var interp = GradientInterpolator.Get();
            interp.Setup(graphic, gradient);
            return FlowTween.MakeTween(duration, interp, graphic);
        }

        /// <summary>Animates the fill amount of a UI <see cref="Image"/>.</summary>
        /// <param name="i">Target Image.</param>
        /// <param name="to">Target fill amount (0–1).</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowFillAmount(this Image i, float to, float duration) =>
            FlowTween.GetTween<Image, float, ImageFillInterpolator>(i, duration, to);

        /// <summary>Animates the scroll position of a <see cref="ScrollRect"/>.</summary>
        /// <param name="s">Target ScrollRect.</param>
        /// <param name="to">Target scroll position.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowPosition(this ScrollRect s, Vector2 to, float duration) =>
            FlowTween.GetTween<ScrollRect, Vector2, ScrollRectPositionInterpolator>(s, duration, to);

        /// <summary>Animates the value of a UI <see cref="Slider"/>.</summary>
        /// <param name="s">Target Slider.</param>
        /// <param name="to">Target slider value.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowValue(this Slider s, float to, float duration) =>
            FlowTween.GetTween<Slider, float, SliderValueInterpolator>(s, duration, to);

        #endregion

        #region TMP Pro
        /// <summary>
        /// Reveals TMP text characters using vertex alpha, animating from 0 to the full character count.
        /// For a per-character typewriter effect see <see cref="FlowTypewriter"/>.
        /// </summary>
        /// <param name="text">Target TMP_Text.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowReveal(this TMP_Text text, float duration)
        {
            text.ForceMeshUpdate();
            return FlowTween.GetTween<TMP_Text, int, TMPProRevealInterpolator>(text, duration, text.textInfo.characterCount);
        }

        /// <summary>
        /// Animates a displayed number in a <see cref="TMP_Text"/> from <paramref name="from"/> to <paramref name="to"/>.
        /// </summary>
        /// <param name="text">Target TMP_Text.</param>
        /// <param name="from">Starting number.</param>
        /// <param name="to">Ending number.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="format">Standard numeric format string (e.g. <c>"0"</c>, <c>"0.00"</c>). Default is <c>"0"</c>.</param>
        public static Tween FlowCounter(this TMP_Text text, float from, float to, float duration, string format = "0")
        {
            var interp = TmpCounterFloatInterpolator.Get();
            interp.Setup(text, from, to, format);
            return FlowTween.MakeTween(duration, interp, text);
        }

        /// <summary>
        /// Animates an integer counter displayed in a <see cref="TMP_Text"/> from <paramref name="from"/> to <paramref name="to"/>.
        /// </summary>
        /// <param name="text">Target TMP_Text.</param>
        /// <param name="from">Starting integer.</param>
        /// <param name="to">Ending integer.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowCounter(this TMP_Text text, int from, int to, float duration)
        {
            var interp = TmpCounterIntInterpolator.Get();
            interp.Setup(text, from, to);
            return FlowTween.MakeTween(duration, interp, text);
        }

        /// <summary>
        /// Animates a counter in a <see cref="TMP_Text"/> using a custom formatter callback.
        /// </summary>
        /// <param name="text">Target TMP_Text.</param>
        /// <param name="from">Starting value.</param>
        /// <param name="to">Ending value.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="formatter">
        /// A function that converts the current float value to a display string.
        /// Example: <c>value => $"${value:0.00}"</c>
        /// </param>
        public static Tween FlowCounter(this TMP_Text text, float from, float to, float duration, Func<float, string> formatter)
        {
            var interp = TmpCounterFormatterInterpolator.Get();
            interp.Setup(text, from, to, formatter);
            return FlowTween.MakeTween(duration, interp, text);
        }

        /// <summary>
        /// Animates the color of all characters in a <see cref="TMP_Text"/>.
        /// Forces a mesh update each tick to apply vertex color changes immediately.
        /// </summary>
        /// <param name="text">Target TMP_Text.</param>
        /// <param name="to">Target color.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowCharacterColor(this TMP_Text text, Color to, float duration)
        {
            var interp = TmpCharacterColorInterpolator.Get();
            interp.Setup(text, to);
            return FlowTween.MakeTween(duration, interp, text);
        }

        /// <summary>
        /// Reveals text in a <see cref="TMP_Text"/> one character at a time using <c>maxVisibleCharacters</c>.
        /// Unlike <see cref="FlowReveal"/>, this shows crisp fully-visible characters rather than fading them in.
        /// </summary>
        /// <param name="text">Target TMP_Text.</param>
        /// <param name="duration">Total reveal duration in seconds.</param>
        /// <param name="onComplete">Optional callback fired when all characters are visible.</param>
        public static Tween FlowTypewriter(this TMP_Text text, float duration, Action onComplete = null)
        {
            text.ForceMeshUpdate();
            int totalChars = text.textInfo.characterCount;
            text.maxVisibleCharacters = 0;

            var interp = TmpTypewriterInterpolator.Get();
            interp.Setup(text, totalChars);
            Tween tween = FlowTween.MakeTween(duration, interp, text);
            
            if (onComplete != null) 
                tween.OnComplete(onComplete);
            return tween;
        }
        #endregion

        #region RigidBody Methods

        /// <summary>
        /// Moves a <see cref="Rigidbody"/> to a world position using <c>MovePosition</c>.
        /// Automatically uses Fixed update mode.
        /// </summary>
        /// <param name="rb">Target Rigidbody.</param>
        /// <param name="to">Target world position.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowPosition(this Rigidbody rb, Vector3 to, float duration) =>
            FlowTween.GetTween<Rigidbody, Vector3, RigidbodyPositionInterpolator>(rb, duration, to)
                     .SetUpdateMode(Tween.TweenUpdateMode.Fixed);

        /// <summary>
        /// Rotates a <see cref="Rigidbody"/> to a world rotation using <c>MoveRotation</c>.
        /// Automatically uses Fixed update mode.
        /// </summary>
        /// <param name="rb">Target Rigidbody.</param>
        /// <param name="to">Target world rotation.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowRotation(this Rigidbody rb, Quaternion to, float duration) =>
            FlowTween.GetTween<Rigidbody, Quaternion, RigidbodyRotationInterpolator>(rb, duration, to)
                     .SetUpdateMode(Tween.TweenUpdateMode.Fixed);

        /// <summary>
        /// Moves a <see cref="Rigidbody2D"/> to a world position using <c>MovePosition</c>.
        /// Automatically uses Fixed update mode.
        /// </summary>
        /// <param name="rb">Target Rigidbody2D.</param>
        /// <param name="to">Target world position.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowPosition(this Rigidbody2D rb, Vector2 to, float duration) =>
            FlowTween.GetTween<Rigidbody2D, Vector2, Rigidbody2DPositionInterpolator>(rb, duration, to)
                     .SetUpdateMode(Tween.TweenUpdateMode.Fixed);

        /// <summary>
        /// Rotates a <see cref="Rigidbody2D"/> to a world rotation using <c>MoveRotation</c>.
        /// Automatically uses Fixed update mode.
        /// </summary>
        /// <param name="rb">Target Rigidbody2D.</param>
        /// <param name="to">Target rotation in degrees.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowRotation(this Rigidbody2D rb, float to, float duration) =>
            FlowTween.GetTween<Rigidbody2D, float, Rigidbody2DRotationInterpolator>(rb, duration, to)
                     .SetUpdateMode(Tween.TweenUpdateMode.Fixed);
        #endregion

        #region Spin Effect

        /// <summary>Rotates a Transform 360° around the Z axis (2D spin).</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="duration">Duration of one full rotation in seconds.</param>
        public static Tween FlowSpin(this Transform transform, float duration, int cycles = -1, Action<int> onCycle = null, SpinDirection direction = SpinDirection.Forward)
        {
            Vector3 spinDirection = direction == SpinDirection.Forward ? Vector3.forward : Vector3.back;
            Tween tween = transform.FlowRotateLocal(spinDirection * 360f, duration)
                .SetLoops(cycles, Tween.LoopType.Restart);
            if (onCycle != null) tween.OnLoop(onCycle);
            return tween;
        }

        /// <summary>Rotates a Transform 360° around a given axis.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="axis">World axis to spin around (e.g. <c>Vector3.up</c>).</param>
        /// <param name="duration">Duration of one full rotation in seconds.</param>
        public static Tween FlowSpin(this Transform transform, Vector3 direction, float duration, int cycles = -1, Action<int> onCycle = null)
        {
            Tween tween = transform.FlowRotateLocal(direction * 360f, duration).SetLoops(cycles, Tween.LoopType.Restart);
            if (onCycle != null) tween.OnLoop(onCycle);
            return tween;
        }

        #endregion

        #region Jello

        /// <summary>
        /// Plays an oscillating squash-and-stretch effect that decays like gelatine.
        /// The transform returns to its original scale on completion.
        /// </summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="intensity">Scale offset magnitude. Default is 0.25.</param>
        /// <param name="frequency">Oscillation frequency in cycles per second. Default is 4.</param>
        public static Tween FlowJello(this Transform transform, float duration, float intensity = 0.25f, float frequency = 4f)
        {
            Vector3 startScale = transform.localScale;
            var interp = JelloInterpolator.Get();
            interp.Setup(transform, startScale, intensity, frequency);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        #endregion

        #region Heartbeat

        /// <summary>
        /// Plays a double-pulse scale effect mimicking a heartbeat (lub-dub).
        /// Supports multiple consecutive beats within the given duration.
        /// </summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="duration">Total duration of all beats in seconds.</param>
        /// <param name="intensity">Scale overshoot magnitude. Default is 0.3.</param>
        /// <param name="beats">Number of heartbeat pulses to play. Default is 1.</param>
        public static Sequence FlowHeartbeat(this Transform transform, float duration, float intensity = 0.3f, int beats = 1)
        {
            Vector3 origin = transform.localScale;
            Vector3 lub    = origin * (1f + intensity);
            Vector3 mid    = origin * (1f + intensity * 0.4f);
            Vector3 dub    = origin * (1f + intensity * 0.65f);

            float beatDur = duration / beats;
            float step    = beatDur / 5f;

            Sequence seq = FlowTween.Sequence();

            for (int i = 0; i < beats; i++)
            {
                seq.Append(transform.FlowScale(lub,    step).Sine().EaseOut())
                   .Append(transform.FlowScale(mid,    step).Sine().EaseInOut())
                   .Append(transform.FlowScale(dub,    step).Sine().EaseOut())
                   .Append(transform.FlowScale(origin, step * 2f).Sine().EaseIn());
            }

            return seq.Play();
        }

        #endregion

        #region WobbleRotate

        /// <summary>
        /// Plays a decaying oscillating rotation effect (wobble), settling back to the original rotation.
        /// </summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="strength">Peak rotation angle in degrees. Default is 20.</param>
        /// <param name="frequency">Oscillation frequency in cycles per second. Default is 4.</param>
        public static Tween FlowWobbleRotate(this Transform transform, float duration, float strength = 20f, float frequency = 4f)
        {
            Quaternion startRot = transform.localRotation;
            var interp = WobbleRotateInterpolator.Get();
            interp.Setup(transform, startRot, strength, frequency);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        #endregion

        #region Flip

        /// <summary>
        /// Flips a Transform on the Y axis by scaling through zero, creating a card-flip illusion.
        /// Set <paramref name="full"/> to <c>true</c> for a 360° flip back to the original face.
        /// </summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="duration">Total flip duration in seconds.</param>
        /// <param name="full">If true, performs a full 360° flip returning to the original face. Default is false (180°).</param>
        public static Sequence FlowFlipY(this Transform transform, float duration, bool full = false)
        {
            Vector3 startScale = transform.localScale;
            Vector3 flat       = new Vector3(0f, startScale.y, startScale.z);
            Vector3 flipped    = new Vector3(-startScale.x, startScale.y, startScale.z);

            float half = duration * 0.5f;

            Sequence seq = FlowTween.Sequence()
                .Append(transform.FlowScale(flat, half).Sine().EaseIn())
                .Append(transform.FlowScale(full ? flat : flipped, 0f))
                .Append(transform.FlowScale(full ? startScale : flipped, half).Sine().EaseOut());

            return seq.Play();
        }

        /// <summary>
        /// Flips a Transform on the X axis by scaling through zero (top-to-bottom flip).
        /// Set <paramref name="full"/> to <c>true</c> for a 360° flip returning to the original face.
        /// </summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="duration">Total flip duration in seconds.</param>
        /// <param name="full">If true, performs a full 360° flip. Default is false (180°).</param>
        public static Sequence FlowFlipX(this Transform transform, float duration, bool full = false)
        {
            Vector3 startScale = transform.localScale;
            Vector3 flat       = new Vector3(startScale.x, 0f, startScale.z);
            Vector3 flipped    = new Vector3(startScale.x, -startScale.y, startScale.z);

            float half = duration * 0.5f;

            Sequence seq = FlowTween.Sequence()
                .Append(transform.FlowScale(flat, half).Sine().EaseIn())
                .Append(transform.FlowScale(full ? startScale : flipped, half).Sine().EaseOut());

            return seq.Play();
        }

        #endregion

        #region Blink

        /// <summary>
        /// Rapidly blinks a <see cref="SpriteRenderer"/> a set number of times by fading in and out.
        /// </summary>
        /// <param name="spriteRenderer">Target SpriteRenderer.</param>
        /// <param name="blinks">Number of blink cycles. Default is 4.</param>
        /// <param name="blinkDuration">Duration of each fade in/out step in seconds. Default is 0.1.</param>
        /// <param name="endVisible">Whether the renderer ends fully visible. Default is true.</param>
        public static Tween FlowBlink(this SpriteRenderer spriteRenderer, int blinks = 4, float blinkDuration = 0.1f, bool endVisible = true)
        {
            var interp = SpriteBlinkInterpolator.Get();
            interp.Setup(spriteRenderer, blinks, endVisible);
            return FlowTween.MakeTween(blinkDuration * blinks, interp, spriteRenderer);
        }

        /// <summary>
        /// Rapidly blinks a <see cref="CanvasGroup"/> a set number of times by fading in and out.
        /// </summary>
        /// <param name="CanvasGroup">Target CanvasGroup.</param>
        /// <param name="blinks">Number of blink cycles. Default is 4.</param>
        /// <param name="blinkDuration">Duration of each blink cycle in seconds. Default is 0.1.</param>
        /// <param name="endVisible">Whether the renderer ends fully visible. Default is true.</param>
        public static Tween FlowBlink(this CanvasGroup canvasGroup, int blinks = 4, float blinkDuration = 0.1f, bool endVisible = true)
        {
            var interp = CanvasGroupBlinkInterpolator.Get();
            interp.Setup(canvasGroup, blinks, endVisible);
            return FlowTween.MakeTween(blinkDuration * blinks, interp, canvasGroup);
        }

        #endregion

        #region SlideIn / SlideOut

        /// <summary>Direction used by <see cref="FlowSlideIn"/> and <see cref="FlowSlideOut"/>.</summary>
        public enum SlideDirection { Left, Right, Up, Down }

        /// <summary>
        /// Slides a <see cref="RectTransform"/> into its current anchored position from an edge offset.
        /// The transform is moved to the start position immediately before the tween begins.
        /// </summary>
        /// <param name="rectTransform">Target RectTransform.</param>
        /// <param name="direction">The direction the element slides in from.</param>
        /// <param name="offset">How far outside the current position to start from, in pixels.</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowSlideIn(this RectTransform rectTransform, SlideDirection direction, float offset, float duration)
        {
            Vector2 target    = rectTransform.anchoredPosition;
            Vector2 startFrom = target + DirectionToOffset(direction, offset);

            return rectTransform.FlowAnchorMove(target, duration)
                                .Sine().EaseOut()
                                .OnStart(() => rectTransform.anchoredPosition = startFrom);
        }

        /// <summary>
        /// Slides a <see cref="RectTransform"/> out of its current anchored position toward an edge.
        /// </summary>
        /// <param name="rectTransform">Target RectTransform.</param>
        /// <param name="direction">The direction the element slides toward.</param>
        /// <param name="offset">How far to slide in pixels.</param>
        /// <param name="duration">Duration in seconds.</param>
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
        /// Starts a continuous up-and-down bobbing loop on a Transform driven by a sine wave.
        /// Call <c>Kill()</c> on the returned tween to stop the effect.
        /// </summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="amplitude">Vertical distance of the bob in units. Default is 0.2.</param>
        /// <param name="frequency">Bobs per second. Default is 1.</param>
        public static Tween FlowFloat(this Transform transform, float amplitude = 0.2f, float frequency = 1f)
        {
            Vector3 origin = transform.localPosition;
            var interp = FloatBobInterpolator.Get();
            interp.Setup(transform, origin, amplitude);
            return FlowTween.MakeTween(1f / frequency, interp, transform).SetLoops(-1, Tween.LoopType.Restart);
        }

        #endregion

        #region Pulse

        /// <summary>
        /// Starts a looping scale and alpha throb effect to draw attention.
        /// Requires both a Transform and a CanvasGroup.
        /// Call <c>Kill()</c> on the returned sequence to stop the effect.
        /// </summary>
        /// <param name="transform">Transform to scale.</param>
        /// <param name="canvasGroup">CanvasGroup to fade.</param>
        /// <param name="scaleMagnitude">Scale overshoot as a fraction of original scale. Default is 0.05.</param>
        /// <param name="alphaMin">Minimum alpha during the pulse. Default is 0.7.</param>
        /// <param name="frequency">Pulses per second. Default is 1.</param>
        public static (Tween scale, Tween alpha) FlowPulse(this Transform transform, CanvasGroup canvasGroup,
            float scaleMagnitude = 0.05f, float alphaMin = 0.7f, float frequency = 1f)
        {
            Vector3 origin = transform.localScale;
            float   period = 1f / frequency;

            Tween scaleTween = transform.FlowScale(origin * (1f + scaleMagnitude), period)
                .SetLoops(-1, Tween.LoopType.Yoyo).Sine().EaseInOut();

            Tween alphaTween = canvasGroup.FlowFade(alphaMin, period)
                .SetLoops(-1, Tween.LoopType.Yoyo).Sine().EaseInOut();

            return (scaleTween, alphaTween);
        }

        /// <summary>
        /// Starts a looping scale-only throb effect. No CanvasGroup required.
        /// Call <c>Kill()</c> on the returned tween to stop the effect.
        /// </summary>
        /// <param name="transform">Transform to scale.</param>
        /// <param name="scaleMagnitude">Scale overshoot as a fraction of original scale. Default is 0.05.</param>
        /// <param name="frequency">Pulses per second. Default is 1.</param>
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

        /// <summary>Shakes a Transform in world-space XY using Perlin noise. Resets position on completion.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="strength">Peak shake offset in units. Default is 1.</param>
        /// <param name="frequency">Shake speed (samples per second). Default is 20.</param>
        public static Tween FlowShake2D(this Transform transform, float duration, float strength = 1f, float frequency = 20f)
        {
            Vector3 startPos = transform.position;
            float seedX = UnityEngine.Random.value * 10000f;
            float seedY = seedX + 131.73f;
            var interp = Shake2DInterpolator.Get();
            interp.Setup(transform, startPos, strength, frequency, seedX, seedY, false);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        /// <summary>Shakes a Transform in local-space XY using Perlin noise. Resets local position on completion.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="strength">Peak shake offset in units. Default is 1.</param>
        /// <param name="frequency">Shake speed (samples per second). Default is 20.</param>
        public static Tween FlowShakeLocal2D(this Transform transform, float duration, float strength = 1f, float frequency = 20f)
        {
            Vector3 startPos = transform.localPosition;
            float seedX = UnityEngine.Random.value * 10000f;
            float seedY = seedX + 131.73f;
            var interp = Shake2DInterpolator.Get();
            interp.Setup(transform, startPos, strength, frequency, seedX, seedY, true);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        #endregion

        #region Shake 3D

        /// <summary>Shakes a Transform in world-space XYZ using Perlin noise. Resets position on completion.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="strength">Peak shake offset in units. Default is 1.</param>
        /// <param name="frequency">Shake speed (samples per second). Default is 20.</param>
        public static Tween FlowShake3D(this Transform transform, float duration, float strength = 1f, float frequency = 20f)
        {
            Vector3 startPos = transform.position;
            float seedX = UnityEngine.Random.value * 10000f;
            float seedY = seedX + 131.73f;
            float seedZ = seedX + 263.46f;
            var interp = Shake3DInterpolator.Get();
            interp.Setup(transform, startPos, strength, frequency, seedX, seedY, seedZ, false);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        /// <summary>Shakes a Transform in local-space XYZ using Perlin noise. Resets local position on completion.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="strength">Peak shake offset in units. Default is 1.</param>
        /// <param name="frequency">Shake speed (samples per second). Default is 20.</param>
        public static Tween FlowShakeLocal3D(this Transform transform, float duration, float strength = 1f, float frequency = 20f)
        {
            Vector3 startPos = transform.localPosition;
            float seedX = UnityEngine.Random.value * 10000f;
            float seedY = seedX + 131.73f;
            float seedZ = seedX + 263.46f;
            var interp = Shake3DInterpolator.Get();
            interp.Setup(transform, startPos, strength, frequency, seedX, seedY, seedZ, true);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        #endregion

        #region Shake Rotation

        /// <summary>Shakes a Transform's local rotation in all three axes using Perlin noise.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="strength">Peak rotation angle in degrees. Default is 15.</param>
        /// <param name="frequency">Shake speed (samples per second). Default is 20.</param>
        public static Tween FlowShakeRotation3D(this Transform transform, float duration, float strength = 15f, float frequency = 20f)
        {
            Quaternion startRot = transform.localRotation;
            float seedX = UnityEngine.Random.value * 10000f;
            float seedY = seedX + 131.73f;
            float seedZ = seedX + 263.46f;
            var interp = ShakeRotation3DInterpolator.Get();
            interp.Setup(transform, startRot, strength, frequency, seedX, seedY, seedZ);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        /// <summary>Shakes a Transform's local rotation around the Z axis only (2D screen shake).</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="strength">Peak rotation angle in degrees. Default is 15.</param>
        /// <param name="frequency">Shake speed (samples per second). Default is 20.</param>
        public static Tween FlowShakeRotation2D(this Transform transform, float duration, float strength = 15f, float frequency = 20f)
        {
            Quaternion startRot = transform.localRotation;
            float seed = UnityEngine.Random.value * 10000f;
            var interp = ShakeRotation2DInterpolator.Get();
            interp.Setup(transform, startRot, strength, frequency, seed);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        /// <summary>Shakes a Transform's local rotation around a specified world axis.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="axis">World axis to shake around (e.g. <c>Vector3.forward</c>).</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="strength">Peak rotation angle in degrees. Default is 15.</param>
        /// <param name="frequency">Shake speed (samples per second). Default is 20.</param>
        public static Tween FlowShakeRotationAxis(this Transform transform, Vector3 axis, float duration, float strength = 15f, float frequency = 20f)
        {
            Quaternion startRot = transform.localRotation;
            float seed = UnityEngine.Random.value * 10000f;
            var interp = ShakeRotationAxisInterpolator.Get();
            interp.Setup(transform, startRot, axis, strength, frequency, seed);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        #endregion

        #region Shake Scale

        /// <summary>Shakes a Transform's local scale using Perlin noise, decaying back to the original scale.</summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="strength">Peak scale offset magnitude. Default is 0.3.</param>
        /// <param name="frequency">Shake speed (samples per second). Default is 20.</param>
        public static Tween FlowShakeScale(this Transform transform, float duration, float strength = 0.3f, float frequency = 20f)
        {
            Vector3 startScale = transform.localScale;
            float seedX = UnityEngine.Random.value * 10000f;
            float seedY = seedX + 131.73f;
            float seedZ = seedX + 263.46f;
            var interp = ShakeScaleInterpolator.Get();
            interp.Setup(transform, startScale, strength, frequency, seedX, seedY, seedZ);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        #endregion

        #region Punch 2D

        /// <summary>
        /// Punches a Transform's world position in a 2D direction, oscillating and decaying back to the start.
        /// </summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="punch">Direction and magnitude of the punch in world space.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="vibrato">Number of oscillations. Default is 10.</param>
        /// <param name="elasticity">Overshoot multiplier. Default is 1.</param>
        public static Tween FlowPunchPosition2D(this Transform transform, Vector2 punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            Vector3 startPosition = transform.position;
            Vector3 punch3D       = new Vector3(punch.x, punch.y, 0f);
            var interp = PunchPosition2DInterpolator.Get();
            interp.Setup(transform, startPosition, punch3D, vibrato, elasticity);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        /// <summary>
        /// Punches a Transform's local scale uniformly in 2D, oscillating and decaying back to the start.
        /// </summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="punch">Scale offset magnitude.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="vibrato">Number of oscillations. Default is 10.</param>
        /// <param name="elasticity">Overshoot multiplier. Default is 1.</param>
        public static Tween FlowPunchScale2D(this Transform transform, float punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            Vector3 startScale = transform.localScale;
            var interp = PunchScale2DInterpolator.Get();
            interp.Setup(transform, startScale, punch, vibrato, elasticity);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        #endregion

        #region Punch 3D

        /// <summary>
        /// Punches a Transform's world position in a 3D direction, oscillating and decaying back to the start.
        /// </summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="punch">Direction and magnitude of the punch in world space.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="vibrato">Number of oscillations. Default is 10.</param>
        /// <param name="elasticity">Overshoot multiplier. Default is 1.</param>
        public static Tween FlowPunchPosition3D(this Transform transform, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            Vector3 startPosition = transform.position;
            var interp = PunchPosition3DInterpolator.Get();
            interp.Setup(transform, startPosition, punch, vibrato, elasticity);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        /// <summary>
        /// Punches a Transform's local scale along a 3D vector, oscillating and decaying back to the start.
        /// </summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="punch">Per-axis scale punch offset.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="vibrato">Number of oscillations. Default is 10.</param>
        /// <param name="elasticity">Overshoot multiplier. Default is 1.</param>
        public static Tween FlowPunchScale3D(this Transform transform, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            Vector3 startScale = transform.localScale;
            var interp = PunchScale3DInterpolator.Get();
            interp.Setup(transform, startScale, punch, vibrato, elasticity);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        #endregion

        #region Flow Path

        /// <summary>
        /// Moves a Transform along a smooth Catmull-Rom spline through the given world-space waypoints.
        /// The transform's current position is used as the implicit starting point.
        /// </summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="waypoints">World-space points to travel through, in order.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="closedLoop">If true, the path loops back to the start position. Default is false.</param>
        /// <param name="orientToPath">If true, the transform rotates to face its direction of travel. Default is false.</param>
        public static Tween FlowPath(this Transform transform, Vector3[] waypoints, float duration,
            bool closedLoop = false, bool orientToPath = false)
        {
            Vector3[] points = new Vector3[waypoints.Length];
            System.Array.Copy(waypoints, points, waypoints.Length);

            Vector3 startPos = transform.position;

            var interp = FlowPathInterpolator.Get();
            interp.Setup(transform, points, startPos, closedLoop, orientToPath, false);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        /// <summary>
        /// Moves a Transform along a smooth Catmull-Rom spline through the given local-space waypoints.
        /// The transform's current local position is used as the implicit starting point.
        /// </summary>
        /// <param name="transform">Target transform.</param>
        /// <param name="waypoints">Local-space points to travel through, in order.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="closedLoop">If true, the path loops back to the start local position. Default is false.</param>
        /// <param name="orientToPath">If true, the transform rotates to face its local direction of travel. Default is false.</param>
        public static Tween FlowPathLocal(this Transform transform, Vector3[] waypoints, float duration, bool closedLoop = false, bool orientToPath = false)
        {
            Vector3[] points = new Vector3[waypoints.Length];
            System.Array.Copy(waypoints, points, waypoints.Length);

            Vector3 startPos = transform.localPosition;

            var interp = FlowPathInterpolator.Get();
            interp.Setup(transform, points, startPos, closedLoop, orientToPath, true);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        #endregion

        #region Shape Blend

        /// <summary>
        /// Animates a blend shape weight on a <see cref="SkinnedMeshRenderer"/>.
        /// Useful for facial animation, morphing, or mesh deformation effects.
        /// </summary>
        /// <param name="renderer">Target SkinnedMeshRenderer.</param>
        /// <param name="shapeIndex">Index of the blend shape to animate.</param>
        /// <param name="to">Target blend shape weight (typically 0–100).</param>
        /// <param name="duration">Duration in seconds.</param>
        public static Tween FlowBlendShape(this SkinnedMeshRenderer renderer, int shapeIndex, float to, float duration)
        {
            var interp = BlendShapeInterpolator.Get();
            interp.Setup(renderer, shapeIndex, to);
            return FlowTween.MakeTween(duration, interp, renderer);
        }

        #endregion

        #region Look At
        /// <summary>
        /// Continuously rotates a Transform to face a dynamic world position each tick for the given duration.
        /// </summary>
        /// <param name="transform">Transform to rotate.</param>
        /// <param name="targetProvider">A delegate returning the world position to face. Called each tick.</param>
        /// <param name="duration">How long to track the position in seconds.</param>
        /// <param name="upAxis">The up axis used for orientation. Defaults to <c>Vector3.up</c>.</param>
        public static Tween FlowLookAt(this Transform transform, Func<Vector3> targetProvider, float duration, Vector3? upAxis = null)
        {
            Vector3 up = upAxis ?? Vector3.up;
            var interp = LookAtInterpolator.Get();
            interp.Setup(transform, targetProvider, up, false);
            return FlowTween.MakeTween(duration, interp, transform);
        }

        /// <summary>
        /// Continuously rotates a Transform to face a fixed world position each tick for the given duration.
        /// </summary>
        /// <param name="transform">Transform to rotate.</param>
        /// <param name="target">The world position to look at.</param>
        /// <param name="duration">How long to track the position in seconds.</param>
        /// <param name="upAxis">The up axis used for orientation. Defaults to <c>Vector3.up</c>.</param>
        public static Tween FlowLookAt2D(this Transform transform, Func<Vector3> targetProvider, float duration)
        {
            var interp = LookAtInterpolator.Get();
            interp.Setup(transform, targetProvider, Vector3.up, true);
            return FlowTween.MakeTween(duration, interp, transform);
        }
        #endregion
    }
}