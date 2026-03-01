using System;
using UnityEngine;

namespace FlT
{
    public static class FlowVirtual
    {
        public static Tween Float(float from, float to, float duration, Action<float> action)
        {
            var interp = FloatInterpolator.Get();
            interp.Setup(from, to, action);

            Tween tween = FlowTween.GetTweenRaw(duration);
            tween.SetInterpolator(interp);

            return tween;
        }

        public static Tween Int(int from, int to, float duration, Action<int> action)
        {
            var interp = IntInterpolator.Get();
            interp.Setup(from, to, action);

            Tween tween = FlowTween.GetTweenRaw(duration);
            tween.SetInterpolator(interp);

            return tween;
        }

        public static Tween Vector2(Vector2 from, Vector2 to, float duration, Action<Vector2> action)
        {
            var interp = Vector2Interpolator.Get();
            interp.Setup(from, to, action);

            Tween tween = FlowTween.GetTweenRaw(duration);
            tween.SetInterpolator(interp);

            return tween;
        }

        public static Tween Vector3(Vector3 from, Vector3 to, float duration, Action<Vector3> action)
        {
            var interp = Vector3Interpolator.Get();
            interp.Setup(from, to, action);

            Tween tween = FlowTween.GetTweenRaw(duration);
            tween.SetInterpolator(interp);

            return tween;
        }

        public static Tween Color(Color from, Color to, float duration, Action<Color> action)
        {
            var interp = ColorInterpolator.Get();
            interp.Setup(from, to, action);

            Tween tween = FlowTween.GetTweenRaw(duration);
            tween.SetInterpolator(interp);

            return tween;
        }

        public static Tween DelayedCall(float duration, Action callback = null)
        {
            Tween tween = FlowTween.GetTweenRaw(duration).Linear().EaseIn();
            if (callback != null) tween.OnComplete(callback);
            return tween;
        }
    }
}
