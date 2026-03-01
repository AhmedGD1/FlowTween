using System.Collections;
using UnityEngine;

namespace FlT
{
    /// <summary>
    /// Coroutine support for FlowTween.
    /// Mirrors DOTween's WaitForCompletion / WaitForKill / WaitForElapsedLoops pattern.
    ///
    /// Usage:
    ///   yield return tween.WaitForCompletion();
    ///   yield return tween.WaitForKill();
    ///   yield return tween.WaitForElapsedLoops(2);
    ///   yield return tween.WaitForPosition(0.5f);
    ///   yield return sequence.WaitForCompletion();
    /// </summary>
    public static class FlowTweenCoroutine
    {
        // ── Tween ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Yields until the tween completes all its loops, or is killed.
        /// </summary>
        public static IEnumerator WaitForCompletion(this Tween tween)
        {
            // We can't poll tween.IsCompleted directly because FlowTween returns
            // the tween to the pool (resetting it) the same frame it completes.
            // Instead we plant a flag via callbacks that survives the reset.
            bool done = false;
            tween.OnComplete(() => done = true);
            tween.OnKill(() => done = true);

            while (!done)
                yield return null;
        }

        /// <summary>
        /// Yields until the tween is killed (via Kill() or a destroyed target).
        /// Natural completion also counts.
        /// </summary>
        public static IEnumerator WaitForKill(this Tween tween)
        {
            bool done = false;
            tween.OnComplete(() => done = true);
            tween.OnKill(() => done = true);

            while (!done)
                yield return null;
        }

        /// <summary>
        /// Yields until the tween has completed at least <paramref name="loops"/> loops.
        /// </summary>
        public static IEnumerator WaitForElapsedLoops(this Tween tween, int loops)
        {
            bool done = false;
            tween.OnComplete(() => done = true);
            tween.OnKill(() => done = true);
            tween.OnLoop(currentLoop =>
            {
                if (currentLoop >= loops) done = true;
            });

            while (!done)
                yield return null;
        }

        /// <summary>
        /// Yields until the tween's normalized progress reaches or exceeds
        /// <paramref name="position"/> (0-1).
        /// </summary>
        public static IEnumerator WaitForPosition(this Tween tween, float position)
        {
            bool done = false;
            tween.OnComplete(() => done = true);
            tween.OnKill(() => done = true);

            while (!done && tween.Progress < position)
                yield return null;
        }

        /// <summary>
        /// Yields for <paramref name="seconds"/> of tween-time elapsed.
        /// </summary>
        public static IEnumerator WaitForSeconds(this Tween tween, float seconds)
        {
            bool done = false;
            tween.OnComplete(() => done = true);
            tween.OnKill(() => done = true);

            float target = tween.Elapsed + seconds;
            while (!done && tween.Elapsed < target)
                yield return null;
        }

        // ── Sequence ───────────────────────────────────────────────────────────

        /// <summary>
        /// Yields until the sequence completes all its loops, or is killed.
        /// </summary>
        public static IEnumerator WaitForCompletion(this Sequence sequence)
        {
            bool done = false;
            sequence.OnComplete(() => done = true);
            sequence.OnKill(() => done = true);

            while (!done)
                yield return null;
        }

        /// <summary>
        /// Yields until the sequence is killed or naturally completes.
        /// </summary>
        public static IEnumerator WaitForKill(this Sequence sequence)
        {
            bool done = false;
            sequence.OnComplete(() => done = true);
            sequence.OnKill(() => done = true);

            while (!done)
                yield return null;
        }

        /// <summary>
        /// Yields until the sequence has completed at least <paramref name="loops"/> loops.
        /// </summary>
        public static IEnumerator WaitForElapsedLoops(this Sequence sequence, int loops)
        {
            bool done = false;
            sequence.OnComplete(() => done = true);
            sequence.OnKill(() => done = true);
            sequence.OnLoop(currentLoop =>
            {
                if (currentLoop >= loops) done = true;
            });

            while (!done)
                yield return null;
        }

        /// <summary>
        /// Yields until the sequence's elapsed time reaches <paramref name="position"/> seconds.
        /// </summary>
        public static IEnumerator WaitForPosition(this Sequence sequence, float position)
        {
            bool done = false;
            sequence.OnComplete(() => done = true);
            sequence.OnKill(() => done = true);

            while (!done && sequence.Elapsed < position)
                yield return null;
        }
    }
}