using UnityEngine;

namespace FlT
{
    public static class EaseMath
    {
        public static float Evaluate(float t, Tween.TransitionType transition, Tween.EaseType ease)
        {
            if (transition == Tween.TransitionType.Linear) return t;

            return ease switch
            {
                Tween.EaseType.In    => In(t, transition),
                Tween.EaseType.Out   => Out(t, transition),
                Tween.EaseType.InOut => InOut(t, transition),
                Tween.EaseType.OutIn => OutIn(t, transition),
                _                    => t
            };
        }

        private static float In(float t, Tween.TransitionType transition) => transition switch
        {
            Tween.TransitionType.Sine    => 1f - Mathf.Cos(t * Mathf.PI / 2f),
            Tween.TransitionType.Quad    => t * t,
            Tween.TransitionType.Cubic   => t * t * t,
            Tween.TransitionType.Quart   => t * t * t * t,
            Tween.TransitionType.Quint   => t * t * t * t * t,
            Tween.TransitionType.Expo    => t == 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f),
            Tween.TransitionType.Circ    => 1f - Mathf.Sqrt(1f - t * t),
            Tween.TransitionType.Back    => BackIn(t),
            Tween.TransitionType.Elastic => ElasticIn(t),
            Tween.TransitionType.Bounce  => BounceIn(t),
            Tween.TransitionType.Spring  => SpringIn(t),
            _                            => t
        };

        private static float Out(float t, Tween.TransitionType transition) => transition switch
        {
            Tween.TransitionType.Sine    => Mathf.Sin(t * Mathf.PI / 2f),
            Tween.TransitionType.Quad    => 1f - (1f - t) * (1f - t),
            Tween.TransitionType.Cubic   => 1f - Mathf.Pow(1f - t, 3f),
            Tween.TransitionType.Quart   => 1f - Mathf.Pow(1f - t, 4f),
            Tween.TransitionType.Quint   => 1f - Mathf.Pow(1f - t, 5f),
            Tween.TransitionType.Expo    => t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t),
            Tween.TransitionType.Circ    => Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f)),
            Tween.TransitionType.Back    => BackOut(t),
            Tween.TransitionType.Elastic => ElasticOut(t),
            Tween.TransitionType.Bounce  => BounceOut(t),
            Tween.TransitionType.Spring  => SpringOut(t),
            _                            => t
        };

        private static float InOut(float t, Tween.TransitionType transition) => transition switch
        {
            Tween.TransitionType.Sine    => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f,
            Tween.TransitionType.Quad    => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f,
            Tween.TransitionType.Cubic   => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f,
            Tween.TransitionType.Quart   => t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) / 2f,
            Tween.TransitionType.Quint   => t < 0.5f ? 16f * t * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 5f) / 2f,
            Tween.TransitionType.Expo    => t == 0f ? 0f : t == 1f ? 1f : t < 0.5f
                                            ? Mathf.Pow(2f, 20f * t - 10f) / 2f
                                            : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f,
            Tween.TransitionType.Circ    => t < 0.5f
                                            ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) / 2f
                                            : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) / 2f,
            Tween.TransitionType.Back    => BackInOut(t),
            Tween.TransitionType.Elastic => ElasticInOut(t),
            Tween.TransitionType.Bounce  => BounceInOut(t),
            Tween.TransitionType.Spring  => SpringInOut(t),
            _                            => t
        };

        private static float OutIn(float t, Tween.TransitionType transition)
        {
            return t < 0.5f
                ? Out(t * 2f, transition) * 0.5f
                : In((t - 0.5f) * 2f, transition) * 0.5f + 0.5f;
        }

        #region Back
        private static float BackIn(float t)
        {
            const float c = 1.70158f;
            return (c + 1f) * t * t * t - c * t * t;
        }

        private static float BackOut(float t)
        {
            const float c = 1.70158f;
            return 1f + (c + 1f) * Mathf.Pow(t - 1f, 3f) + c * Mathf.Pow(t - 1f, 2f);
        }

        private static float BackInOut(float t)
        {
            const float c = 1.70158f * 1.525f;
            return t < 0.5f
                ? Mathf.Pow(2f * t, 2f) * ((c + 1f) * 2f * t - c) / 2f
                : (Mathf.Pow(2f * t - 2f, 2f) * ((c + 1f) * (2f * t - 2f) + c) + 2f) / 2f;
        }
        #endregion

        #region Elastic
        private static float ElasticIn(float t)
        {
            if (t == 0f || t == 1f) return t;
            return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * (2f * Mathf.PI) / 3f);
        }

        private static float ElasticOut(float t)
        {
            if (t == 0f || t == 1f) return t;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI) / 3f) + 1f;
        }

        private static float ElasticInOut(float t)
        {
            if (t == 0f || t == 1f) return t;
            return t < 0.5f
                ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * (2f * Mathf.PI) / 4.5f)) / 2f
                : Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * (2f * Mathf.PI) / 4.5f) / 2f + 1f;
        }
        #endregion

        #region Bounce
        private static float BounceOut(float t)
        {
            const float n1 = 7.5625f, d1 = 2.75f;
            if (t < 1f / d1)   return n1 * t * t;
            if (t < 2f / d1)   return n1 * (t -= 1.5f / d1) * t + 0.75f;
            if (t < 2.5f / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f;
                                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }

        private static float BounceIn(float t) => 1f - BounceOut(1f - t);

        private static float BounceInOut(float t) => t < 0.5f
            ? BounceIn(t * 2f) * 0.5f
            : BounceOut(t * 2f - 1f) * 0.5f + 0.5f;
        #endregion

        #region Spring
        private static float SpringOut(float t)
        {
            return 1f - Mathf.Cos(t * Mathf.PI * (0.2f + 2.5f * t * t * t)) * Mathf.Pow(1f - t, 2.2f);
        }

        private static float SpringIn(float t) => 1f - SpringOut(1f - t);

        private static float SpringInOut(float t) => t < 0.5f
            ? SpringIn(t * 2f) * 0.5f
            : SpringOut(t * 2f - 1f) * 0.5f + 0.5f;
        #endregion
    }
}