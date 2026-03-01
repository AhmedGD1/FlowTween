using System.Collections;

namespace FlT
{
    public static class FlowTweenCoroutine
    {
        // ── Tween ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Yields until the tween completes all its loops, or is killed.
        /// Safe to use with finite loop counts only; infinite loops will yield forever.
        /// </summary>
        public static IEnumerator WaitForCompletion(this Tween tween)
        {
            while (tween != null && !tween.IsCompleted)
                yield return null;
        }

        /// <summary>
        /// Yields until the tween is killed (via Kill() or a destroyed target).
        /// Completes naturally also counts — the tween is marked completed which stops the loop.
        /// </summary>
        public static IEnumerator WaitForKill(this Tween tween)
        {
            // IsCompleted becomes true on both natural completion AND Kill(),
            // so this doubles as a "wait until gone" helper.
            while (tween != null && !tween.IsCompleted)
                yield return null;
        }

        /// <summary>
        /// Yields until the tween has completed at least <paramref name="loops"/> loops.
        /// Loop count starts at 0 on the first completed loop.
        /// </summary>
        public static IEnumerator WaitForElapsedLoops(this Tween tween, int loops)
        {
            while (tween != null && !tween.IsCompleted && tween.DbgCurrentLoop < loops)
                yield return null;
        }

        /// <summary>
        /// Yields until the tween's normalized progress reaches or exceeds
        /// <paramref name="position"/> (0–1).
        /// </summary>
        public static IEnumerator WaitForPosition(this Tween tween, float position)
        {
            while (tween != null && !tween.IsCompleted && tween.Progress < position)
                yield return null;
        }

        /// <summary>
        /// Yields for <paramref name="seconds"/> of tween-time elapsed
        /// (respects the tween's own TimeScale, but NOT FlowTween's globalTimeScale
        ///  since Elapsed is already advanced by the engine each frame).
        /// </summary>
        public static IEnumerator WaitForSeconds(this Tween tween, float seconds)
        {
            float target = tween.Elapsed + seconds;
            while (tween != null && !tween.IsCompleted && tween.Elapsed < target)
                yield return null;
        }

        // ── Sequence ───────────────────────────────────────────────────────────

        /// <summary>
        /// Yields until the sequence completes all its loops, or is killed.
        /// </summary>
        public static IEnumerator WaitForCompletion(this Sequence sequence)
        {
            while (sequence != null && !sequence.IsCompleted)
                yield return null;
        }

        /// <summary>
        /// Yields until the sequence is killed or naturally completes.
        /// </summary>
        public static IEnumerator WaitForKill(this Sequence sequence)
        {
            while (sequence != null && !sequence.IsCompleted)
                yield return null;
        }

        /// <summary>
        /// Yields until the sequence has completed at least <paramref name="loops"/> loops.
        /// </summary>
        public static IEnumerator WaitForElapsedLoops(this Sequence sequence, int loops)
        {
            while (sequence != null && !sequence.IsCompleted && sequence.DbgCurrentLoop < loops)
                yield return null;
        }

        /// <summary>
        /// Yields until the sequence's elapsed time reaches <paramref name="position"/> seconds.
        /// </summary>
        public static IEnumerator WaitForPosition(this Sequence sequence, float position)
        {
            while (sequence != null && !sequence.IsCompleted && sequence.Elapsed < position)
                yield return null;
        }
    }
}
